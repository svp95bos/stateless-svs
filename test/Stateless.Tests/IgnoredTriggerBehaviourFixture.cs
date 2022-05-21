using System;

using Xunit;

namespace Stateless.Tests
{
    public class IgnoredTriggerBehaviourFixture
    {
        [Fact]
        public void StateRemainsUnchanged()
        {
            StateMachine<State, Trigger>.IgnoredTriggerBehaviour ignored = new(Trigger.X, null);
            Assert.False(ignored.ResultsInTransitionFrom(State.B, new object[0], out _));
        }

        [Fact]
        public void ExposesCorrectUnderlyingTrigger()
        {
            StateMachine<State, Trigger>.IgnoredTriggerBehaviour ignored = new(
                Trigger.X, null);

            Assert.Equal(Trigger.X, ignored.Trigger);
        }

        protected bool False(params object[] args)
        {
            return false;
        }

        [Fact]
        public void WhenGuardConditionFalse_IsGuardConditionMetIsFalse()
        {
            StateMachine<State, Trigger>.IgnoredTriggerBehaviour ignored = new(
                Trigger.X, new StateMachine<State, Trigger>.TransitionGuard(False));

            Assert.False(ignored.GuardConditionsMet());
        }

        protected bool True(params object[] args)
        {
            return true;
        }

        [Fact]
        public void WhenGuardConditionTrue_IsGuardConditionMetIsTrue()
        {
            StateMachine<State, Trigger>.IgnoredTriggerBehaviour ignored = new(
                Trigger.X, new StateMachine<State, Trigger>.TransitionGuard(True));

            Assert.True(ignored.GuardConditionsMet());
        }
        [Fact]
        public void IgnoredTriggerMustBeIgnoredSync()
        {
            bool internalActionExecuted = false;
            StateMachine<State, Trigger> stateMachine = new(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .Ignore(Trigger.X);

            try
            {
                // >>> The following statement should not execute the internal action
                stateMachine.Fire(Trigger.X);
            }
            catch (NullReferenceException)
            {
                internalActionExecuted = true;
            }

            Assert.False(internalActionExecuted);
        }

        [Fact]
        public void IgnoreIfTrueTriggerMustBeIgnored()
        {
            StateMachine<State, Trigger> stateMachine = new(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .IgnoreIf(Trigger.X, () => true);

            stateMachine.Fire(Trigger.X);

            Assert.Equal(State.B, stateMachine.State);
        }
        [Fact]
        public void IgnoreIfFalseTriggerMustNotBeIgnored()
        {
            StateMachine<State, Trigger> stateMachine = new(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .IgnoreIf(Trigger.X, () => false);

            stateMachine.Fire(Trigger.X);

            Assert.Equal(State.C, stateMachine.State);
        }
    }
}
