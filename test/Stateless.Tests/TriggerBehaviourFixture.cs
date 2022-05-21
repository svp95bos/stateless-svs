using System;

using Xunit;

namespace Stateless.Tests;

public class TriggerBehaviourFixture
{
    [Fact]
    public void ExposesCorrectUnderlyingTrigger()
    {
        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transitioning = new(
            Trigger.X, State.C, null);

        Assert.Equal(Trigger.X, transitioning.Trigger);
    }

    protected bool False(params object[] args)
    {
        return false;
    }

    [Fact]
    public void WhenGuardConditionFalse_GuardConditionsMetIsFalse()
    {
        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transitioning = new(
            Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(False));

        Assert.False(transitioning.GuardConditionsMet());
    }

    protected bool True(params object[] args)
    {
        return true;
    }

    [Fact]
    public void WhenGuardConditionTrue_GuardConditionsMetIsTrue()
    {
        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transitioning = new(
            Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(True));

        Assert.True(transitioning.GuardConditionsMet());
    }

    [Fact]
    public void WhenOneOfMultipleGuardConditionsFalse_GuardConditionsMetIsFalse()
    {
        Tuple<Func<object[], bool>, string>[] falseGuard = new[] {
            new Tuple<Func<object[], bool>, string>(args => true, "1"),
            new Tuple<Func<object[], bool>, string>(args => true, "2")
        };

        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transitioning = new(
            Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(falseGuard));

        Assert.True(transitioning.GuardConditionsMet());
    }

    [Fact]
    public void WhenAllMultipleGuardConditionsFalse_IsGuardConditionsMetIsFalse()
    {
        Tuple<Func<object[], bool>, string>[] falseGuard = new[] {
            new Tuple<Func<object[], bool>, string>(args => false, "1"),
            new Tuple<Func<object[], bool>, string>(args => false, "2")
        };

        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transitioning = new(
            Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(falseGuard));

        Assert.False(transitioning.GuardConditionsMet());
    }

    [Fact]
    public void WhenAllGuardConditionsTrue_GuardConditionsMetIsTrue()
    {
        Tuple<Func<object[], bool>, string>[] trueGuard = new[] {
            new Tuple<Func<object[], bool>, string>(args => true, "1"),
            new Tuple<Func<object[], bool>, string>(args => true, "2")
        };

        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transitioning = new(
            Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(trueGuard));

        Assert.True(transitioning.GuardConditionsMet());
    }
}
