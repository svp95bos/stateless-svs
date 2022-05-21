using System.Threading.Tasks;

using Xunit;

namespace Stateless.Tests;

public class InternalTransitionAsyncFixture
{
    /// <summary>
    /// This unit test demonstrated bug report #417
    /// </summary>
    [Fact]
    public async Task InternalTransitionAsyncIf_GuardExecutedOnlyOnce()
    {
        int guardCalls = 0;
        Order order = new()
        {
            Status = OrderStatus.OrderPlaced,
            PaymentStatus = PaymentStatus.Pending,
        };
        StateMachine<OrderStatus, OrderStateTrigger> stateMachine = new(order.Status);
        stateMachine.Configure(OrderStatus.OrderPlaced)
             .InternalTransitionAsyncIf(OrderStateTrigger.PaymentCompleted,
                   () => PreCondition(ref guardCalls),
                   () => ChangePaymentState(order, PaymentStatus.Completed));

        await stateMachine.FireAsync(OrderStateTrigger.PaymentCompleted);

        Assert.Equal(1, guardCalls);
    }

    private static bool PreCondition(ref int calls)
    {
        calls++;
        return true;
    }

    private static async Task ChangePaymentState(Order order, PaymentStatus paymentStatus) => await Task.FromResult(order.PaymentStatus = paymentStatus);

    private enum OrderStatus { OrderPlaced }
    private enum PaymentStatus { Pending, Completed }
    private enum OrderStateTrigger { PaymentCompleted }
    private class Order
    {
        public OrderStatus Status { get; internal set; }
        public PaymentStatus PaymentStatus { get; internal set; }
    }
}
