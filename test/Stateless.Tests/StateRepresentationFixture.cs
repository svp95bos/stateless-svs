using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Stateless.Tests;

public class StateRepresentationFixture
{
    [Fact]
    public void UponEntering_EnteringActionsExecuted()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        StateMachine<State, Trigger>.Transition
            transition = new(State.A, State.B, Trigger.X),
            actualTransition = null;
        stateRepresentation.AddEntryAction((t, a) => actualTransition = t, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        stateRepresentation.Enter(transition);
        Assert.Equal(transition, actualTransition);
    }

    [Fact]
    public void UponLeaving_EnteringActionsNotExecuted()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        StateMachine<State, Trigger>.Transition
            transition = new(State.A, State.B, Trigger.X),
            actualTransition = null;
        stateRepresentation.AddEntryAction((t, a) => actualTransition = t, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        stateRepresentation.Exit(transition);
        Assert.Null(actualTransition);
    }

    [Fact]
    public void UponLeaving_LeavingActionsExecuted()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.A);
        StateMachine<State, Trigger>.Transition
            transition = new(State.A, State.B, Trigger.X),
            actualTransition = null;
        stateRepresentation.AddExitAction(t => actualTransition = t, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        stateRepresentation.Exit(transition);
        Assert.Equal(transition, actualTransition);
    }

    [Fact]
    public void UponEntering_LeavingActionsNotExecuted()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.A);
        StateMachine<State, Trigger>.Transition
            transition = new(State.A, State.B, Trigger.X),
            actualTransition = null;
        stateRepresentation.AddExitAction(t => actualTransition = t, Reflection.InvocationInfo.Create(null, "exitActionDescription"));
        stateRepresentation.Enter(transition);
        Assert.Null(actualTransition);
    }

    [Fact]
    public void IncludesUnderlyingState()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        Assert.True(stateRepresentation.Includes(State.B));
    }

    [Fact]
    public void DoesNotIncludeUnrelatedState()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        Assert.False(stateRepresentation.Includes(State.C));
    }

    [Fact]
    public void IncludesSubstate()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        stateRepresentation.AddSubstate(CreateRepresentation(State.C));
        Assert.True(stateRepresentation.Includes(State.C));
    }

    [Fact]
    public void DoesNotIncludeSuperstate()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        stateRepresentation.Superstate = CreateRepresentation(State.C);
        Assert.False(stateRepresentation.Includes(State.C));
    }

    [Fact]
    public void IsIncludedInUnderlyingState()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        Assert.True(stateRepresentation.IsIncludedIn(State.B));
    }

    [Fact]
    public void IsNotIncludedInUnrelatedState()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        Assert.False(stateRepresentation.IsIncludedIn(State.C));
    }

    [Fact]
    public void IsNotIncludedInSubstate()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        stateRepresentation.AddSubstate(CreateRepresentation(State.C));
        Assert.False(stateRepresentation.IsIncludedIn(State.C));
    }

    [Fact]
    public void IsIncludedInSuperstate()
    {
        StateMachine<State, Trigger>.StateRepresentation stateRepresentation = CreateRepresentation(State.B);
        stateRepresentation.Superstate = CreateRepresentation(State.C);
        Assert.True(stateRepresentation.IsIncludedIn(State.C));
    }

    [Fact]
    public void WhenTransitioningFromSubToSuperstate_SubstateEntryActionsExecuted()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        bool executed = false;
        sub.AddEntryAction((t, a) => executed = true, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        StateMachine<State, Trigger>.Transition transition = new(super.UnderlyingState, sub.UnderlyingState, Trigger.X);
        sub.Enter(transition);
        Assert.True(executed);
    }

    [Fact]
    public void WhenTransitioningFromSubToSuperstate_SubstateExitActionsExecuted()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        bool executed = false;
        sub.AddExitAction(t => executed = true, Reflection.InvocationInfo.Create(null, "exitActionDescription"));
        StateMachine<State, Trigger>.Transition transition = new(sub.UnderlyingState, super.UnderlyingState, Trigger.X);
        sub.Exit(transition);
        Assert.True(executed);
    }

    [Fact]
    public void WhenTransitioningToSuperFromSubstate_SuperEntryActionsNotExecuted()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        bool executed = false;
        super.AddEntryAction((t, a) => executed = true, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        StateMachine<State, Trigger>.Transition transition = new(super.UnderlyingState, sub.UnderlyingState, Trigger.X);
        super.Enter(transition);
        Assert.False(executed);
    }

    [Fact]
    public void WhenTransitioningFromSuperToSubstate_SuperExitActionsNotExecuted()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        bool executed = false;
        super.AddExitAction(t => executed = true, Reflection.InvocationInfo.Create(null, "exitActionDescription"));
        StateMachine<State, Trigger>.Transition transition = new(super.UnderlyingState, sub.UnderlyingState, Trigger.X);
        super.Exit(transition);
        Assert.False(executed);
    }

    [Fact]
    public void WhenEnteringSubstate_SuperEntryActionsExecuted()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        bool executed = false;
        super.AddEntryAction((t, a) => executed = true, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        StateMachine<State, Trigger>.Transition transition = new(State.C, sub.UnderlyingState, Trigger.X);
        sub.Enter(transition);
        Assert.True(executed);
    }

    [Fact]
    public void WhenLeavingSubstate_SuperExitActionsExecuted()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        bool executed = false;
        super.AddExitAction(t => executed = true, Reflection.InvocationInfo.Create(null, "exitActionDescription"));
        StateMachine<State, Trigger>.Transition transition = new(sub.UnderlyingState, State.C, Trigger.X);
        sub.Exit(transition);
        Assert.True(executed);
    }

    [Fact]
    public void EntryActionsExecuteInOrder()
    {
        List<int> actual = new();

        StateMachine<State, Trigger>.StateRepresentation rep = CreateRepresentation(State.B);
        rep.AddEntryAction((t, a) => actual.Add(0), Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        rep.AddEntryAction((t, a) => actual.Add(1), Reflection.InvocationInfo.Create(null, "entryActionDescription"));

        rep.Enter(new StateMachine<State, Trigger>.Transition(State.A, State.B, Trigger.X));

        Assert.Equal(2, actual.Count);
        Assert.Equal(0, actual[0]);
        Assert.Equal(1, actual[1]);
    }

    [Fact]
    public void ExitActionsExecuteInOrder()
    {
        List<int> actual = new();

        StateMachine<State, Trigger>.StateRepresentation rep = CreateRepresentation(State.B);
        rep.AddExitAction(t => actual.Add(0), Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        rep.AddExitAction(t => actual.Add(1), Reflection.InvocationInfo.Create(null, "entryActionDescription"));

        rep.Exit(new StateMachine<State, Trigger>.Transition(State.B, State.C, Trigger.X));

        Assert.Equal(2, actual.Count);
        Assert.Equal(0, actual[0]);
        Assert.Equal(1, actual[1]);
    }

    [Fact]
    public void WhenTransitionExists_TriggerCannotBeFired()
    {
        StateMachine<State, Trigger>.StateRepresentation rep = CreateRepresentation(State.B);
        Assert.False(rep.CanHandle(Trigger.X));
    }

    [Fact]
    public void WhenTransitionDoesNotExist_TriggerCanBeFired()
    {
        StateMachine<State, Trigger>.StateRepresentation rep = CreateRepresentation(State.B);
        rep.AddTriggerBehaviour(new StateMachine<State, Trigger>.IgnoredTriggerBehaviour(Trigger.X, null));
        Assert.True(rep.CanHandle(Trigger.X));
    }

    [Fact]
    public void WhenTransitionExistsInSupersate_TriggerCanBeFired()
    {
        StateMachine<State, Trigger>.StateRepresentation rep = CreateRepresentation(State.B);
        rep.AddTriggerBehaviour(new StateMachine<State, Trigger>.IgnoredTriggerBehaviour(Trigger.X, null));
        StateMachine<State, Trigger>.StateRepresentation sub = CreateRepresentation(State.C);
        sub.Superstate = rep;
        rep.AddSubstate(sub);
        Assert.True(sub.CanHandle(Trigger.X));
    }

    [Fact]
    public void WhenEnteringSubstate_SuperstateEntryActionsExecuteBeforeSubstate()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        int order = 0, subOrder = 0, superOrder = 0;
        super.AddEntryAction((t, a) => superOrder = order++, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        sub.AddEntryAction((t, a) => subOrder = order++, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        StateMachine<State, Trigger>.Transition transition = new(State.C, sub.UnderlyingState, Trigger.X);
        sub.Enter(transition);
        Assert.True(superOrder < subOrder);
    }

    [Fact]
    public void WhenExitingSubstate_SubstateEntryActionsExecuteBeforeSuperstate()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        int order = 0, subOrder = 0, superOrder = 0;
        super.AddExitAction(t => superOrder = order++, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        sub.AddExitAction(t => subOrder = order++, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
        StateMachine<State, Trigger>.Transition transition = new(sub.UnderlyingState, State.C, Trigger.X);
        sub.Exit(transition);
        Assert.True(subOrder < superOrder);
    }

    [Fact]
    public void WhenTransitionUnmetGuardConditions_TriggerCannotBeFired()
    {
        StateMachine<State, Trigger>.StateRepresentation rep = CreateRepresentation(State.B);

        Tuple<Func<object[], bool>, string>[] falseConditions = new[] {
            new Tuple<Func<object[], bool>, string>(args => true, "1"),
            new Tuple<Func<object[], bool>, string>(args => false, "2")
        };

        StateMachine<State, Trigger>.TransitionGuard transitionGuard = new(falseConditions);
        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transition = new(Trigger.X, State.C, transitionGuard);
        rep.AddTriggerBehaviour(transition);

        Assert.False(rep.CanHandle(Trigger.X));
    }

    [Fact]
    public void WhenTransitioGuardConditionsMet_TriggerCanBeFired()
    {
        StateMachine<State, Trigger>.StateRepresentation rep = CreateRepresentation(State.B);

        Tuple<Func<object[], bool>, string>[] trueConditions = new[] {
            new Tuple<Func<object[], bool>, string>(args => true, "1"),
            new Tuple<Func<object[], bool>, string>(args => true, "2")
        };

        StateMachine<State, Trigger>.TransitionGuard transitionGuard = new(trueConditions);
        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transition = new(Trigger.X, State.C, transitionGuard);
        rep.AddTriggerBehaviour(transition);

        Assert.True(rep.CanHandle(Trigger.X));
    }

    [Fact]
    public void WhenTransitionExistAndSuperstateUnmetGuardConditions_FireNotPossible()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        Tuple<Func<object[], bool>, string>[] falseConditions = new[] {
            new Tuple<Func<object[], bool>, string>(args => true, "1"),
            new Tuple<Func<object[], bool>, string>(args => false, "2")
        };
        StateMachine<State, Trigger>.TransitionGuard transitionGuard = new(falseConditions);
        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transition = new(Trigger.X, State.C, transitionGuard);
        super.AddTriggerBehaviour(transition);

        bool reslt = sub.TryFindHandler(Trigger.X, Array.Empty<object>(), out StateMachine<State, Trigger>.TriggerBehaviourResult result);

        Assert.False(reslt);
        Assert.False(sub.CanHandle(Trigger.X));
        Assert.False(super.CanHandle(Trigger.X));

    }
    [Fact]
    public void WhenTransitionExistSuperstateMetGuardConditions_CanBeFired()
    {
        CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub);

        Tuple<Func<object[], bool>, string>[] trueConditions = new[] {
            new Tuple<Func<object[], bool>, string>(args => true, "1"),
            new Tuple<Func<object[], bool>, string>(args => true, "2")
        };
        StateMachine<State, Trigger>.TransitionGuard transitionGuard = new(trueConditions);
        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transition = new(Trigger.X, State.C, transitionGuard);

        super.AddTriggerBehaviour(transition);
        sub.TryFindHandler(Trigger.X, Array.Empty<object>(), out StateMachine<State, Trigger>.TriggerBehaviourResult result);

        Assert.True(sub.CanHandle(Trigger.X));
        Assert.True(super.CanHandle(Trigger.X));
        Assert.NotNull(result);
        Assert.True(result?.Handler.GuardConditionsMet());
        Assert.False(result?.UnmetGuardConditions.Any());

    }

    void CreateSuperSubstatePair(out StateMachine<State, Trigger>.StateRepresentation super, out StateMachine<State, Trigger>.StateRepresentation sub)
    {
        super = CreateRepresentation(State.A);
        sub = CreateRepresentation(State.B);
        super.AddSubstate(sub);
        sub.Superstate = super;
    }

    static StateMachine<State, Trigger>.StateRepresentation CreateRepresentation(State state) => new(state);

    // Issue #398 - Set guard description if substate transition fails
    [Fact]
    public void SetGuardDescriptionWhenSubstateGuardFails()
    {
        const string expectedGuardDescription = "Guard failed";
        ICollection<string> guardDescriptions = null;

        StateMachine<State, Trigger> fsm = new(State.B);
        fsm.OnUnhandledTrigger((state, trigger, descriptions) => guardDescriptions = descriptions);

        fsm.Configure(State.B).SubstateOf(State.A).PermitIf(Trigger.X, State.C, () => false, expectedGuardDescription);

        fsm.Fire(Trigger.X);

        Assert.Equal(fsm.State, State.B);
        Assert.True(guardDescriptions != null);
        Assert.Equal(guardDescriptions.Count, 1);
        Assert.Equal(guardDescriptions.First(), expectedGuardDescription);
    }

    // Issue #422 - Add all guard descriptions to result if multiple guards fail for same trigger
    [Fact]
    public void AddAllGuardDescriptionsWhenMultipleGuardsFailForSameTrigger()
    {
        ICollection<string> expectedGuardDescriptions = new List<string> { "PermitReentryIf guard failed", "PermitIf guard failed" };
        ICollection<string> guardDescriptions = null;

        StateMachine<State, Trigger> fsm = new(State.A);
        fsm.OnUnhandledTrigger((state, trigger, descriptions) => guardDescriptions = descriptions);

        fsm.Configure(State.A)
            .PermitReentryIf(Trigger.X, () => false, "PermitReentryIf guard failed")
            .PermitIf(Trigger.X, State.C, () => false, "PermitIf guard failed");

        fsm.Fire(Trigger.X);

        Assert.Equal(fsm.State, State.A);
        Assert.True(guardDescriptions != null);
        Assert.Equal(2, guardDescriptions.Count);
        foreach (string description in guardDescriptions)
        {
            Assert.True(expectedGuardDescriptions.Contains(description));
        }
    }
}
