using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Stateless.Tests;

public class StateMachineFixture
{
    const string
        StateA = "A", StateB = "B", StateC = "C",
        TriggerX = "X", TriggerY = "Y";

    int _numCalls = 0;

    void CountCalls() => _numCalls++;

    [Fact]
    public void CanUseReferenceTypeMarkers()
    {
        RunSimpleTest(
            new[] { StateA, StateB, StateC },
            new[] { TriggerX, TriggerY });
    }

    [Fact]
    public void CanUseValueTypeMarkers()
    {
        RunSimpleTest(
            Enum.GetValues(typeof(State)).Cast<State>(),
            Enum.GetValues(typeof(Trigger)).Cast<Trigger>());
    }

    static void RunSimpleTest<TState, TTransition>(IEnumerable<TState> states, IEnumerable<TTransition> transitions)
    {
        TState a = states.First();
        TState b = states.Skip(1).First();
        TTransition x = transitions.First();

        StateMachine<TState, TTransition> sm = new(a);

        sm.Configure(a)
            .Permit(x, b);

        sm.Fire(x);

        Assert.Equal(b, sm.State);
    }

    [Fact]
    public void InitialStateIsCurrent()
    {
        State initial = State.B;
        StateMachine<State, Trigger> sm = new(initial);
        Assert.Equal(initial, sm.State);
    }

    [Fact]
    public void StateCanBeStoredExternally()
    {
        State state = State.B;
        StateMachine<State, Trigger> sm = new(() => state, s => state = s);
        sm.Configure(State.B).Permit(Trigger.X, State.C);
        Assert.Equal(State.B, sm.State);
        Assert.Equal(State.B, state);
        sm.Fire(Trigger.X);
        Assert.Equal(State.C, sm.State);
        Assert.Equal(State.C, state);
    }

    [Fact]
    public void StateMutatorShouldBeCalledOnlyOnce()
    {
        State state = State.B;
        int count = 0;
        StateMachine<State, Trigger> sm = new(() => state, (s) => { state = s; count++; });
        sm.Configure(State.B).Permit(Trigger.X, State.C);
        sm.Fire(Trigger.X);
        Assert.Equal(1, count);
    }

    [Fact]
    public void SubstateIsIncludedInCurrentState()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        sm.Configure(State.B).SubstateOf(State.C);

        Assert.Equal(State.B, sm.State);
        Assert.True(sm.IsInState(State.C));
    }

    [Fact]
    public void WhenInSubstate_TriggerIgnoredInSuperstate_RemainsInSubstate()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.B)
            .SubstateOf(State.C);

        sm.Configure(State.C)
            .Ignore(Trigger.X);

        sm.Fire(Trigger.X);

        Assert.Equal(State.B, sm.State);
    }

    [Fact]
    public void PermittedTriggersIncludeSuperstatePermittedTriggers()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.A)
            .Permit(Trigger.Z, State.B);

        sm.Configure(State.B)
            .SubstateOf(State.C)
            .Permit(Trigger.X, State.A);

        sm.Configure(State.C)
            .Permit(Trigger.Y, State.A);

        IEnumerable<Trigger> permitted = sm.GetPermittedTriggers();

        Assert.True(permitted.Contains(Trigger.X));
        Assert.True(permitted.Contains(Trigger.Y));
        Assert.False(permitted.Contains(Trigger.Z));
    }

    [Fact]
    public void PermittedTriggersAreDistinctValues()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.B)
            .SubstateOf(State.C)
            .Permit(Trigger.X, State.A);

        sm.Configure(State.C)
            .Permit(Trigger.X, State.B);

        IEnumerable<Trigger> permitted = sm.GetPermittedTriggers();
        Assert.Equal(1, permitted.Count());
        Assert.Equal(Trigger.X, permitted.First());
    }

    [Fact]
    public void AcceptedTriggersRespectGuards()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.B)
            .PermitIf(Trigger.X, State.A, () => false);

        Assert.Equal(0, sm.GetPermittedTriggers().Count());
    }

    [Fact]
    public void AcceptedTriggersRespectMultipleGuards()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.B)
            .PermitIf(Trigger.X, State.A,
                new Tuple<Func<bool>, string>(() => true, "1"),
                new Tuple<Func<bool>, string>(() => false, "2"));

        Assert.Equal(0, sm.GetPermittedTriggers().Count());
    }

    [Fact]
    public void WhenDiscriminatedByGuard_ChoosesPermitedTransition()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.B)
            .PermitIf(Trigger.X, State.A, () => false)
            .PermitIf(Trigger.X, State.C, () => true);

        sm.Fire(Trigger.X);

        Assert.Equal(State.C, sm.State);
    }

    [Fact]
    public void WhenDiscriminatedByMultiConditionGuard_ChoosesPermitedTransition()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.B)
            .PermitIf(Trigger.X, State.A,
                new Tuple<Func<bool>, string>(() => true, "1"),
                new Tuple<Func<bool>, string>(() => false, "2"))
            .PermitIf(Trigger.X, State.C,
                new Tuple<Func<bool>, string>(() => true, "1"),
                new Tuple<Func<bool>, string>(() => true, "2"));

        sm.Fire(Trigger.X);

        Assert.Equal(State.C, sm.State);
    }

    [Fact]
    public void WhenTriggerIsIgnored_ActionsNotExecuted()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        bool fired = false;

        sm.Configure(State.B)
            .OnEntry(t => fired = true)
            .Ignore(Trigger.X);

        sm.Fire(Trigger.X);

        Assert.False(fired);
    }

    [Fact]
    public void IfSelfTransitionPermited_ActionsFire()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        bool fired = false;

        sm.Configure(State.B)
            .OnEntry(t => fired = true)
            .PermitReentry(Trigger.X);

        sm.Fire(Trigger.X);

        Assert.True(fired);
    }

    [Fact]
    public void ImplicitReentryIsDisallowed()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        Assert.Throws<ArgumentException>(() => sm.Configure(State.B)
           .Permit(Trigger.X, State.B));
    }

    [Fact]
    public void TriggerParametersAreImmutableOnceSet()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        sm.SetTriggerParameters<string, int>(Trigger.X);
        Assert.Throws<InvalidOperationException>(() => sm.SetTriggerParameters<string>(Trigger.X));
    }

    [Fact]
    public void ExceptionThrownForInvalidTransition()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
        Assert.Equal(exception.Message, "No valid leaving transitions are permitted from state 'A' for trigger 'X'. Consider ignoring the trigger.");
    }

    [Fact]
    public void ExceptionThrownForInvalidTransitionMentionsGuardDescriptionIfPresent()
    {
        // If guard description is empty then method name of guard is used
        // so I have skipped empty description test.
        const string guardDescription = "test";

        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).PermitIf(Trigger.X, State.B, () => false, guardDescription);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
        Assert.Equal(typeof(InvalidOperationException), exception.GetType());
    }

    [Fact]
    public void ExceptionThrownForInvalidTransitionMentionsMultiGuardGuardDescriptionIfPresent()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).PermitIf(Trigger.X, State.B,
            new Tuple<Func<bool>, string>(() => false, "test1"),
            new Tuple<Func<bool>, string>(() => false, "test2"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
        Assert.Equal(typeof(InvalidOperationException), exception.GetType());
    }

    [Fact]
    public void ParametersSuppliedToFireArePassedToEntryAction()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        StateMachine<State, Trigger>.TriggerWithParameters<string, int> x = sm.SetTriggerParameters<string, int>(Trigger.X);

        sm.Configure(State.B)
            .Permit(Trigger.X, State.C);

        string entryArgS = null;
        int entryArgI = 0;

        sm.Configure(State.C)
            .OnEntryFrom(x, (s, i) =>
            {
                entryArgS = s;
                entryArgI = i;
            });

        string suppliedArgS = "something";
        int suppliedArgI = 42;

        sm.Fire(x, suppliedArgS, suppliedArgI);

        Assert.Equal(suppliedArgS, entryArgS);
        Assert.Equal(suppliedArgI, entryArgI);
    }

    [Fact]
    public void WhenAnUnhandledTriggerIsFired_TheProvidedHandlerIsCalledWithStateAndTrigger()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        State? state = null;
        Trigger? trigger = null;
        sm.OnUnhandledTrigger((s, t, u) =>
        {
            state = s;
            trigger = t;
        });

        sm.Fire(Trigger.Z);

        Assert.Equal(State.B, state);
        Assert.Equal(Trigger.Z, trigger);
    }

    [Fact]
    public void WhenATransitionOccurs_TheOnTransitionedEventFires()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.B)
            .Permit(Trigger.X, State.A);

        StateMachine<State, Trigger>.Transition transition = null;
        sm.OnTransitioned(t => transition = t);

        sm.Fire(Trigger.X);

        Assert.NotNull(transition);
        Assert.Equal(Trigger.X, transition.Trigger);
        Assert.Equal(State.B, transition.Source);
        Assert.Equal(State.A, transition.Destination);
        Assert.Equal(Array.Empty<object>(), transition.Parameters);
    }

    [Fact]
    public void WhenATransitionOccurs_TheOnTransitionCompletedEventFires()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.B)
            .Permit(Trigger.X, State.A);

        StateMachine<State, Trigger>.Transition transition = null;
        sm.OnTransitionCompleted(t => transition = t);

        sm.Fire(Trigger.X);

        Assert.NotNull(transition);
        Assert.Equal(Trigger.X, transition.Trigger);
        Assert.Equal(State.B, transition.Source);
        Assert.Equal(State.A, transition.Destination);
        Assert.Equal(Array.Empty<object>(), transition.Parameters);
    }

    /// <summary>
    /// The expected ordering is OnExit, OnTransitioned, OnEntry, OnTransitionCompleted
    /// </summary>
    [Fact]
    public void TheOnTransitionedEventFiresBeforeTheOnEntryEventAndOnTransitionCompletedFiresAfterwards()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        List<string> expectedOrdering = new() { "OnExit", "OnTransitioned", "OnEntry", "OnTransitionCompleted" };
        List<string> actualOrdering = new();

        sm.Configure(State.B)
            .Permit(Trigger.X, State.A)
            .OnExit(() => actualOrdering.Add("OnExit"));

        sm.Configure(State.A)
            .OnEntry(() => actualOrdering.Add("OnEntry"));

        sm.OnTransitioned(t => actualOrdering.Add("OnTransitioned"));
        sm.OnTransitionCompleted(t => actualOrdering.Add("OnTransitionCompleted"));

        sm.Fire(Trigger.X);

        Assert.Equal(expectedOrdering.Count, actualOrdering.Count);
        for (int i = 0; i < expectedOrdering.Count; i++)
        {
            Assert.Equal(expectedOrdering[i], actualOrdering[i]);
        }
    }

    [Fact]
    public void WhenATransitionOccurs_WithAParameterizedTrigger_TheOnTransitionedEventFires()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        StateMachine<State, Trigger>.TriggerWithParameters<string> triggerX = sm.SetTriggerParameters<string>(Trigger.X);

        sm.Configure(State.B)
            .Permit(Trigger.X, State.A);

        StateMachine<State, Trigger>.Transition transition = null;
        sm.OnTransitioned(t => transition = t);

        string parameter = "the parameter";
        sm.Fire(triggerX, parameter);

        Assert.NotNull(transition);
        Assert.Equal(Trigger.X, transition.Trigger);
        Assert.Equal(State.B, transition.Source);
        Assert.Equal(State.A, transition.Destination);
        Assert.Equal(1, transition.Parameters.Count());
        Assert.Equal(parameter, transition.Parameters[0]);
    }

    [Fact]
    public void WhenATransitionOccurs_WithAParameterizedTrigger_TheOnTransitionCompletedEventFires()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        StateMachine<State, Trigger>.TriggerWithParameters<string> triggerX = sm.SetTriggerParameters<string>(Trigger.X);

        sm.Configure(State.B)
            .Permit(Trigger.X, State.A);

        StateMachine<State, Trigger>.Transition transition = null;
        sm.OnTransitionCompleted(t => transition = t);

        string parameter = "the parameter";
        sm.Fire(triggerX, parameter);

        Assert.NotNull(transition);
        Assert.Equal(Trigger.X, transition.Trigger);
        Assert.Equal(State.B, transition.Source);
        Assert.Equal(State.A, transition.Destination);
        Assert.Equal(1, transition.Parameters.Count());
        Assert.Equal(parameter, transition.Parameters[0]);
    }

    [Fact]
    public void WhenATransitionOccurs_WithAParameterizedTrigger_WithMultipleParameters_TheOnTransitionedEventFires()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        StateMachine<State, Trigger>.TriggerWithParameters<string, int, bool> triggerX = sm.SetTriggerParameters<string, int, bool>(Trigger.X);

        sm.Configure(State.B)
            .Permit(Trigger.X, State.A);

        StateMachine<State, Trigger>.Transition transition = null;
        sm.OnTransitioned(t => transition = t);

        string firstParameter = "the parameter";
        int secondParameter = 99;
        bool thirdParameter = true;
        sm.Fire(triggerX, firstParameter, secondParameter, thirdParameter);

        Assert.NotNull(transition);
        Assert.Equal(Trigger.X, transition.Trigger);
        Assert.Equal(State.B, transition.Source);
        Assert.Equal(State.A, transition.Destination);
        Assert.Equal(3, transition.Parameters.Count());
        Assert.Equal(firstParameter, transition.Parameters[0]);
        Assert.Equal(secondParameter, transition.Parameters[1]);
        Assert.Equal(thirdParameter, transition.Parameters[2]);
    }

    [Fact]
    public void WhenATransitionOccurs_WithAParameterizedTrigger_WithMultipleParameters_TheOnTransitionCompletedEventFires()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        StateMachine<State, Trigger>.TriggerWithParameters<string, int, bool> triggerX = sm.SetTriggerParameters<string, int, bool>(Trigger.X);

        sm.Configure(State.B)
            .Permit(Trigger.X, State.A);

        StateMachine<State, Trigger>.Transition transition = null;
        sm.OnTransitionCompleted(t => transition = t);

        string firstParameter = "the parameter";
        int secondParameter = 99;
        bool thirdParameter = true;
        sm.Fire(triggerX, firstParameter, secondParameter, thirdParameter);

        Assert.NotNull(transition);
        Assert.Equal(Trigger.X, transition.Trigger);
        Assert.Equal(State.B, transition.Source);
        Assert.Equal(State.A, transition.Destination);
        Assert.Equal(3, transition.Parameters.Count());
        Assert.Equal(firstParameter, transition.Parameters[0]);
        Assert.Equal(secondParameter, transition.Parameters[1]);
        Assert.Equal(thirdParameter, transition.Parameters[2]);
    }

    [Fact]
    public void DirectCyclicConfigurationDetected()
    {
        StateMachine<State, Trigger> sm = new(State.A);

        Assert.Throws(typeof(ArgumentException), () => { sm.Configure(State.A).SubstateOf(State.A); });
    }

    [Fact]
    public void NestedCyclicConfigurationDetected()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.B).SubstateOf(State.A);

        Assert.Throws(typeof(ArgumentException), () => { sm.Configure(State.A).SubstateOf(State.B); });
    }

    [Fact]
    public void NestedTwoLevelsCyclicConfigurationDetected()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.B).SubstateOf(State.A);
        sm.Configure(State.C).SubstateOf(State.B);

        Assert.Throws(typeof(ArgumentException), () => { sm.Configure(State.A).SubstateOf(State.C); });
    }

    [Fact]
    public void DelayedNestedCyclicConfigurationDetected()
    {
        // Set up two states and substates, then join them
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.B).SubstateOf(State.A);

        sm.Configure(State.C);
        sm.Configure(State.A).SubstateOf(State.C);

        Assert.Throws(typeof(ArgumentException), () => { sm.Configure(State.C).SubstateOf(State.B); });
    }

    [Fact]
    public void IgnoreVsPermitReentry()
    {
        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .OnEntry(CountCalls)
            .PermitReentry(Trigger.X)
            .Ignore(Trigger.Y);

        _numCalls = 0;

        sm.Fire(Trigger.X);
        sm.Fire(Trigger.Y);

        Assert.Equal(1, _numCalls);
    }

    [Fact]
    public void IgnoreVsPermitReentryFrom()
    {
        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .OnEntryFrom(Trigger.X, CountCalls)
            .OnEntryFrom(Trigger.Y, CountCalls)
            .PermitReentry(Trigger.X)
            .Ignore(Trigger.Y);

        _numCalls = 0;

        sm.Fire(Trigger.X);
        sm.Fire(Trigger.Y);

        Assert.Equal(1, _numCalls);
    }

    [Fact]
    public void IfSelfTransitionPermited_ActionsFire_InSubstate()
    {
        StateMachine<State, Trigger> sm = new(State.A);

        bool onEntryStateBfired = false;
        bool onExitStateBfired = false;
        bool onExitStateAfired = false;

        sm.Configure(State.B)
            .OnEntry(t => onEntryStateBfired = true)
            .PermitReentry(Trigger.X)
            .OnExit(t => onExitStateBfired = true);

        sm.Configure(State.A)
            .SubstateOf(State.B)
            .OnExit(t => onExitStateAfired = true);

        sm.Fire(Trigger.X);

        Assert.Equal(State.B, sm.State);
        Assert.True(onEntryStateBfired);
        Assert.True(onExitStateBfired);
        Assert.True(onExitStateAfired);
    }

    [Fact]
    public void TransitionWhenParameterizedGuardTrue()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A).PermitIf(x, State.B, i => i == 2);
        sm.Fire(x, 2);

        Assert.Equal(sm.State, State.B);
    }

    [Fact]
    public void ExceptionWhenParameterizedGuardFalse()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A).PermitIf(x, State.B, i => i == 3);

        Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
    }

    [Fact]
    public void TransitionWhenBothParameterizedGuardClausesTrue()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        Tuple<Func<int, bool>, string> positiveGuard = Tuple.Create(new Func<int, bool>(o => o == 2), "Positive Guard");
        Tuple<Func<int, bool>, string> negativeGuard = Tuple.Create(new Func<int, bool>(o => o != 3), "Negative Guard");
        sm.Configure(State.A).PermitIf(x, State.B, positiveGuard, negativeGuard);
        sm.Fire(x, 2);

        Assert.Equal(sm.State, State.B);
    }

    [Fact]
    public void ExceptionWhenBothParameterizedGuardClausesFalse()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        // Create Two guards that both must be true
        Tuple<Func<int, bool>, string> positiveGuard = Tuple.Create(new Func<int, bool>(o => o == 2), "Positive Guard");
        Tuple<Func<int, bool>, string> negativeGuard = Tuple.Create(new Func<int, bool>(o => o != 3), "Negative Guard");
        sm.Configure(State.A).PermitIf(x, State.B, positiveGuard, negativeGuard);

        Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 3));
    }

    [Fact]
    public void TransitionWhenGuardReturnsTrueOnTriggerWithMultipleParameters()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<string, int> x = sm.SetTriggerParameters<string, int>(Trigger.X);
        sm.Configure(State.A).PermitIf(x, State.B, (s, i) => s == "3" && i == 3);
        sm.Fire(x, "3", 3);
        Assert.Equal(sm.State, State.B);
    }

    [Fact]
    public void ExceptionWhenGuardFalseOnTriggerWithMultipleParameters()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<string, int> x = sm.SetTriggerParameters<string, int>(Trigger.X);
        sm.Configure(State.A).PermitIf(x, State.B, (s, i) => s == "3" && i == 3);
        Assert.Equal(sm.State, State.A);

        Assert.Throws<InvalidOperationException>(() => sm.Fire(x, "2", 2));
        Assert.Throws<InvalidOperationException>(() => sm.Fire(x, "3", 2));
        Assert.Throws<InvalidOperationException>(() => sm.Fire(x, "2", 3));
    }

    [Fact]
    public void TransitionWhenPermitIfHasMultipleExclusiveGuards()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A)
            .PermitIf(x, State.B, i => i == 3)
            .PermitIf(x, State.C, i => i == 2);
        sm.Fire(x, 3);
        Assert.Equal(sm.State, State.B);
    }

    [Fact]
    public void ExceptionWhenPermitIfHasMultipleNonExclusiveGuards()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A).PermitIf(x, State.B, i => i % 2 == 0)  // Is Even
            .PermitIf(x, State.C, i => i == 2);

        Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
    }

    [Fact]
    public void TransitionWhenPermitDyanmicIfHasMultipleExclusiveGuards()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A)
            .PermitDynamicIf(x, i => i == 3 ? State.B : State.C, i => i == 3 || i == 5)
            .PermitDynamicIf(x, i => i == 2 ? State.C : State.D, i => i == 2 || i == 4);
        sm.Fire(x, 3);
        Assert.Equal(sm.State, State.B);
    }

    [Fact]
    public void ExceptionWhenPermitDyanmicIfHasMultipleNonExclusiveGuards()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A).PermitDynamicIf(x, i => i == 4 ? State.B : State.C, i => i % 2 == 0)
            .PermitDynamicIf(x, i => i == 2 ? State.C : State.D, i => i == 2);

        Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
    }

    [Fact]
    public void TransitionWhenPermitIfHasMultipleExclusiveGuardsWithSuperStateTrue()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A).PermitIf(x, State.D, i => i == 3);
        {
            sm.Configure(State.B).SubstateOf(State.A).PermitIf(x, State.C, i => i == 2);
        }
        sm.Fire(x, 3);
        Assert.Equal(sm.State, State.D);
    }

    [Fact]
    public void TransitionWhenPermitIfHasMultipleExclusiveGuardsWithSuperStateFalse()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A).PermitIf(x, State.D, i => i == 3);
        {
            sm.Configure(State.B).SubstateOf(State.A).PermitIf(x, State.C, i => i == 2);
        }
        sm.Fire(x, 2);
        Assert.Equal(sm.State, State.C);
    }

    [Fact]
    public void TransitionWhenPermitReentryIfParameterizedGuardTrue()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A)
            .PermitReentryIf(x, i => i == 3);
        sm.Fire(x, 3);
        Assert.Equal(sm.State, State.A);
    }

    [Fact]
    public void TransitionWhenPermitReentryIfParameterizedGuardFalse()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A)
            .PermitReentryIf(x, i => i == 3);

        Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
    }

    [Fact]
    public void NoTransitionWhenIgnoreIfParameterizedGuardTrue()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A).IgnoreIf(x, i => i == 3);
        sm.Fire(x, 3);

        Assert.Equal(sm.State, State.A);
    }

    [Fact]
    public void ExceptionWhenIgnoreIfParameterizedGuardFalse()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> x = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A).IgnoreIf(x, i => i == 3);

        Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
    }

    /// <summary>
    /// Verifies guard clauses are only called one time during a transition evaluation.
    /// </summary>
    [Fact]
    public void GuardClauseCalledOnlyOnce()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        int i = 0;

        sm.Configure(State.A).PermitIf(Trigger.X, State.B, () =>
        {
            ++i;
            return true;
        });

        sm.Fire(Trigger.X);

        Assert.Equal(1, i);
    }
    [Fact]
    public void NoExceptionWhenPermitIfHasMultipleExclusiveGuardsBothFalse()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        bool onUnhandledTriggerWasCalled = false;
        sm.OnUnhandledTrigger((s, t) => { onUnhandledTriggerWasCalled = true; });  // NEVER CALLED
        int i = 0;
        sm.Configure(State.A)
            .PermitIf(Trigger.X, State.B, () => i == 2)
            .PermitIf(Trigger.X, State.C, () => i == 1);

        sm.Fire(Trigger.X);  // THROWS EXCEPTION

        Assert.True(onUnhandledTriggerWasCalled, "OnUnhandledTrigger was called");
        Assert.Equal(sm.State, State.A);
    }

    [Fact]
    public void TransitionToSuperstateDoesNotExitSuperstate()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        bool superExit = false;
        bool superEntry = false;
        bool subExit = false;

        sm.Configure(State.A)
            .OnEntry(() => superEntry = true)
            .OnExit(() => superExit = true);

        sm.Configure(State.B)
            .SubstateOf(State.A)
            .Permit(Trigger.Y, State.A)
            .OnExit(() => subExit = true);

        sm.Fire(Trigger.Y);

        Assert.True(subExit);
        Assert.False(superEntry);
        Assert.False(superExit);
    }

    [Fact]
    public void OnExitFiresOnlyOnceReentrySubstate()
    {
        StateMachine<State, Trigger> sm = new(State.A);

        int exitB = 0;
        int exitA = 0;
        int entryB = 0;
        int entryA = 0;

        sm.Configure(State.A)
            .SubstateOf(State.B)
            .OnEntry(() => entryA++)
            .PermitReentry(Trigger.X)
            .OnExit(() => exitA++);

        sm.Configure(State.B)
            .OnEntry(() => entryB++)
            .OnExit(() => exitB++);

        sm.Fire(Trigger.X);

        Assert.Equal(0, exitB);
        Assert.Equal(0, entryB);
        Assert.Equal(1, exitA);
        Assert.Equal(1, entryA);
    }

    [Fact]
    public void WhenConfigurePermittedTransitionOnTriggerWithoutParameters_ThenStateMachineCanFireTrigger()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).Permit(trigger, State.B);
        Assert.True(sm.CanFire(trigger));
    }
    [Fact]
    public void WhenConfigurePermittedTransitionOnTriggerWithoutParameters_ThenStateMachineCanEnumeratePermittedTriggers()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).Permit(trigger, State.B);
        Assert.Single(sm.PermittedTriggers, trigger);
    }


    [Fact]
    public void WhenConfigurePermittedTransitionOnTriggerWithParameters_ThenStateMachineCanFireTrigger()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).Permit(trigger, State.B);
        sm.SetTriggerParameters<string>(trigger);
        Assert.True(sm.CanFire(trigger));
    }
    [Fact]
    public void WhenConfigurePermittedTransitionOnTriggerWithParameters_ThenStateMachineCanEnumeratePermittedTriggers()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).Permit(trigger, State.B);
        sm.SetTriggerParameters<string>(trigger);
        Assert.Single(sm.PermittedTriggers, trigger);
    }

    [Fact]
    public void WhenConfigureInternalTransitionOnTriggerWithoutParameters_ThenStateMachineCanFireTrigger()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).InternalTransition(trigger, (_) => { });
        Assert.True(sm.CanFire(trigger));
    }

    [Fact]
    public void WhenConfigureInternalTransitionOnTriggerWithoutParameters_ThenStateMachineCanEnumeratePermittedTriggers()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).InternalTransition(trigger, (_) => { });
        Assert.Single(sm.PermittedTriggers, trigger);
    }

    [Fact]
    public void WhenConfigureInternalTransitionOnTriggerWithParameters_ThenStateMachineCanFireTrigger()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).InternalTransition(sm.SetTriggerParameters<string>(trigger), (arg, _) => { });
        Assert.True(sm.CanFire(trigger));
    }

    [Fact]
    public void WhenConfigureInternalTransitionOnTriggerWithParameters_ThenStateMachineCanEnumeratePermittedTriggers()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).InternalTransition(sm.SetTriggerParameters<string>(trigger), (arg, _) => { });
        Assert.Single(sm.PermittedTriggers, trigger);
    }

    [Fact]
    public void WhenConfigureConditionallyPermittedTransitionOnTriggerWithParameters_ThenStateMachineCanFireTrigger()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).PermitIf(sm.SetTriggerParameters<string>(trigger), State.B, _ => true);
        Assert.True(sm.CanFire(trigger));
    }

    [Fact]
    public void WhenConfigureConditionallyPermittedTransitionOnTriggerWithParameters_ThenStateMachineCanEnumeratePermittedTriggers()
    {
        Trigger trigger = Trigger.X;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).PermitIf(sm.SetTriggerParameters<string>(trigger), State.B, _ => true);
        Assert.Single(sm.PermittedTriggers, trigger);
    }

    [Fact]
    public void PermittedTriggersIncludeAllDefinedTriggers()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A)
            .Permit(Trigger.X, State.B)
            .InternalTransition(Trigger.Y, _ => { })
            .Ignore(Trigger.Z);
        Assert.All(new[] { Trigger.X, Trigger.Y, Trigger.Z }, trigger => Assert.Contains(trigger, sm.PermittedTriggers));
    }

    [Fact]
    public void PermittedTriggersExcludeAllUndefinedTriggers()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A)
            .Permit(Trigger.X, State.B);
        Assert.All(new[] { Trigger.Y, Trigger.Z }, trigger => Assert.DoesNotContain(trigger, sm.PermittedTriggers));
    }

    [Fact]
    public void PermittedTriggersIncludeAllInheritedTriggers()
    {
        State superState = State.A,
                subState = State.B,
                otherState = State.C;
        Trigger superStateTrigger = Trigger.X,
                subStateTrigger = Trigger.Y;

        StateMachine<State, Trigger> hsm(State initialState)
        {
            return new StateMachine<State, Trigger>(initialState)
                                .Configure(superState)
                                .Permit(superStateTrigger, otherState)
                            .Machine
                                .Configure(subState)
                                .SubstateOf(superState)
                                .Permit(subStateTrigger, otherState)
                            .Machine;
        }

        StateMachine<State, Trigger> hsmInSuperstate = hsm(superState);
        StateMachine<State, Trigger> hsmInSubstate = hsm(subState);

        Assert.All(hsmInSuperstate.PermittedTriggers, trigger => Assert.Contains(trigger, hsmInSubstate.PermittedTriggers));
        Assert.Contains(superStateTrigger, hsmInSubstate.PermittedTriggers);
        Assert.Contains(superStateTrigger, hsmInSuperstate.PermittedTriggers);
        Assert.Contains(subStateTrigger, hsmInSubstate.PermittedTriggers);
        Assert.DoesNotContain(subStateTrigger, hsmInSuperstate.PermittedTriggers);
    }

    [Fact]
    public void CanFire_GetUnmetGuardDescriptionsIfGuardFails()
    {
        const string guardDescription = "Guard failed";
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A)
          .PermitIf(Trigger.X, State.B, () => false, guardDescription);

        bool result = sm.CanFire(Trigger.X, out ICollection<string> unmetGuards);

        Assert.False(result);
        Assert.True(unmetGuards?.Count == 1);
        Assert.Contains(guardDescription, unmetGuards);
    }

    [Fact]
    public void CanFire_GetNullUnmetGuardDescriptionsIfInvalidTrigger()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        bool result = sm.CanFire(Trigger.X, out ICollection<string> unmetGuards);

        Assert.False(result);
        Assert.Null(unmetGuards);
    }

    [Fact]
    public void CanFire_GetEmptyUnmetGuardDescriptionsIfValidTrigger()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).Permit(Trigger.X, State.B);
        bool result = sm.CanFire(Trigger.X, out ICollection<string> unmetGuards);

        Assert.True(result);
        Assert.True(unmetGuards?.Count == 0);
    }

    [Fact]
    public void CanFire_GetEmptyUnmetGuardDescriptionsIfGuardPasses()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A).PermitIf(Trigger.X, State.B, () => true, "Guard passed");
        bool result = sm.CanFire(Trigger.X, out ICollection<string> unmetGuards);

        Assert.True(result);
        Assert.True(unmetGuards?.Count == 0);
    }

}