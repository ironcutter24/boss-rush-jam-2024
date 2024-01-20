using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

public class StateMachine<TState>
{
    public event Action<string> StateChanged;

    internal enum Event { Entry, Process, Exit }
    private Event stateEvent = Event.Entry;
    private Dictionary<TState, State> states = new Dictionary<TState, State>();

    public State CurrentState { get; private set; }
    private State NextState { get; set; }


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

    (int from, int to) depth = (int.MaxValue, int.MaxValue);
    public void Process()
    {
        if (stateEvent == Event.Entry)
        {
            StateChanged?.Invoke(CurrentState.FormatParents());
            CurrentState.PerformOnEntry(depth.to);
        }

        CurrentState.PerformOnProcess();

        NextState = CurrentState.PerformTransitionCheck();
        if (NextState != null)
        {
            State.HaveSharedParent(CurrentState, NextState, out depth);
            GD.Print($"Depth: ({depth.from}, {depth.to})");
            CurrentState.PerformOnExit(depth.from);
            stateEvent = Event.Exit;
            CurrentState = NextState;
        }

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

        internal void PerformOnEntry(int depth)
        {
            if (depth > 1)
                parent?.PerformOnEntry(depth - 1);

            onEntryCallback?.Invoke();
        }

        internal void PerformOnProcess()
        {
            parent?.PerformOnProcess();
            onProcessCallback?.Invoke();
        }

        internal State PerformTransitionCheck()
        {
            foreach (var pair in transitions)
            {
                if (pair.Value.Invoke()) return pair.Key;
            }
            return parent?.PerformTransitionCheck();
        }

        internal void PerformOnExit(int depth)
        {
            onExitCallback?.Invoke();
            if (depth > 1)
                parent?.PerformOnExit(depth - 1);
        }

        public static bool HaveSharedParent(State a, State b, out (int from, int to) depth)
        {
            State aParent = a;
            State bParent = b;
            depth = (-1, -1);

            while (aParent != null)
            {
                bParent = b;
                depth.to = 0;
                depth.from++;

                while (bParent != null)
                {
                    if (aParent == bParent)
                    {
                        return true; // Found a common ancestor
                    }
                    bParent = bParent.parent;
                    depth.to++;
                }
                aParent = aParent.parent;
            }

            // Reset depth values to their maximum if no common ancestor is found
            depth = (int.MaxValue, int.MaxValue);
            return false;
        }


        internal string FormatParents()
        {
            return ((parent != null) ? $"{parent.FormatParents()} -> " : "") + Key.ToString();
        }

        private Action onEntryCallback;
        public State OnEntry(Action entryAction)
        {
            onEntryCallback = entryAction;
            return this;
        }

        private Action onProcessCallback;
        public State OnProcess(Action processAction)
        {
            onProcessCallback = processAction;
            return this;
        }

        private Action onExitCallback;
        public State OnExit(Action exitAction)
        {
            onExitCallback = exitAction;
            return this;
        }

        #endregion

    }
}
