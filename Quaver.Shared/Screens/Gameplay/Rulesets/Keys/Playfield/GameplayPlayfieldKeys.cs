/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Shared.Config;
using Quaver.Shared.Skinning;
using System;
using System.Linq;
using Wobble;
using Wobble.Graphics;
using Wobble.Window;

namespace Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield
{
    public class GameplayPlayfieldKeys : IGameplayPlayfield
    {
        /// <summary>
        ///     Reference to the current gameplay screen.
        /// </summary>
        public GameplayScreen Screen { get; }

        /// <summary>
        ///     Reference to the ruleset for the playfield.
        /// </summary>
        public GameplayRulesetKeys Ruleset { get; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public Container Container { get; set; }

        /// <summary>
        ///     The foreground container will be drawn in this layer
        /// </summary>
        public Layer GameplayForegroundLayer { get; private set; }

        /// <summary>
        ///     The background container will be drawn in this layer
        /// </summary>
        public Layer GameplayBackgroundLayer { get; private set; }

        /// <summary>
        ///     Shortcut to container's layer manager
        /// </summary>
        public LayerManager GameplayLayerManager => ((LayeredContainer)Container)?.LayerManager;

        /// <summary>
        ///     The background of the playfield.
        /// </summary>
        public Container BackgroundContainer { get; private set; }

        /// <summary>
        ///     The foreground of the playfield.
        /// </summary>
        public Container ForegroundContainer { get; private set; }

        /// <summary>
        ///     The stage of the playfield.
        /// </summary>
        public GameplayPlayfieldKeysStage Stage { get; private set; }

        /// <summary>
        ///     The X size of the playfield.
        /// </summary>
        public float Width
        {
            get
            {
                var skin = SkinManager.Skin.Keys[Screen.Map.Mode];
                var padding = Padding * 2 - ReceptorPadding;
                var width = (LaneSize + ReceptorPadding) * Screen.Map.GetKeyCount(false) + padding;

                if (Screen.Map.HasScratchKey)
                {
                    var size = skin.ScratchLaneSize <= 0 ? LaneSize : skin.ScratchLaneSize;
                    width += size + ReceptorPadding;
                }

                return width;
            }
        }

        /// <summary>
        ///     Padding of the playfield.
        /// </summary>
        public float Padding
        {
            get
            {
                if (Screen.IsSongSelectPreview)
                    return 0;

                return SkinManager.Skin.Keys[Screen.Map.Mode].StageReceptorPadding;
            }
        }

        /// <summary>
        ///     The width of the entire playfield for song select previews
        /// </summary>
        public static float PREVIEW_PLAYFIELD_WIDTH
        {
            get
            {
                var game = GameBase.Game as QuaverGame;

                if (game?.CurrentScreen?.Type == QuaverScreenType.Editor)
                    return 400;

                return 420;
            }
        }

        /// <summary>
        ///     The size of the each lane.
        /// </summary>
        public float LaneSize
        {
            get
            {
                // Use skin's ColumnSize if it fits inside the preview, otherwise scale down.
                var previewWidth = PREVIEW_PLAYFIELD_WIDTH / Screen.Map.GetKeyCount();
                
                var columnWidth = SkinManager.Skin.Keys[Screen.Map.Mode].ColumnSize * WindowManager.BaseToVirtualRatio;
                
                if (Screen.IsSongSelectPreview && previewWidth < columnWidth)
                    return previewWidth;

                return columnWidth;
            }
        }

        /// <summary>
        ///     Padding of the receptor.
        /// </summary>
        internal float ReceptorPadding
        {
            get
            {
                if (Screen.IsSongSelectPreview)
                    return 0;

                return SkinManager.Skin.Keys[Screen.Map.Mode].NotePadding;
            }
        }

        /// <summary>
        ///     Position for each Receptor in each lane from the top of the screen.
        /// </summary>
        internal float[] ReceptorPositionY { get; private set; }

        /// <summary>
        ///     Direction in which the hit objects fall towards the receptor, in radians
        /// </summary>
        internal float[] HitObjectFallRotation { get; private set; }

        /// <summary>
        ///     Position for each Column Lighting relative from the top of the screen.
        /// </summary>
        internal float[] ColumnLightingPositionY { get; private set; }

        /// <summary>
        ///     HitObject Target Position from the relative top of the screen.
        /// </summary>
        internal float[] HitPositionY { get; private set; }

        /// <summary>
        ///     HeldHitObject Target Position relative from the top of the screen.
        /// </summary>
        internal float[] HoldHitPositionY { get; private set; }

        /// <summary>
        ///     LN end Target Position relative from the top of the screen.
        /// </summary>
        internal float[] HoldEndHitPositionY { get; private set; }

        /// <summary>
        ///     Position for each Timing Line relative from the top of the screen.
        /// </summary>
        internal float[] TimingLinePositionY { get; private set; }

        /// <summary>
        ///     Size Adjustment for each LongNote in specific lane so the LN EndTime snaps with StartTime.
        /// </summary>
        internal float[] LongNoteSizeAdjustment { get; private set; }

        /// <summary>
        ///     Determines the scroll direction of each lane
        /// </summary>
        public ScrollDirection[] ScrollDirections { get; private set; }

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="ruleset"></param>
        public GameplayPlayfieldKeys(GameplayScreen screen, GameplayRulesetKeys ruleset)
        {
            Screen = screen;
            Ruleset = ruleset;
            Container = new LayeredContainer();
            InitializeLayers();
            SetLaneScrollDirections();
            SetReferencePositions();
            CreateElementContainers();
        }

        private void InitializeLayers()
        {
            GameplayForegroundLayer = GameplayLayerManager.NewLayer($"Foreground", Screen);
            GameplayBackgroundLayer = GameplayLayerManager.NewLayer($"Background", Screen);
            LayerManager.RequireOrder(new []
            {
                GameplayLayerManager.TopLayer,
                GameplayForegroundLayer,
                GameplayBackgroundLayer,
                GameplayLayerManager.DefaultLayer
            });
        }


        /// <summary>
        ///     Create Foreground and Background Containers, as well as the Stage.
        /// </summary>
        private void CreateElementContainers()
        {
            // Create background container
            BackgroundContainer = new Container
            {
                Parent = Container,
                Size = new ScalableVector2(Width, WindowManager.Height),
                Alignment = Alignment.TopCenter,
                X = SkinManager.Skin.Keys[Screen.Map.Mode].ColumnAlignment,
                Layer = GameplayBackgroundLayer
            };

            // Create the foreground container.
            ForegroundContainer = new Container
            {
                Parent = Container,
                Size = new ScalableVector2(Width, WindowManager.Height),
                Alignment = Alignment.TopCenter,
                X = SkinManager.Skin.Keys[Screen.Map.Mode].ColumnAlignment,
                Layer = GameplayForegroundLayer
            };

            Stage = new GameplayPlayfieldKeysStage(Screen, this);
        }

        /// <summary>
        ///     Returns Array of Scroll Directions for each specific lane for a specific GameMode.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private void SetLaneScrollDirections()
        {
            var keys = Ruleset.Screen.Map?.GetKeyCount() ?? 4;

            ScrollDirection direction;
            switch (Ruleset.Map.Mode)
            {
                case GameMode.Keys4:
                    direction = ConfigManager.ScrollDirection4K.Value;
                    break;
                case GameMode.Keys7:
                    direction = ConfigManager.ScrollDirection7K.Value;
                    break;
                default:
                    throw new Exception("Map Mode does not exist.");
            }

            // Case: Config = Split Scroll
            if (direction.Equals(ScrollDirection.Split))
            {
                var halfIndex = (int)Math.Ceiling(keys / 2.0);
                ScrollDirections = new ScrollDirection[keys];
                for (var i = 0; i < keys; i++)
                {
                    if (i >= halfIndex)
                        ScrollDirections[i] = ScrollDirection.Up;
                    else
                        ScrollDirections[i] = ScrollDirection.Down;
                }
                return;
            }

            // Case: Config = Down/Up Scroll
            ScrollDirections = Enumerable.Repeat(direction, keys).ToArray();
        }

        /// <summary>
        ///     Set Positions for Receptor, Column Lighting, Hit Position, and Timing Lines
        /// </summary>
        private void SetReferencePositions()
        {
            var skin = SkinManager.Skin.Keys[Screen.Map.Mode];
            ReceptorPositionY = new float[ScrollDirections.Length];
            HitObjectFallRotation = new float[ScrollDirections.Length];
            ColumnLightingPositionY = new float[ScrollDirections.Length];
            HitPositionY = new float[ScrollDirections.Length];
            HoldHitPositionY = new float[ScrollDirections.Length];
            HoldEndHitPositionY = new float[ScrollDirections.Length];
            TimingLinePositionY = new float[ScrollDirections.Length];
            LongNoteSizeAdjustment = new float[ScrollDirections.Length];

            var defaultLaneSize = skin.WidthForNoteHeightScale > 0 ? skin.WidthForNoteHeightScale : LaneSize;

            for (var i = 0; i < ScrollDirections.Length; i++)
            {
                var hitObOffset = defaultLaneSize * skin.NoteHitObjects[i][0].Height / skin.NoteHitObjects[i][0].Width;
                var holdHitObOffset = defaultLaneSize * skin.NoteHoldHitObjects[i][0].Height / skin.NoteHoldHitObjects[i][0].Width;
                var holdEndOffset = LaneSize * skin.NoteHoldEnds[i].Height / skin.NoteHoldEnds[i].Width;
                var receptorOffset = LaneSize * skin.NoteReceptorsUp[i].Height / skin.NoteReceptorsUp[i].Width;

                if (SkinManager.Skin.Keys[Screen.Map.Mode].DrawLongNoteEnd)
                    LongNoteSizeAdjustment[i] = (holdHitObOffset - holdEndOffset) / 2;
                else
                    LongNoteSizeAdjustment[i] = holdHitObOffset / 2;

                var hitPosOffsetY = skin.HitPosOffsetY;

                if (Ruleset.Screen.IsSongSelectPreview)
                    hitPosOffsetY *= LaneSize / skin.ColumnSize;
                else
                    hitPosOffsetY *= WindowManager.BaseToVirtualRatio;

                switch (ScrollDirections[i])
                {
                    case ScrollDirection.Down:
                        ReceptorPositionY[i] = WindowManager.Height - skin.ReceptorPosOffsetY - receptorOffset;
                        ColumnLightingPositionY[i] = ReceptorPositionY[i] - skin.ColumnLightingOffsetY - skin.ColumnLightingScale * LaneSize * skin.ColumnLighting.Height / skin.ColumnLighting.Width;
                        HitPositionY[i] = ReceptorPositionY[i] + hitPosOffsetY - hitObOffset;
                        HoldHitPositionY[i] = ReceptorPositionY[i] + hitPosOffsetY - holdHitObOffset;
                        HoldEndHitPositionY[i] = ReceptorPositionY[i] + hitPosOffsetY - holdEndOffset;
                        TimingLinePositionY[i] = ReceptorPositionY[i] + hitPosOffsetY;
                        break;
                    case ScrollDirection.Up:
                        ReceptorPositionY[i] = skin.ReceptorPosOffsetY;
                        HitPositionY[i] = ReceptorPositionY[i] - hitPosOffsetY + receptorOffset;
                        HoldHitPositionY[i] = ReceptorPositionY[i] - hitPosOffsetY + receptorOffset;
                        HoldEndHitPositionY[i] = ReceptorPositionY[i] - hitPosOffsetY + receptorOffset;
                        ColumnLightingPositionY[i] = ReceptorPositionY[i] + receptorOffset + skin.ColumnLightingOffsetY;
                        TimingLinePositionY[i] = HitPositionY[i];
                        break;
                    default:
                        throw new Exception($"Scroll Direction in current lane index {i} does not exist.");
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            Stage.Update(gameTime);
            Container?.Update(gameTime);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime) => Container.Draw(gameTime);

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Destroy() => Container?.Destroy();

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public void HandleFailure(GameTime gameTime)
        {
        }
    }
}
