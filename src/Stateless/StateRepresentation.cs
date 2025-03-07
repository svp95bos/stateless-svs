﻿namespace Stateless;

public partial class StateMachine<TState, TTrigger>
{
    internal partial class StateRepresentation
    {
        readonly TState _state;

        internal IDictionary<TTrigger, ICollection<TriggerBehaviour>> TriggerBehaviours { get; } = new Dictionary<TTrigger, ICollection<TriggerBehaviour>>();
        internal ICollection<EntryActionBehavior> EntryActions { get; } = new List<EntryActionBehavior>();
        internal ICollection<ExitActionBehavior> ExitActions { get; } = new List<ExitActionBehavior>();
        internal ICollection<ActivateActionBehaviour> ActivateActions { get; } = new List<ActivateActionBehaviour>();
        internal ICollection<DeactivateActionBehaviour> DeactivateActions { get; } = new List<DeactivateActionBehaviour>();

        StateRepresentation _superstate; // null

        readonly ICollection<StateRepresentation> _substates = new List<StateRepresentation>();
        public TState InitialTransitionTarget { get; private set; } = default;

        public StateRepresentation(TState state)
        {
            _state = state;
        }

        internal ICollection<StateRepresentation> GetSubstates() => _substates;

        public bool CanHandle(TTrigger trigger, params object[] args) => TryFindHandler(trigger, args, out TriggerBehaviourResult _);

        public bool CanHandle(TTrigger trigger, object[] args, out ICollection<string> unmetGuards)
        {
            bool handlerFound = TryFindHandler(trigger, args, out TriggerBehaviourResult result);
            unmetGuards = result?.UnmetGuardConditions;
            return handlerFound;
        }

        public bool TryFindHandler(TTrigger trigger, object[] args, out TriggerBehaviourResult handler)
        {
            TriggerBehaviourResult superStateHandler = null;

            bool handlerFound = (TryFindLocalHandler(trigger, args, out TriggerBehaviourResult localHandler) ||
                                (Superstate != null && Superstate.TryFindHandler(trigger, args, out superStateHandler)));

            // If no handler for super state, replace by local handler (see issue #398)
            handler = superStateHandler ?? localHandler;

            return handlerFound;
        }

        private bool TryFindLocalHandler(TTrigger trigger, object[] args, out TriggerBehaviourResult handlerResult)
        {
            // Get list of candidate trigger handlers
            if (!TriggerBehaviours.TryGetValue(trigger, out ICollection<TriggerBehaviour> possible))
            {
                handlerResult = null;
                return false;
            }

            // Guard functions are executed here
            TriggerBehaviourResult[] actual = possible
                .Select(h => new TriggerBehaviourResult(h, h.UnmetGuardConditions(args)))
                .ToArray();

            // Find a handler for the trigger
            handlerResult = TryFindLocalHandlerResult(trigger, actual)
                ?? TryFindLocalHandlerResultWithUnmetGuardConditions(actual);

            if (handlerResult == null)
            {
                return false;
            }

            return !handlerResult.UnmetGuardConditions.Any();
        }

        private TriggerBehaviourResult TryFindLocalHandlerResult(TTrigger trigger, IEnumerable<TriggerBehaviourResult> results)
        {
            List<TriggerBehaviourResult> actual = results
                .Where(r => !r.UnmetGuardConditions.Any())
                .ToList();

            if (actual.Count <= 1)
            {
                return actual.FirstOrDefault();
            }

            string message = string.Format(StateRepresentationResources.MultipleTransitionsPermitted, trigger, _state);
            throw new InvalidOperationException(message);
        }

        private static TriggerBehaviourResult TryFindLocalHandlerResultWithUnmetGuardConditions(IEnumerable<TriggerBehaviourResult> results)
        {
            TriggerBehaviourResult result = results.FirstOrDefault(r => r.UnmetGuardConditions.Any());

            if (result != null)
            {
                IEnumerable<string> unmetConditions = results.Where(r => r.UnmetGuardConditions.Any())
                                             .SelectMany(r => r.UnmetGuardConditions);

                // Add other unmet conditions to first result
                foreach (string condition in unmetConditions)
                {
                    if (!result.UnmetGuardConditions.Contains(condition))
                    {
                        result.UnmetGuardConditions.Add(condition);
                    }
                }
            }

            return result;
        }

        public void AddActivateAction(Action action, Reflection.InvocationInfo activateActionDescription) => ActivateActions.Add(new ActivateActionBehaviour.Sync(_state, action, activateActionDescription));

        public void AddDeactivateAction(Action action, Reflection.InvocationInfo deactivateActionDescription) => DeactivateActions.Add(new DeactivateActionBehaviour.Sync(_state, action, deactivateActionDescription));

        public void AddEntryAction(TTrigger trigger, Action<Transition, object[]> action, Reflection.InvocationInfo entryActionDescription) => EntryActions.Add(new EntryActionBehavior.SyncFrom<TTrigger>(trigger, action, entryActionDescription));

        public void AddEntryAction(Action<Transition, object[]> action, Reflection.InvocationInfo entryActionDescription) => EntryActions.Add(new EntryActionBehavior.Sync(action, entryActionDescription));

        public void AddExitAction(Action<Transition> action, Reflection.InvocationInfo exitActionDescription) => ExitActions.Add(new ExitActionBehavior.Sync(action, exitActionDescription));

        public void Activate()
        {
            if (_superstate != null)
            {
                _superstate.Activate();
            }

            ExecuteActivationActions();
        }

        public void Deactivate()
        {
            ExecuteDeactivationActions();

            if (_superstate != null)
            {
                _superstate.Deactivate();
            }
        }

        void ExecuteActivationActions()
        {
            foreach (ActivateActionBehaviour action in ActivateActions)
            {
                action.Execute();
            }
        }

        void ExecuteDeactivationActions()
        {
            foreach (DeactivateActionBehaviour action in DeactivateActions)
            {
                action.Execute();
            }
        }

        public void Enter(Transition transition, params object[] entryArgs)
        {
            if (transition.IsReentry)
            {
                ExecuteEntryActions(transition, entryArgs);
            }
            else if (!Includes(transition.Source))
            {
                if (_superstate != null && transition is not InitialTransition)
                {
                    _superstate.Enter(transition, entryArgs);
                }

                ExecuteEntryActions(transition, entryArgs);
            }
        }

        public Transition Exit(Transition transition)
        {
            if (transition.IsReentry)
            {
                ExecuteExitActions(transition);
            }
            else if (!Includes(transition.Destination))
            {
                ExecuteExitActions(transition);

                // Must check if there is a superstate, and if we are leaving that superstate
                if (_superstate != null)
                {
                    // Check if destination is within the state list
                    if (IsIncludedIn(transition.Destination))
                    {
                        // Destination state is within the list, exit first superstate only if it is NOT the the first
                        if (!_superstate.UnderlyingState.Equals(transition.Destination))
                        {
                            return _superstate.Exit(transition);
                        }
                    }
                    else
                    {
                        // Exit the superstate as well
                        return _superstate.Exit(transition);
                    }
                }
            }
            return transition;
        }

        void ExecuteEntryActions(Transition transition, object[] entryArgs)
        {
            foreach (EntryActionBehavior action in EntryActions)
            {
                action.Execute(transition, entryArgs);
            }
        }

        void ExecuteExitActions(Transition transition)
        {
            foreach (ExitActionBehavior action in ExitActions)
            {
                action.Execute(transition);
            }
        }
        internal void InternalAction(Transition transition, object[] args)
        {
            InternalTriggerBehaviour.Sync internalTransition = null;

            // Look for actions in superstate(s) recursivly until we hit the topmost superstate, or we actually find some trigger handlers.
            StateRepresentation aStateRep = this;
            while (aStateRep != null)
            {
                if (aStateRep.TryFindLocalHandler(transition.Trigger, args, out TriggerBehaviourResult result))
                {
                    // Trigger handler found in this state
                    if (result.Handler is InternalTriggerBehaviour.Async)
                    {
                        throw new InvalidOperationException("Running Async internal actions in synchronous mode is not allowed");
                    }

                    internalTransition = result.Handler as InternalTriggerBehaviour.Sync;
                    break;
                }
                // Try to look for trigger handlers in superstate (if it exists)
                aStateRep = aStateRep._superstate;
            }

            // Execute internal transition event handler
            if (internalTransition == null)
            {
                throw new ArgumentNullException("The configuration is incorrect, no action assigned to this internal transition.");
            }

            internalTransition.InternalAction(transition, args);
        }
        public void AddTriggerBehaviour(TriggerBehaviour triggerBehaviour)
        {
            if (!TriggerBehaviours.TryGetValue(triggerBehaviour.Trigger, out ICollection<TriggerBehaviour> allowed))
            {
                allowed = new List<TriggerBehaviour>();
                TriggerBehaviours.Add(triggerBehaviour.Trigger, allowed);
            }
            allowed.Add(triggerBehaviour);
        }

        public StateRepresentation Superstate
        {
            get => _superstate;
            set => _superstate = value;
        }

        public TState UnderlyingState => _state;

        public void AddSubstate(StateRepresentation substate) => _substates.Add(substate);

        /// <summary>
        /// Checks if the state is in the set of this state or its sub-states
        /// </summary>
        /// <param name="state">The state to check</param>
        /// <returns>True if included</returns>
        public bool Includes(TState state) => _state.Equals(state) || _substates.Any(s => s.Includes(state));

        /// <summary>
        /// Checks if the state is in the set of this state or a super-state
        /// </summary>
        /// <param name="state">The state to check</param>
        /// <returns>True if included</returns>
        public bool IsIncludedIn(TState state)
        {
            return
                _state.Equals(state) ||
                (_superstate != null && _superstate.IsIncludedIn(state));
        }

        public IEnumerable<TTrigger> PermittedTriggers => GetPermittedTriggers();

        public IEnumerable<TTrigger> GetPermittedTriggers(params object[] args)
        {
            IEnumerable<TTrigger> result = TriggerBehaviours
                .Where(t => t.Value.Any(a => !a.UnmetGuardConditions(args).Any()))
                .Select(t => t.Key);

            if (Superstate != null)
            {
                result = result.Union(Superstate.GetPermittedTriggers(args));
            }

            return result;
        }

        internal void SetInitialTransition(TState state)
        {
            InitialTransitionTarget = state;
            HasInitialTransition = true;
        }
        public bool HasInitialTransition { get; private set; }
    }
}
