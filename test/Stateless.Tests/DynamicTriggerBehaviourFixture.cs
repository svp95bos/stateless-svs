using System.Linq;

using Xunit;

namespace Stateless.Tests
{
    public class DynamicTriggerBehaviour
    {
        [Fact]
        public void DestinationStateIsDynamic()
        {
            StateMachine<State, Trigger> sm = new(State.A);
            sm.Configure(State.A)
                .PermitDynamic(Trigger.X, () => State.B);

            sm.Fire(Trigger.X);

            Assert.Equal(State.B, sm.State);
        }

        [Fact]
        public void DestinationStateIsCalculatedBasedOnTriggerParameters()
        {
            StateMachine<State, Trigger> sm = new(State.A);
            StateMachine<State, Trigger>.TriggerWithParameters<int> trigger = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A)
                .PermitDynamic(trigger, i => i == 1 ? State.B : State.C);

            sm.Fire(trigger, 1);

            Assert.Equal(State.B, sm.State);
        }

        [Fact]
        public void Sdfsf()
        {
            StateMachine<State, Trigger> sm = new(State.A);
            StateMachine<State, Trigger>.TriggerWithParameters<int> trigger = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A)
                .PermitDynamicIf(trigger, (i) => i == 1 ? State.C : State.B, (i) => i == 1);

            // Should not throw
            sm.GetPermittedTriggers().ToList();

            sm.Fire(trigger, 1);
        }
    }
}
