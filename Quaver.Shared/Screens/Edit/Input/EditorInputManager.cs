﻿using System;
using System.Collections.Generic;
using System.Linq;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Screens.Edit.Dialogs;
using Quaver.Shared.Screens.Edit.Plugins;
using Quaver.Shared.Screens.Edit.UI;
using Quaver.Shared.Screens.Edit.UI.Playfield.Waveform;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Logging;
using Wobble.Platform;

namespace Quaver.Shared.Screens.Edit.Input
{
    public class EditorInputManager
    {
        public EditorInputConfig InputConfig { get; }
        public EditScreen Screen { get; }
        private EditScreenView View { get; }

        private Dictionary<Keybind, HashSet<KeybindActions>> KeybindDictionary;
        private GenericKeyState PreviousKeyState;

        private const int HoldRepeatActionDelay = 250;
        private const int HoldRepeatActionInterval = 25;
        private Dictionary<KeybindActions, long> LastActionPress = new Dictionary<KeybindActions, long>();
        private Dictionary<KeybindActions, long> LastActionTime = new Dictionary<KeybindActions, long>();

        private static HashSet<KeybindActions> HoldRepeatActions = new HashSet<KeybindActions>()
        {
            KeybindActions.ZoomIn,
            KeybindActions.ZoomInLarge,
            KeybindActions.ZoomOut,
            KeybindActions.ZoomOutLarge,
            KeybindActions.SeekForwards,
            KeybindActions.SeekForwardsAndSelect,
            KeybindActions.SeekForwardsAndMove,
            KeybindActions.SeekBackwards,
            KeybindActions.SeekBackwardsAndSelect,
            KeybindActions.SeekBackwardsAndMove,
            KeybindActions.SeekForwardsLarge,
            KeybindActions.SeekBackwardsLarge,
            KeybindActions.SeekForwards1ms,
            KeybindActions.SeekBackwards1ms,
            KeybindActions.IncreasePlaybackRate,
            KeybindActions.DecreasePlaybackRate,
            KeybindActions.ChangeToolUp,
            KeybindActions.ChangeToolDown,
        };

        private static HashSet<KeybindActions> HoldAndReleaseActions = new HashSet<KeybindActions>()
        {
        };

        private static HashSet<KeybindActions> EnabledActionsDuringGameplayPreview = new HashSet<KeybindActions>()
        {
        };

        private static Dictionary<KeybindActions, Bindable<bool>> InvertScrollingActions = new();

        public EditorInputManager(EditScreen screen)
        {
            InputConfig = EditorInputConfig.LoadFromConfig();
            KeybindDictionary = InputConfig.ReverseDictionary(InvertScrollingActions);
            PreviousKeyState = new GenericKeyState(GenericKeyManager.GetPressedKeys());
            Screen = screen;
            View = (EditScreenView)screen.View;

            ConstructInvertScrollingActions();
            Screen.InvertBeatSnapScroll.ValueChanged += InvertScrollingValueChanged;
            ConfigManager.InvertEditorScrolling.ValueChanged += InvertScrollingValueChanged;
        }

        private void ConstructInvertScrollingActions()
        {
            InvertScrollingActions = new Dictionary<KeybindActions, Bindable<bool>>
            {
                [KeybindActions.IncreaseSnap] = Screen.InvertBeatSnapScroll,
                [KeybindActions.DecreaseSnap] = Screen.InvertBeatSnapScroll,
                [KeybindActions.SeekForwards] = ConfigManager.InvertEditorScrolling,
                [KeybindActions.SeekForwardsLarge] = ConfigManager.InvertEditorScrolling,
                [KeybindActions.SeekForwards1ms] = ConfigManager.InvertEditorScrolling,
                [KeybindActions.SeekBackwards] = ConfigManager.InvertEditorScrolling,
                [KeybindActions.SeekBackwardsLarge] = ConfigManager.InvertEditorScrolling,
                [KeybindActions.SeekBackwards1ms] = ConfigManager.InvertEditorScrolling
            };
        }

        private void InvertScrollingValueChanged(object sender, BindableValueChangedEventArgs<bool> e)
        {
            KeybindDictionary = InputConfig.ReverseDictionary(InvertScrollingActions);
        }

        public void HandleInput()
        {
            if (DialogManager.Dialogs.Count != 0) return;
            if (View.IsImGuiHovered) return;

            HandleKeyPresses();
            HandleKeyReleasesAfterHoldAction();
            HandlePluginKeyPresses();

            HandleMouseInputs();
        }

        private void HandleKeyPresses()
        {
            var keyState = new GenericKeyState(GenericKeyManager.GetPressedKeys());
            var uniqueKeyPresses = keyState.UniqueKeyPresses(PreviousKeyState);
            var allMatchedActions = new Dictionary<Keybind, HashSet<KeybindActions>>();
            foreach (var pressedKeybind in keyState.PressedKeybinds())
            {
                if (!KeybindDictionary.TryGetValue(pressedKeybind, out var actions)) continue;

                allMatchedActions.Add(pressedKeybind, actions);

                foreach (var action in actions)
                {
                    if (uniqueKeyPresses.Contains(pressedKeybind))
                    {
                        HandleAction(action);
                        LastActionPress[action] = GameBase.Game.TimeRunning;
                    }
                    else if (CanRepeat(action))
                        HandleAction(action);
                    else if (CanHold(action) || pressedKeybind.Key.ScrollDirection != null)
                        HandleAction(action, false);
                }
            }
            HandleActionCombination(allMatchedActions, uniqueKeyPresses);

            PreviousKeyState = keyState;
        }

        private void HandleActionCombination(Dictionary<Keybind, HashSet<KeybindActions>> actions, HashSet<Keybind> uniqueKeyPresses)
        {
            HandleSwapLane(actions, uniqueKeyPresses);
        }

        private void HandleSwapLane(Dictionary<Keybind, HashSet<KeybindActions>> keybindActions, HashSet<Keybind> uniqueKeyPresses)
        {
            var heldLane = -1;
            var uniquePressLane = -1;
            var success = false;
            foreach (var (keybind, actionsSet) in keybindActions)
            {
                foreach (var action in actionsSet)
                {
                    if (!action.HasFlag(KeybindActions.SwapNoteAtLane))
                        continue;

                    var lane = (int)(action ^ KeybindActions.SwapNoteAtLane);
                    if (lane > Screen.WorkingMap.GetKeyCount())
                        continue;

                    if (uniqueKeyPresses.Contains(keybind))
                    {
                        uniquePressLane = lane;
                        // If two actions are uniquely pressed at the same time, both will be registered
                        if (heldLane == -1)
                            heldLane = uniquePressLane;
                    }
                    else
                    {
                        heldLane = lane;
                    }

                    if (uniquePressLane == -1 || heldLane == -1 || heldLane == uniquePressLane)
                        continue;

                    success = true;
                    break;
                }

                if (success) break;
            }

            if (!success)
                return;

            Screen.SwapSelectedObjects(heldLane, uniquePressLane);
        }

        private void HandleKeyReleasesAfterHoldAction()
        {
            foreach (var action in HoldAndReleaseActions)
            {
                var binds = InputConfig.GetOrDefault(action);
                if (binds.IsNotBound()) continue;

                if (binds.IsUniqueRelease())
                    HandleAction(action, false, true);
            }
        }

        private long TimeSinceLastPress(KeybindActions action) => GameBase.Game.TimeRunning - LastActionPress.GetValueOrDefault(action, GameBase.Game.TimeRunning);
        private long TimeSinceLastAction(KeybindActions action) => GameBase.Game.TimeRunning - LastActionTime.GetValueOrDefault(action, GameBase.Game.TimeRunning);

        private bool CanRepeat(KeybindActions action)
        {
            if (!HoldRepeatActions.Contains(action))
                return false;

            return TimeSinceLastAction(action) > HoldRepeatActionInterval && TimeSinceLastPress(action) > HoldRepeatActionDelay;
        }

        private bool CanHold(KeybindActions action)
        {
            if (!HoldAndReleaseActions.Contains(action))
                return false;

            return TimeSinceLastAction(action) > HoldRepeatActionInterval;
        }

        private void HandleAction(KeybindActions action, bool isKeyPress = true, bool isRelease = false)
        {
            switch (action)
            {
                case KeybindActions.ExitEditor:
                    Screen.LeaveEditor();
                    break;
                case KeybindActions.PlayPause:
                    Screen.TogglePlayPause();
                    break;
                case KeybindActions.ZoomIn:
                    Screen.AdjustZoom(1);
                    break;
                case KeybindActions.ZoomInLarge:
                    Screen.AdjustZoom(5);
                    break;
                case KeybindActions.ZoomOut:
                    Screen.AdjustZoom(-1);
                    break;
                case KeybindActions.ZoomOutLarge:
                    Screen.AdjustZoom(-5);
                    break;
                case KeybindActions.SeekForwards:
                    Screen.SeekInDirection(Direction.Forward);
                    break;
                case KeybindActions.SeekForwardsAndSelect:
                    Screen.SeekInDirection(Direction.Forward, enableSelection: true);
                    break;
                case KeybindActions.SeekForwardsAndMove:
                    Screen.SeekInDirection(Direction.Forward, enableMoving: true);
                    break;
                case KeybindActions.SeekBackwards:
                    Screen.SeekInDirection(Direction.Backward);
                    break;
                case KeybindActions.SeekBackwardsAndSelect:
                    Screen.SeekInDirection(Direction.Backward, enableSelection: true);
                    break;
                case KeybindActions.SeekBackwardsAndMove:
                    Screen.SeekInDirection(Direction.Backward, enableMoving: true);
                    break;
                case KeybindActions.SeekForwardsLarge:
                    Screen.SeekInDirection(Direction.Forward, 0.25f);
                    break;
                case KeybindActions.SeekBackwardsLarge:
                    Screen.SeekInDirection(Direction.Backward, 0.25f);
                    break;
                case KeybindActions.SeekForwards1ms:
                    if (!Screen.Track.IsPlaying)
                        Screen.SeekTo(Screen.Track.Time + 1);
                    break;
                case KeybindActions.SeekBackwards1ms:
                    if (!Screen.Track.IsPlaying)
                        Screen.SeekTo(Screen.Track.Time - 1);
                    break;
                case KeybindActions.SeekForwards1msAndMove:
                    if (!Screen.Track.IsPlaying)
                        Screen.SeekTo(Screen.Track.Time + 1, enableMoving: true);
                    break;
                case KeybindActions.SeekBackwards1msAndMove:
                    if (!Screen.Track.IsPlaying)
                        Screen.SeekTo(Screen.Track.Time - 1, enableMoving: true);
                    break;
                case KeybindActions.SeekForwards1msAndSelect:
                    if (!Screen.Track.IsPlaying)
                        Screen.SeekTo(Screen.Track.Time + 1, enableSelection: true);
                    break;
                case KeybindActions.SeekBackwards1msAndSelect:
                    if (!Screen.Track.IsPlaying)
                        Screen.SeekTo(Screen.Track.Time - 1, enableSelection: true);
                    break;
                case KeybindActions.SeekToStartOfSelection:
                    Screen.SeekToStartOfSelection();
                    break;
                case KeybindActions.SeekToEndOfSelection:
                    Screen.SeekToEndOfSelection();
                    break;
                case KeybindActions.SeekToStartOfSelectionAndSelect:
                    Screen.SeekToStartOfSelection(true);
                    break;
                case KeybindActions.SeekToEndOfSelectionAndSelect:
                    Screen.SeekToEndOfSelection(true);
                    break;
                case KeybindActions.SeekToStart:
                    Screen.SeekToStart();
                    break;
                case KeybindActions.SeekToEnd:
                    Screen.SeekToEnd();
                    break;
                case KeybindActions.SeekToStartAndSelect:
                    Screen.SeekToStart(true);
                    break;
                case KeybindActions.SeekToEndAndSelect:
                    Screen.SeekToEnd(true);
                    break;
                case KeybindActions.IncreasePlaybackRate:
                    Screen.ChangeAudioPlaybackRate(Direction.Forward);
                    break;
                case KeybindActions.DecreasePlaybackRate:
                    Screen.ChangeAudioPlaybackRate(Direction.Backward);
                    break;
                case KeybindActions.SetPreviewTime:
                    Screen.ActionManager.SetPreviewTime((int) Screen.Track.Time);
                    break;
                case KeybindActions.ChangeToolUp:
                    Screen.ChangeTool(Direction.Backward);
                    break;
                case KeybindActions.ChangeToolDown:
                    Screen.ChangeTool(Direction.Forward);
                    break;
                case KeybindActions.ChangeToolToSelect:
                    Screen.ChangeToolTo(EditorCompositionTool.Select);
                    break;
                case KeybindActions.ChangeToolToNote:
                    Screen.ChangeToolTo(EditorCompositionTool.Note);
                    break;
                case KeybindActions.ChangeToolToLongNote:
                    Screen.ChangeToolTo(EditorCompositionTool.LongNote);
                    break;
                case KeybindActions.IncreaseSnap:
                    Screen.ChangeBeatSnap(Direction.Forward);
                    break;
                case KeybindActions.DecreaseSnap:
                    Screen.ChangeBeatSnap(Direction.Backward);
                    break;
                case KeybindActions.ChangeSnapTo1:
                    Screen.BeatSnap.Value = 1;
                    break;
                case KeybindActions.ChangeSnapTo2:
                    Screen.BeatSnap.Value = 2;
                    break;
                case KeybindActions.ChangeSnapTo3:
                    Screen.BeatSnap.Value = 3;
                    break;
                case KeybindActions.ChangeSnapTo4:
                    Screen.BeatSnap.Value = 4;
                    break;
                case KeybindActions.ChangeSnapTo5:
                    Screen.BeatSnap.Value = 5;
                    break;
                case KeybindActions.ChangeSnapTo6:
                    Screen.BeatSnap.Value = 6;
                    break;
                case KeybindActions.ChangeSnapTo7:
                    Screen.BeatSnap.Value = 7;
                    break;
                case KeybindActions.ChangeSnapTo8:
                    Screen.BeatSnap.Value = 8;
                    break;
                case KeybindActions.ChangeSnapTo9:
                    Screen.BeatSnap.Value = 9;
                    break;
                case KeybindActions.ChangeSnapTo10:
                    Screen.BeatSnap.Value = 10;
                    break;
                case KeybindActions.ChangeSnapTo12:
                    Screen.BeatSnap.Value = 12;
                    break;
                case KeybindActions.ChangeSnapTo16:
                    Screen.BeatSnap.Value = 16;
                    break;
                case KeybindActions.OpenCustomSnapDialog:
                    Screen.OpenCustomSnapDialog();
                    break;
                case KeybindActions.OpenMetadataDialog:
                    Screen.OpenMetadataDialog();
                    break;
                case KeybindActions.OpenModifiersDialog:
                    Screen.OpenModifiersDialog();
                    break;
                case KeybindActions.OpenQuaFile:
                    try
                    {
                        Utils.NativeUtils.OpenNatively($"{ConfigManager.SongDirectory.Value}/{Screen.Map.Directory}/{Screen.Map.Path}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, LogType.Runtime);
                    }
                    break;
                case KeybindActions.OpenFolder:
                    try
                    {
                        Utils.NativeUtils.OpenNatively($"{ConfigManager.SongDirectory.Value}/{Screen.Map.Directory}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, LogType.Runtime);
                    }
                    break;
                case KeybindActions.CreateNewDifficulty:
                    Screen.CreateNewDifficulty(false);
                    break;
                case KeybindActions.CreateNewDifficultyFromCurrent:
                    Screen.CreateNewDifficulty();
                    break;
                case KeybindActions.Export:
                    Screen.ExportToZip();
                    break;
                case KeybindActions.Upload:
                    Screen.UploadMapset();
                    break;
                case KeybindActions.UploadAndSubmitForRank:
                    Screen.UploadMapset();
                    Screen.SubmitForRank();
                    break;
                case KeybindActions.ToggleBpmPanel:
                    Screen.ToggleBuiltinPlugin(EditorBuiltInPlugin.TimingPointEditor);
                    break;
                case KeybindActions.ToggleSvPanel:
                    Screen.ToggleBuiltinPlugin(EditorBuiltInPlugin.ScrollVelocityEditor);
                    break;
                case KeybindActions.ToggleAutoMod:
                    View.AutoMod.IsActive.Value = !View.AutoMod.IsActive.Value;
                    break;
                case KeybindActions.ToggleGotoPanel:
                    Screen.ToggleBuiltinPlugin(EditorBuiltInPlugin.GoToObjects);
                    break;
                case KeybindActions.ToggleGameplayPreview:
                    Screen.DisplayGameplayPreview.Value = !Screen.DisplayGameplayPreview.Value;
                    break;
                case KeybindActions.ToggleHitsounds:
                    Screen.EnableHitsounds.Value = !Screen.EnableHitsounds.Value;
                    break;
                case KeybindActions.ToggleMetronome:
                    Screen.EnableMetronome.Value = !Screen.EnableMetronome.Value;
                    break;
                case KeybindActions.TogglePitchRate:
                    ConfigManager.Pitched.Value = !ConfigManager.Pitched.Value;
                    break;
                case KeybindActions.ToggleWaveform:
                    Screen.ShowWaveform.Value = !Screen.ShowWaveform.Value;
                    break;
                case KeybindActions.ToggleWaveformLowPass:
                    Screen.WaveformFilter.Value = Screen.WaveformFilter.Value == EditorPlayfieldWaveformFilter.LowPass
                        ? EditorPlayfieldWaveformFilter.None
                        : EditorPlayfieldWaveformFilter.LowPass;
                    break;
                case KeybindActions.ToggleWaveformHighPass:
                    Screen.WaveformFilter.Value = Screen.WaveformFilter.Value == EditorPlayfieldWaveformFilter.HighPass
                        ? EditorPlayfieldWaveformFilter.None
                        : EditorPlayfieldWaveformFilter.HighPass;
                    break;
                case KeybindActions.ToggleReferenceDifficulty:
                    if (Screen.UneditableMap.Value != null)
                        Screen.UneditableMap.Value = null;
                    else
                        Screen.ShowReferenceDifficulty();
                    break;
                case KeybindActions.NextReferenceDifficulty:
                    Screen.ReferenceDifficultyIndex.Value++;
                    break;
                case KeybindActions.PreviousReferenceDifficulty:
                    Screen.ReferenceDifficultyIndex.Value--;
                    break;
                case KeybindActions.PlayTest:
                    Screen.ExitToTestPlay();
                    break;
                case KeybindActions.PlayTestFromBeginning:
                    Screen.ExitToTestPlay(true);
                    break;
                case KeybindActions.ToggleLayerViewMode:
                    Screen.ToggleViewLayers();
                    break;
                case KeybindActions.ChangeSelectedLayerUp:
                    Screen.ChangeSelectedLayer(Direction.Backward);
                    break;
                case KeybindActions.ChangeSelectedLayerDown:
                    Screen.ChangeSelectedLayer(Direction.Forward);
                    break;
                case KeybindActions.ToggleCurrentLayerVisibility:
                    Screen.ToggleSelectedLayerVisibility();
                    break;
                case KeybindActions.ToggleAllLayersVisibility:
                    Screen.ToggleAllLayerVisibility();
                    break;
                case KeybindActions.MoveSelectedNotesToCurrentLayer:
                    Screen.MoveSelectedNotesToCurrentLayer();
                    break;
                case KeybindActions.CreateNewLayer:
                    Screen.AddNewLayer();
                    break;
                case KeybindActions.DeleteCurrentLayer:
                    Screen.DeleteLayer();
                    break;
                case KeybindActions.RenameCurrentLayer:
                    Screen.RenameLayer();
                    break;
                case KeybindActions.RecolorCurrentLayer:
                    Screen.RecolorLayer();
                    break;
                case KeybindActions.Undo:
                    Screen.ActionManager.Undo();
                    break;
                case KeybindActions.Redo:
                    Screen.ActionManager.Redo();
                    break;
                case KeybindActions.Copy:
                    Screen.CopySelectedObjects();
                    break;
                case KeybindActions.Paste:
                    Screen.PasteCopiedObjects(false);
                    break;
                case KeybindActions.SelectNotesAtCurrentTime:
                    Screen.SelectObjectsAtCurrentTime();
                    break;
                case KeybindActions.SelectAllNotes:
                    Screen.SelectAllObjects();
                    break;
                case KeybindActions.SelectAllNotesInLayer:
                    Screen.SelectAllObjectsInLayer();
                    break;
                case KeybindActions.MirrorNotesLeftRight:
                    Screen.FlipSelectedObjects();
                    break;
                case KeybindActions.Deselect:
                    Screen.SelectedHitObjects.Clear();
                    break;
                case KeybindActions.Cut:
                    Screen.CutSelectedObjects();
                    break;
                case KeybindActions.DeleteSelection:
                    Screen.DeleteSelectedObjects();
                    break;
                case KeybindActions.Save:
                    Screen.Save();
                    break;
                case KeybindActions.ApplyOffsetToMap:
                    DialogManager.Show(new EditorApplyOffsetDialog(Screen));
                    break;
                case KeybindActions.ResnapToCurrentBeatSnap:
                    Screen.ActionManager.ResnapNotes(new List<int> { Screen.BeatSnap.Value }, Screen.SelectedHitObjects.Value);
                    break;
                case KeybindActions.PlaceNoteAtLane1:
                case KeybindActions.PlaceNoteAtLane2:
                case KeybindActions.PlaceNoteAtLane3:
                case KeybindActions.PlaceNoteAtLane4:
                case KeybindActions.PlaceNoteAtLane5:
                case KeybindActions.PlaceNoteAtLane6:
                case KeybindActions.PlaceNoteAtLane7:
                case KeybindActions.PlaceNoteAtLane8:
                case KeybindActions.PlaceNoteAtLane9:
                case KeybindActions.PlaceNoteAtLane10:
                    var lane = (int)(action ^ KeybindActions.PlaceNoteAtLane);
                    if (lane <= Screen.WorkingMap.GetKeyCount())
                        Screen.PlaceOrRemoveHitObjectAtCurrentTime(lane);
                    break;
                case KeybindActions.PlaceNoteAtLane:
                case KeybindActions.SwapNoteAtLane:
                case KeybindActions.SwapNoteAtLane1:
                case KeybindActions.SwapNoteAtLane2:
                case KeybindActions.SwapNoteAtLane3:
                case KeybindActions.SwapNoteAtLane4:
                case KeybindActions.SwapNoteAtLane5:
                case KeybindActions.SwapNoteAtLane6:
                case KeybindActions.SwapNoteAtLane7:
                case KeybindActions.SwapNoteAtLane8:
                case KeybindActions.SwapNoteAtLane9:
                case KeybindActions.SwapNoteAtLane10:
                default:
                    return;
            }

            LastActionTime[action] = GameBase.Game.TimeRunning;
        }

        ~EditorInputManager()
        {
            ConfigManager.InvertEditorScrolling.ValueChanged -= InvertScrollingValueChanged;
            if (Screen?.InvertBeatSnapScroll != null)
                Screen.InvertBeatSnapScroll.ValueChanged -= InvertScrollingValueChanged;
        }

        private void HandleMouseInputs()
        {
        }

        private void HandlePluginKeyPresses()
        {
            foreach (var (pluginName, keybinds) in InputConfig.PluginKeybinds)
            {
                if (keybinds.IsUniquePress())
                {
                    // Toggle plugin
                    foreach (var plugin in Screen.Plugins)
                    {
                        if (plugin.Name != pluginName)
                            continue;

                        Screen.TogglePlugin(plugin);
                    }
                }
            }
        }
    }
}