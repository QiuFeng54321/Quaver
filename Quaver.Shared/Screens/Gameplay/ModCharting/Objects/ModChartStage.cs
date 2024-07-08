using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Quaver.Shared.Assets;
using Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Gameplay.ModCharting.Objects;

[MoonSharpUserData]
public class ModChartStage : ModChartGlobalVariable
{
    public ModChartStage(ElementAccessShortcut shortcut) : base(shortcut)
    {
    }

    public GameplayPlayfieldLane LaneContainer(int lane) =>
        Shortcut.GameplayPlayfieldKeysStage.LaneContainers[lane - 1];

    public Sprite BgMask => Shortcut.GameplayPlayfieldKeysStage.BgMask;
    public Sprite Background => Shortcut.GameplayScreenView.Background;
    public Container ForegroundContainer => Shortcut.GameplayPlayfieldKeys.ForegroundContainer;
    public Container PlayfieldContainer => Shortcut.GameplayPlayfieldKeys.Container;


    /// <summary>
    ///     Width of lane (receptor alone)
    /// </summary>
    /// <returns></returns>
    public float LaneSize => Shortcut.GameplayPlayfieldKeys.LaneSize;

    /// <summary>
    ///     Padding of receptor
    /// </summary>
    /// <returns></returns>
    public float ReceptorPadding => Shortcut.GameplayPlayfieldKeys.ReceptorPadding;

    /// <summary>
    ///     Separation between lanes
    /// </summary>
    /// <returns></returns>
    public float LaneSeparationWidth => LaneSize + ReceptorPadding;

    public float HitObjectFallRotation(int lane) => Shortcut.GameplayPlayfieldKeys.HitObjectFallRotation[lane - 1];

    public void HitObjectFallRotation(int lane, float rotationRad) =>
        Shortcut.GameplayPlayfieldKeys.HitObjectFallRotation[lane - 1] = rotationRad;

    /// <summary>
    ///     Positions of each receptor
    /// </summary>
    /// <returns>Scalable vector (x, y, scale_x, scale_y) for each receptor</returns>
    public ScalableVector2[] GetLanePositions()
    {
        var positions = new ScalableVector2[Shortcut.GameplayScreen.Map.GetKeyCount()];
        for (var i = 0; i < Shortcut.GameplayScreen.Map.GetKeyCount(); i++)
        {
            positions[i] = Shortcut.GameplayPlayfieldKeysStage.LaneContainers[i].Position;
        }

        return positions;
    }

    /// <summary>
    ///     Sets the position of a receptor of a particular lane
    /// </summary>
    /// <param name="lane"></param>
    /// <param name="pos"></param>
    public void SetLanePosition(int lane, ScalableVector2 pos)
    {
        Shortcut.GameplayPlayfieldKeysStage.LaneContainers[lane - 1].Position = pos;
    }
}