using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Scores;
using Quaver.Shared.Screens.Results;
using Wobble.Screens;

namespace Quaver.Shared.Screens.Tests.Results
{
    public class TestResultsScreenView : ScreenView
    {
        private ResultsScreen Results { get; }

        private Map TestMap { get; } = new Map
        {
            Artist = "HyuN",
            Title = "Princess of Winter",
            Creator = "Example21",
            DifficultyName = "Insane",
            Difficulty10X = 18.86f,
            Difficulty12X = 20.32f,
        };

        private Score TestScore { get; } = new Score()
        {
            Name = "ExampleUser86",
            DateTime = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
            Accuracy = 93.42,
            MaxCombo = 1234,
            TotalScore = 1000000,
            CountMarv = 1771,
            CountPerf = 787,
            CountGreat = 134,
            CountGood = 13,
            CountOkay = 4,
            CountMiss = 45,
            Mods = (long)(ModIdentifier.Speed12X | ModIdentifier.Mirror | ModIdentifier.NoLongNotes | ModIdentifier.NoFail),
            JudgementWindowMarv = 16,
            JudgementWindowPerf = 43,
            JudgementWindowGreat = 76,
            JudgementWindowGood = 106,
            JudgementWindowOkay = 127,
            JudgementWindowMiss = 164,
        };

        public TestResultsScreenView(Screen screen) : base(screen)
        {
            Results = new ResultsScreen(TestMap, TestScore);
        }

        public override void Update(GameTime gameTime) => Results?.Update(gameTime);

        public override void Draw(GameTime gameTime) => Results?.Draw(gameTime);

        public override void Destroy() => Results?.Destroy();
    }
}