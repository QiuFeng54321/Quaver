﻿using System;

namespace Quaver.Shared.Screens.Edit.Input
{
    [Serializable]
    [Flags]
    public enum KeybindActions
    {
        ExitEditor,
        PlayPause,
        ZoomIn,
        ZoomInLarge,
        ZoomOut,
        ZoomOutLarge,
        SeekForwards,
        SeekForwardsAndSelect,
        SeekForwardsAndMove,
        SeekBackwards,
        SeekBackwardsAndSelect,
        SeekBackwardsAndMove,
        SeekForwardsLarge,
        SeekBackwardsLarge,
        SeekForwards1ms,
        SeekBackwards1ms,
        SeekForwards1msAndMove,
        SeekBackwards1msAndMove,
        SeekForwards1msAndSelect,
        SeekBackwards1msAndSelect,
        SeekToStartOfSelection,
        SeekToEndOfSelection,
        SeekToStartOfSelectionAndSelect,
        SeekToEndOfSelectionAndSelect,
        SeekToStart,
        SeekToEnd,
        SeekToStartAndSelect,
        SeekToEndAndSelect,
        IncreasePlaybackRate,
        DecreasePlaybackRate,
        SetPreviewTime,
        ChangeToolUp,
        ChangeToolDown,
        ChangeToolToSelect,
        ChangeToolToNote,
        ChangeToolToLongNote,
        IncreaseSnap,
        DecreaseSnap,
        ChangeSnapTo1,
        ChangeSnapTo2,
        ChangeSnapTo3,
        ChangeSnapTo4,
        ChangeSnapTo5,
        ChangeSnapTo6,
        ChangeSnapTo7,
        ChangeSnapTo8,
        ChangeSnapTo9,
        ChangeSnapTo10,
        ChangeSnapTo12,
        ChangeSnapTo16,
        OpenCustomSnapDialog,
        OpenMetadataDialog,
        OpenModifiersDialog,
        OpenQuaFile,
        OpenFolder,
        CreateNewDifficulty,
        CreateNewDifficultyFromCurrent,
        Export,
        Upload,
        UploadAndSubmitForRank,
        ToggleBpmPanel,
        ToggleSvPanel,
        ToggleAutoMod,
        ToggleGotoPanel,
        ToggleLayerViewMode,
        ToggleGameplayPreview,
        ToggleHitsounds,
        ToggleMetronome,
        TogglePitchRate,
        ToggleWaveform,
        ToggleWaveformLowPass,
        ToggleWaveformHighPass,
        ToggleReferenceDifficulty,
        NextReferenceDifficulty,
        PreviousReferenceDifficulty,
        PlayTest,
        PlayTestFromBeginning,
        ChangeSelectedLayerUp,
        ChangeSelectedLayerDown,
        ToggleCurrentLayerVisibility,
        ToggleAllLayersVisibility,
        MoveSelectedNotesToCurrentLayer,
        CreateNewLayer,
        DeleteCurrentLayer,
        RenameCurrentLayer,
        RecolorCurrentLayer,
        Undo,
        Redo,
        Cut,
        DeleteSelection,
        Copy,
        Paste,
        SelectNotesAtCurrentTime,
        SelectAllNotes,
        SelectAllNotesInLayer,
        MirrorNotesLeftRight,
        Deselect,
        Save,
        ApplyOffsetToMap,
        ResnapToCurrentBeatSnap,
        // This can save us from repetitive work
        PlaceNoteAtLane = 1 << 16,
        PlaceNoteAtLane1,
        PlaceNoteAtLane2,
        PlaceNoteAtLane3,
        PlaceNoteAtLane4,
        PlaceNoteAtLane5,
        PlaceNoteAtLane6,
        PlaceNoteAtLane7,
        PlaceNoteAtLane8,
        PlaceNoteAtLane9,
        PlaceNoteAtLane10,
        SwapNoteAtLane = 1 << 17,
        SwapNoteAtLane1,
        SwapNoteAtLane2,
        SwapNoteAtLane3,
        SwapNoteAtLane4,
        SwapNoteAtLane5,
        SwapNoteAtLane6,
        SwapNoteAtLane7,
        SwapNoteAtLane8,
        SwapNoteAtLane9,
        SwapNoteAtLane10
    }
}