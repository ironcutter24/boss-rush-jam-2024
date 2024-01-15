using System;
using System.Collections;
using System.Collections.Generic;

public class StateMachine<TState>
{
    private Dictionary<TState, State> states = new Dictionary<TState, State>();

    public State CurrentState { get; private set; }

    internal enum Event { Entry, Process, Exit }
    private Event stateEvent = Event.Entry;

    public StateMachine(TState entryState)
    {
        CurrentState = Configure(entryState);
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
            state = new State(this, stateKey);
            states.Add(stateKey, state);
            return state;
        }
    }

    public void Process()
    {
        CurrentState = CurrentState.Process(ref stateEvent);

        if (stateEvent == Event.Entry) { stateEvent = Event.Process; }
        if (stateEvent == Event.Exit) { stateEvent = Event.Entry; }
    }

    public class State
    {
        internal Dictionary<State, Func<bool>> transitions = new Dictionary<State, Func<bool>>();
        private State parent;

        public StateMachine<TState> OwnerMachine { get; private set; }
        public TState Key { get; private set; }

        public State(StateMachine<TState> owner, TState stateKey)
        {
            OwnerMachine = owner;
            Key = stateKey;
        }

        internal State Process(ref Event stateEvent)
        {
            if (stateEvent == Event.Entry)
            {
                onEntry?.Invoke();
            }

            onProcess?.Invoke();

            foreach (var t in transitions)
            {
                if (t.Value.Invoke())
                {
                    onExit?.Invoke();
                    stateEvent = Event.Exit;
                    return t.Key;
                }
            }

            return this;
        }

        #region State Configuration

        public State SubstateOf(TState parentState)
        {
            parent = OwnerMachine.Configure(parentState);
            return this;
        }

        public State AddTransition(TState nextState, Func<bool> condition)
        {
            State nextStateInstance = OwnerMachine.Configure(nextState);
            Func<bool> existingCondition;
            if (transitions.TryGetValue(nextStateInstance, out existingCondition))
            {
                transitions[nextStateInstance] = () => existingCondition() || condition();
            }
            else
            {
                transitions.Add(nextStateInstance, condition);
            }
            return this;
        }

        #endregion

        #region State Actions

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

        #endregion

    }
}
