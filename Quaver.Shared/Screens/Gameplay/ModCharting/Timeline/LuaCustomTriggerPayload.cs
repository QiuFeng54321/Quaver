using MoonSharp.Interpreter;

namespace Quaver.Shared.Screens.Gameplay.ModCharting.Timeline;

public class LuaCustomTriggerPayload: ITriggerPayload
{
    public Closure TriggerClosure { get; set; }
    public Closure UndoClosure { get; set; }

    public LuaCustomTriggerPayload(Closure triggerClosure, Closure undoClosure)
    {
        TriggerClosure = triggerClosure;
        UndoClosure = undoClosure;
    }

    public void Trigger(ValueVertex<ITriggerPayload> valueVertex)
    {
        TriggerClosure?.SafeCall(valueVertex);
    }

    public void Undo(ValueVertex<ITriggerPayload> valueVertex)
    {
        UndoClosure?.SafeCall(valueVertex);
    }
}