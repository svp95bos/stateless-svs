﻿using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Stateless.Tests;

public class InternalTransitionFixture
{

    /// <summary>
    /// The expected behaviour of the internal transistion is that the state does not change.
    /// This will fail if the state changes after the trigger has fired.
    /// </summary>
    [Fact]
    public void StayInSameStateOneState_Transition()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A)
            .InternalTransition(Trigger.X, t => { });

        Assert.Equal(State.A, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.A, sm.State);
    }

    [Fact]
    public void StayInSameStateTwoStates_Transition()
    {
        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .InternalTransition(Trigger.X, t => { })
            .Permit(Trigger.Y, State.B);

        sm.Configure(State.B)
                .InternalTransition(Trigger.X, t => { })
                .Permit(Trigger.Y, State.A);

        // This should not cause any state changes
        Assert.Equal(State.A, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.A, sm.State);

        // Change state to B
        sm.Fire(Trigger.Y);

        // This should also not cause any state changes
        Assert.Equal(State.B, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.B, sm.State);
    }
    [Fact]
    public void StayInSameSubStateTransitionInSuperstate_Transition()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.A)
                .InternalTransition(Trigger.X, t => { });

        sm.Configure(State.B)
                .SubstateOf(State.A);

        // This should not cause any state changes
        Assert.Equal(State.B, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.B, sm.State);
    }
    [Fact]
    public void StayInSameSubStateTransitionInSubstate_Transition()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.A);

        sm.Configure(State.B)
                .SubstateOf(State.A)
                .InternalTransition(Trigger.X, t => { });

        // This should not cause any state changes
        Assert.Equal(State.B, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.B, sm.State);
    }

    [Fact]
    public void StayInSameStateOneState_Action()
    {
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A)
            .InternalTransition(Trigger.X, () => { });

        Assert.Equal(State.A, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.A, sm.State);
    }

    [Fact]
    public void StayInSameStateTwoStates_Action()
    {
        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .InternalTransition(Trigger.X, () => { })
            .Permit(Trigger.Y, State.B);

        sm.Configure(State.B)
                .InternalTransition(Trigger.X, () => { })
                .Permit(Trigger.Y, State.A);

        // This should not cause any state changes
        Assert.Equal(State.A, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.A, sm.State);

        // Change state to B
        sm.Fire(Trigger.Y);

        // This should also not cause any state changes
        Assert.Equal(State.B, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.B, sm.State);
    }
    [Fact]
    public void StayInSameSubStateTransitionInSuperstate_Action()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.A)
                .InternalTransition(Trigger.X, () => { })
                .InternalTransition(Trigger.Y, () => { });

        sm.Configure(State.B)
                .SubstateOf(State.A);

        // This should not cause any state changes
        Assert.Equal(State.B, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.B, sm.State);
        sm.Fire(Trigger.Y);
        Assert.Equal(State.B, sm.State);
    }
    [Fact]
    public void StayInSameSubStateTransitionInSubstate_Action()
    {
        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.A);

        sm.Configure(State.B)
                .SubstateOf(State.A)
                .InternalTransition(Trigger.X, () => { })
                .InternalTransition(Trigger.Y, () => { });

        // This should not cause any state changes
        Assert.Equal(State.B, sm.State);
        sm.Fire(Trigger.X);
        Assert.Equal(State.B, sm.State);
        sm.Fire(Trigger.Y);
        Assert.Equal(State.B, sm.State);
    }

    [Fact]
    public void AllowTriggerWithTwoParameters()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        StateMachine<State, Trigger>.TriggerWithParameters<int, string> trigger = sm.SetTriggerParameters<int, string>(Trigger.X);
        const int intParam = 5;
        const string strParam = "Five";
        bool callbackInvoked = false;

        sm.Configure(State.B)
            .InternalTransition(trigger, (i, s, transition) =>
            {
                callbackInvoked = true;
                Assert.Equal(intParam, i);
                Assert.Equal(strParam, s);
            });

        sm.Fire(trigger, intParam, strParam);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void AllowTriggerWithThreeParameters()
    {
        StateMachine<State, Trigger> sm = new(State.B);
        StateMachine<State, Trigger>.TriggerWithParameters<int, string, bool> trigger = sm.SetTriggerParameters<int, string, bool>(Trigger.X);
        const int intParam = 5;
        const string strParam = "Five";
        bool boolParam = true;
        bool callbackInvoked = false;

        sm.Configure(State.B)
            .InternalTransition(trigger, (i, s, b, transition) =>
            {
                callbackInvoked = true;
                Assert.Equal(intParam, i);
                Assert.Equal(strParam, s);
                Assert.Equal(boolParam, b);
            });

        sm.Fire(trigger, intParam, strParam, boolParam);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void ConditionalInternalTransition_ShouldBeReflectedInPermittedTriggers()
    {
        bool isPermitted = true;
        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A)
            .InternalTransitionIf(Trigger.X, u => isPermitted, t => { });

        Assert.Equal(1, sm.GetPermittedTriggers().ToArray().Length);
        isPermitted = false;
        Assert.Equal(0, sm.GetPermittedTriggers().ToArray().Length);
    }

    [Fact]
    public void InternalTriggerHandledOnlyOnceInSuper()
    {
        State handledIn = State.C;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .InternalTransition(Trigger.X, () => handledIn = State.A);

        sm.Configure(State.B)
            .SubstateOf(State.A)
            .InternalTransition(Trigger.X, () => handledIn = State.B);

        // The state machine is in state A. It should only be handled in in State A, so handledIn should be equal to State.A
        sm.Fire(Trigger.X);

        Assert.Equal(State.A, handledIn);
    }
    [Fact]
    public void InternalTriggerHandledOnlyOnceInSub()
    {
        State handledIn = State.C;

        StateMachine<State, Trigger> sm = new(State.B);

        sm.Configure(State.A)
            .InternalTransition(Trigger.X, () => handledIn = State.A);

        sm.Configure(State.B)
            .SubstateOf(State.A)
            .InternalTransition(Trigger.X, () => handledIn = State.B);

        // The state machine is in state B. It should only be handled in in State B, so handledIn should be equal to State.B
        sm.Fire(Trigger.X);

        Assert.Equal(State.B, handledIn);
    }
    [Fact]
    public void OnlyOneHandlerExecuted()
    {
        int handled = 0;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .InternalTransition(Trigger.X, () => handled++)
            .InternalTransition(Trigger.Y, () => handled++);

        sm.Fire(Trigger.X);

        Assert.Equal(1, handled);

        sm.Fire(Trigger.Y);

        Assert.Equal(2, handled);
    }
    [Fact]
    public async Task AsyncHandlesNonAsyndActionAsync()
    {
        bool handled = false;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .InternalTransition(Trigger.Y, () => handled = true);

        await sm.FireAsync(Trigger.Y);

        Assert.True(handled);
    }
}
