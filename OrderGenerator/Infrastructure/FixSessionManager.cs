using OrderGenerator.Infrastructure.Fix;
using OrderGenerator.Models;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using System.Collections.Concurrent;

namespace OrderGenerator.Infrastructure
{
    public class FixSessionManager
    {
        private readonly FixClientApp _application;
        private readonly IInitiator _initiator;

        public FixSessionManager()
        {
            var settings = new SessionSettings("Infrastructure/Fix/initiator.cfg");
            _application = new FixClientApp();
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new FileLogFactory(settings);
            _initiator = new SocketInitiator(_application, storeFactory, settings, logFactory);
            _initiator.Start();
        }

        public static readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingResponses = new();

        public async Task<string> SendNewOrderAsync(OrderDto dto)
        {
            var sessionId = new SessionID("FIX.4.4", "ORDER_GENERATOR", "ORDER_ACCUMULATOR");
            var session = Session.LookupSession(sessionId);

            if (session == null || !session.IsLoggedOn)
                return "Sessão FIX não está conectada.";

            var orderId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>();
            _pendingResponses[orderId] = tcs;

            var newOrder = new NewOrderSingle(
                new ClOrdID(orderId),
                new Symbol(dto.Symbol),
                new Side(dto.Side == "Compra" ? Side.BUY : Side.SELL),
                new TransactTime(DateTime.UtcNow),
                new OrdType(OrdType.LIMIT)
            );

            newOrder.Set(new OrderQty(dto.Quantity));
            newOrder.Set(new Price(dto.Price));
            newOrder.Set(new TimeInForce(TimeInForce.DAY));

            Session.SendToTarget(newOrder, sessionId);

            var timeout = Task.Delay(5000); // timeout de 5s
            var result = await Task.WhenAny(tcs.Task, timeout);

            _pendingResponses.TryRemove(orderId, out _);

            return result == tcs.Task ? tcs.Task.Result : "Sem resposta do FIX (timeout)";
            //return $"Ordem {orderId} enviada via FIX";
        }
    }
}
