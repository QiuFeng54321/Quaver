using System.Collections.Generic;
using Quaver.API.Maps;
using Quaver.API.Maps.Structures;
using Quaver.Shared.Screens.Edit.Actions.SV.RemoveBatch;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Quaver.API.Helpers;

namespace Quaver.Shared.Screens.Edit.Actions.SV.AddBatch
{
    [MoonSharpUserData]
    public class EditorActionAddScrollVelocityBatch : IEditorAction
    {
        public EditorActionType Type { get; } = EditorActionType.AddScrollVelocityBatch;

        private EditorActionManager ActionManager { get; }

        private Qua WorkingMap { get; }

        public List<SliderVelocityInfo> ScrollVelocities { get; }

        [MoonSharpVisible(false)]
        public EditorActionAddScrollVelocityBatch(EditorActionManager manager, Qua workingMap, List<SliderVelocityInfo> svs)
        {
            ActionManager = manager;
            WorkingMap = workingMap;
            ScrollVelocities = svs;
        }

        [MoonSharpVisible(false)]
        public void Perform()
        {
            WorkingMap.SliderVelocities.InsertSorted(ScrollVelocities);
            ActionManager.TriggerEvent(Type, new EditorScrollVelocityBatchAddedEventArgs(ScrollVelocities));
        }

        [MoonSharpVisible(false)]
        public void Undo() => new EditorActionRemoveScrollVelocityBatch(ActionManager, WorkingMap, ScrollVelocities).Perform();
    }
}
