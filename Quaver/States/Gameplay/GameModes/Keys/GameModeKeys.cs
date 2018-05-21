﻿using System;
using System.ComponentModel;
using Quaver.API.Enums;
using Quaver.API.Maps;
using Quaver.API.Maps.Processors.Scoring;
using Quaver.Input;
using Quaver.Logging;
using Quaver.Main;
using Quaver.States.Gameplay.GameModes.Keys.Input;
using Quaver.States.Gameplay.GameModes.Keys.Playfield;
using Quaver.States.Gameplay.HitObjects;

namespace Quaver.States.Gameplay.GameModes.Keys
{
    internal class GameModeKeys : GameModeRuleset
    {
        /// <inheritdoc />
        /// <summary>
        ///     The input manager for this ruleset.
        /// </summary>
        protected sealed override IGameplayInputManager InputManager { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        internal sealed override ScoreProcessor ScoreProcessor { get; set; }

        /// <summary>
        ///     Reference to the actual gameplay screen.
        /// </summary>
        private GameplayScreen Screen { get; }
        
        /// <inheritdoc />
        /// <summary>
        ///     Ctor - Sets the correct mode, either 4 or 7k.
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="mode"></param>
        /// <param name="map"></param>
        public GameModeKeys(GameplayScreen screen, GameMode mode, Qua map): base(screen, map)
        {
            Screen = screen;
            
            switch (mode)
            {
                case GameMode.Keys4:
                case GameMode.Keys7:
                    Mode = mode;
                    break;
                default:
                    throw new InvalidEnumArgumentException("GameModeKeys can only be initialized with GameMode.Keys4 or GameModes.Keys7");
            }

            // Initialize the score processor.
            ScoreProcessor = new ScoreProcessorKeys(map, GameBase.CurrentMods);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        protected override HitObject CreateHitObject(HitObjectInfo info)
        {
            var playfield = (KeysPlayfield)Playfield;
            var objectManager = (KeysHitObjectManager) HitObjectManager;
            
            // Create the new HitObject.
            var hitObject = new KeysHitObject(this, info)
            {
                Width = playfield.LaneSize,
                OffsetYFromReceptor = info.StartTime
            };

            // Calculate position & offset from the receptor.
            // TODO: Handle SV's.
            hitObject.PositionY = hitObject.OffsetYFromReceptor + playfield.Stage.HitPositionOffset;
            
            // Get Note Snapping
            if (GameBase.LoadedSkin.ColourObjectsBySnapDistance)
                hitObject.SnapIndex = hitObject.GetBeatSnap(hitObject.GetTimingPoint(Map.TimingPoints)); 
            
            // Disregard non-long note objects after this point, so we can initailize them separately.
            if (!hitObject.IsLongNote) 
                return hitObject;
            
            // TODO: Handle SV's.
            hitObject.LongNoteOffsetYFromReceptor = info.EndTime;
            
            hitObject.InitialLongNoteSize = (ulong)((hitObject.LongNoteOffsetYFromReceptor - hitObject.OffsetYFromReceptor) * KeysHitObjectManager.ScrollSpeed);
            hitObject.CurrentLongNoteSize = hitObject.InitialLongNoteSize;

            return hitObject;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        protected override void CreatePlayfield()
        {
            Playfield = new KeysPlayfield(Screen);
            Logger.LogSuccess("Playfield Initialized", LogType.Runtime);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        protected override IGameplayInputManager CreateInputManager()
        {
            return new KeysInputManager(this, Mode);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        protected override HitObjectManager CreateHitObjectManager() => new KeysHitObjectManager(this, 255);
    }
}