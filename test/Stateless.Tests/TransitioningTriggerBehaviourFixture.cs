using Xunit;

namespace Stateless.Tests;

public class TransitioningTriggerBehaviourFixture
{
    [Fact]
    public void TransitionsToDestinationState()
    {
        StateMachine<State, Trigger>.TransitioningTriggerBehaviour transtioning = new(Trigger.X, State.C, null);
        Assert.True(transtioning.ResultsInTransitionFrom(State.B, new object[0], out State destination));
        Assert.Equal(State.C, destination);
    }
}
