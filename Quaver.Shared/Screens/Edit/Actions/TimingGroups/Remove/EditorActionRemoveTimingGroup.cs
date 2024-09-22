using System.Collections.Generic;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Quaver.API.Maps;
using Quaver.API.Maps.Structures;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.SetTimingGroupBatch;
using Quaver.Shared.Screens.Edit.Actions.SV.Remove;
using Quaver.Shared.Screens.Edit.Actions.TimingGroups.Add;

namespace Quaver.Shared.Screens.Edit.Actions.TimingGroups.Remove
{
    [MoonSharpUserData]
    public class EditorActionRemoveTimingGroup : IEditorAction
    {
        public EditorActionType Type { get; } = EditorActionType.RemoveTimingGroup;

        private EditorActionManager ActionManager { get; }

        private Qua WorkingMap { get; }

        public string Id { get; }

        public TimingGroup TimingGroup { get; }

        public List<HitObjectInfo> ChildHitObjects { get; private set; }

        [MoonSharpVisible(false)]
        public EditorActionRemoveTimingGroup(EditorActionManager manager, Qua workingMap, string id,
            TimingGroup timingGroup, List<HitObjectInfo> childHitObjects)
        {
            ActionManager = manager;
            WorkingMap = workingMap;
            TimingGroup = timingGroup;
            ChildHitObjects = childHitObjects;
            Id = id;
        }

        [MoonSharpVisible(false)]
        public void Perform()
        {
            WorkingMap.TimingGroups.Remove(Id);

            if (ChildHitObjects == null)
            {
                ChildHitObjects = new List<HitObjectInfo>();
                foreach (var hitObject in WorkingMap.HitObjects)
                {
                    if (hitObject.TimingGroup != Id)
                        continue;

                    ChildHitObjects.Add(hitObject);
                }
            }

            ActionManager.TriggerEvent(Type, new EditorTimingGroupRemovedEventArgs(Id, TimingGroup, ChildHitObjects));
            new EditorActionSetTimingGroupBatch(ActionManager, WorkingMap, ChildHitObjects, Qua.GlobalScrollGroupId)
                .Perform();
        }

        [MoonSharpVisible(false)]
        public void Undo()
        {
            new EditorActionAddTimingGroup(ActionManager, WorkingMap, Id, TimingGroup, ChildHitObjects).Perform();
        }
    }
}