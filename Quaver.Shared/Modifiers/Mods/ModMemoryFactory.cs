using System;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Shared.Helpers;

namespace Quaver.Shared.Modifiers.Mods
{
    public class ModMemoryFactory : IGameplayModifier
    {
        public string Name { get; set; } = "Memory Factory";

        public ModIdentifier ModIdentifier { get; set; } = ModIdentifier.MemoryFactory;

        public ModType Type { get; set; } = ModType.DifficultyIncrease;

        public string Description { get; set; } = "hi";

        public bool Ranked() => false;

        public bool AllowedInMultiplayer { get; set; } = false;

        public bool OnlyMultiplayerHostCanCanChange { get; set; } = false;

        public bool ChangesMapObjects { get; set; } = false;

        public ModIdentifier[] IncompatibleMods { get; set; } = Array.Empty<ModIdentifier>();

        public Color ModColor { get; } = ColorHelper.HexToColor("#123456");

        public void InitializeMod()
        {
        }
    }
}