using System;

using Xunit;

namespace Stateless.Tests
{
    public class TriggerWithParametersFixture
    {
        [Fact]
        public void DescribesUnderlyingTrigger()
        {
            StateMachine<State, Trigger>.TriggerWithParameters<string> twp = new(Trigger.X);
            Assert.Equal(Trigger.X, twp.Trigger);
        }

        [Fact]
        public void ParametersOfCorrectTypeAreAccepted()
        {
            StateMachine<State, Trigger>.TriggerWithParameters<string> twp = new(Trigger.X);
            twp.ValidateParameters(new[] { "arg" });
        }

        [Fact]
        public void ParametersArePolymorphic()
        {
            StateMachine<State, Trigger>.TriggerWithParameters<object> twp = new(Trigger.X);
            twp.ValidateParameters(new[] { "arg" });
        }

        [Fact]
        public void IncompatibleParametersAreNotValid()
        {
            StateMachine<State, Trigger>.TriggerWithParameters<string> twp = new(Trigger.X);
            Assert.Throws<ArgumentException>(() => twp.ValidateParameters(new object[] { 123 }));
        }

        [Fact]
        public void TooFewParametersDetected()
        {
            StateMachine<State, Trigger>.TriggerWithParameters<string, string> twp = new(Trigger.X);
            Assert.Throws<ArgumentException>(() => twp.ValidateParameters(new[] { "a" }));
        }

        [Fact]
        public void TooManyParametersDetected()
        {
            StateMachine<State, Trigger>.TriggerWithParameters<string, string> twp = new(Trigger.X);
            Assert.Throws<ArgumentException>(() => twp.ValidateParameters(new[] { "a", "b", "c" }));
        }

        /// <summary>
        /// issue #380 - default params on PermitIfDynamic lead to ambiguity at compile time... explicits work properly.
        /// </summary>
        [Fact]
        public void StateParameterIsNotAmbiguous()
        {
            StateMachine<State, Trigger> fsm = new(State.A);
            StateMachine<State, Trigger>.TriggerWithParameters<State> pressTrigger = fsm.SetTriggerParameters<State>(Trigger.X);

            fsm.Configure(State.A)
                .PermitDynamicIf(pressTrigger, state => state);
        }

        [Fact]
        public void IncompatibleParameterListIsNotValid()
        {
            StateMachine<State, Trigger>.TriggerWithParameters twp = new(Trigger.X, new Type[] { typeof(int), typeof(string) });
            Assert.Throws<ArgumentException>(() => twp.ValidateParameters(new object[] { 123 }));
        }

        [Fact]
        public void ParameterListOfCorrectTypeAreAccepted()
        {
            StateMachine<State, Trigger>.TriggerWithParameters twp = new(Trigger.X, new Type[] { typeof(int), typeof(string) });
            twp.ValidateParameters(new object[] { 123, "arg" });
        }
    }
}
