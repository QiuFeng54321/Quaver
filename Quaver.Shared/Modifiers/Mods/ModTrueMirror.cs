using System;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Shared.Helpers;

namespace Quaver.Shared.Modifiers.Mods
{
    public class ModTrueMirror : IGameplayModifier
    {
        public string Name { get; set; } = "True Mirror";

        public ModIdentifier ModIdentifier { get; set; } = ModIdentifier.TrueMirror;

        public ModType Type { get; set; } = ModType.DifficultyIncrease;

        public string Description { get; set; } = "The map doesn't get mirrored, YOU do.";

        public bool Ranked() => false;

        public bool AllowedInMultiplayer { get; set; } = false;

        public bool OnlyMultiplayerHostCanCanChange { get; set; } = false;

        public bool ChangesMapObjects { get; set; } = false;

        public ModIdentifier[] IncompatibleMods { get; set; } = Array.Empty<ModIdentifier>();

        public Color ModColor { get; } = ColorHelper.HexToColor("#1F1E33");

        public void InitializeMod()
        {
        }
    }
}