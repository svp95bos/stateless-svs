namespace Stateless;

public partial class StateMachine<TState, TTrigger>
{
    class OnTransitionedEvent
    {
        event Action<Transition> _onTransitioned;
        readonly List<Func<Transition, Task>> _onTransitionedAsync = new();

        public void Invoke(Transition transition)
        {
            if (_onTransitionedAsync.Count != 0)
            {
                throw new InvalidOperationException(
                    "Cannot execute asynchronous action specified as OnTransitioned callback. " +
                    "Use asynchronous version of Fire [FireAsync]");
            }

            _onTransitioned?.Invoke(transition);
        }

#if TASKS
        public async Task InvokeAsync(Transition transition)
        {
            _onTransitioned?.Invoke(transition);

            foreach (Func<Transition, Task> callback in _onTransitionedAsync)
            {
                await callback(transition).ConfigureAwait(false);
            }
        }
#endif

        public void Register(Action<Transition> action) => _onTransitioned += action;

        public void Register(Func<Transition, Task> action) => _onTransitionedAsync.Add(action);
    }
}
