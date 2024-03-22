using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Storyboard.StateMachine;

[MoonSharpUserData]
public class StoryboardStateMachine
{
    private readonly List<IStateMachineState> _states = new();

    public const int IdleStateId = -1;
    public int CurrentStateId { get; private set; } = IdleStateId;
    
    public void ChangeState(int id)
    {
        if (CurrentStateId != IdleStateId)
        {
            _states[CurrentStateId].OnDisable();
        }

        if (id == IdleStateId)
        {
            CurrentStateId = IdleStateId;
            return;
        }

        if (id >= _states.Count || id < 0) throw new ArgumentOutOfRangeException(nameof(id), $"No state has the id {id}");
        var state = _states[id];
        CurrentStateId = id;
        state.OnEnable();
    }

    public void ExitToGlobal()
    {
        ChangeState(IdleStateId);
    }

    public int RegisterState(IStateMachineState state)
    {
        state.Id = _states.Count;
        _states.Add(state);
        state.OnInitialize();
        state.OnEnable();
        return state.Id;
    }
    
    public int RegisterState(Closure onInitialize, Closure updater,  Closure onEnable,
        Closure onDisable)
    {
        var state = new LuaStateMachineState(onInitialize, updater, onEnable, onDisable);
        return RegisterState(state);
    }

    public void Update()
    {
        if (CurrentStateId == IdleStateId) return;
        var state = _states[CurrentStateId];
        var nextStateId = state.OnUpdate();
        if (nextStateId == CurrentStateId) return;
        ChangeState(nextStateId);
    }
}