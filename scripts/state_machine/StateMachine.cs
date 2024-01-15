using System;
using System.Collections;
using System.Collections.Generic;

public class StateMachine<TState>
{
    private Dictionary<TState, State> states = new Dictionary<TState, State>();

    public State CurrentState { get; private set; }

    public void Init(TState enterState)
    {
        CurrentState = states[enterState];
    }

    public State Configure(TState stateKey)
    {
        State state;
        if (states.TryGetValue(stateKey, out state))
        {
            return state;
        }
        else
        {
            state = new State(stateKey);
            states.Add(stateKey, state);
            return state;
        }
    }

    public void Process()
    {
        var s = CurrentState.Process();
        CurrentState = states[s];
    }

    public class State
    {
        internal Dictionary<TState, Func<bool>> transitions = new Dictionary<TState, Func<bool>>();
        private bool isFirstProcess = true;

        public TState Key { get; private set; }

        public State(TState stateKey)
        {
            Key = stateKey;
        }

        public State AddTransition(TState nextState, Func<bool> condition)
        {
            Func<bool> existingCondition;
            if (transitions.TryGetValue(nextState, out existingCondition))
            {
                transitions[nextState] = () => existingCondition() || condition();
            }
            else
            {
                transitions.Add(nextState, condition);
            }
            return this;
        }

        internal TState Process()
        {
            if (isFirstProcess)
            {
                onEntry?.Invoke();
                isFirstProcess = false;
            }

            onProcess?.Invoke();

            foreach (var t in transitions)
            {
                if (t.Value.Invoke())
                {
                    onExit?.Invoke();
                    isFirstProcess = true;
                    return t.Key;
                }
            }

            return Key;
        }

        Action onEntry;
        public State OnEntry(Action entryAction)
        {
            onEntry = entryAction;
            return this;
        }

        Action onProcess;
        public State OnProcess(Action processAction)
        {
            onProcess = processAction;
            return this;
        }

        Action onExit;
        public State OnExit(Action exitAction)
        {
            onExit = exitAction;
            return this;
        }
    }
}
