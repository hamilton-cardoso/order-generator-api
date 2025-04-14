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

        public async Task<FixOrderResultDto> SendNewOrderAsync(OrderDto dto)
        {
            var sessionId = new SessionID("FIX.4.4", "ORDER_GENERATOR", "ORDER_ACCUMULATOR");
            var session = Session.LookupSession(sessionId);

            if (session == null || !session.IsLoggedOn)
                return new FixOrderResultDto { Status = "ERRO", Detail = "Sessão FIX não conectada." };

            var orderId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>();
            _pendingResponses[orderId] = tcs;

            try
            {
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

                Console.WriteLine($"[FIX] Enviando ordem {orderId} de {dto.Side} {dto.Quantity} {dto.Symbol} @ {dto.Price}");

                if (result == tcs.Task && tcs.Task.IsCompletedSuccessfully)
                {
                    var response = tcs.Task.Result;

                    if (response.StartsWith("REJEITADA"))
                    {
                        return new FixOrderResultDto
                        {
                            Status = "REJEITADA",
                            Detail = response.Replace("REJEITADA: ", "")
                        };
                    }

                    return new FixOrderResultDto { Status = "ACEITA" };
                }

                return new FixOrderResultDto
                {
                    Status = "ERRO",
                    Detail = "Sem resposta do FIX (timeout)."
                };
            }
            catch (ObjectDisposedException)
            {
                return new FixOrderResultDto
                {
                    Status = "ERRO",
                    Detail = "Conexão FIX foi encerrada. Tente novamente mais tarde."
                };
            }
            catch (Exception ex)
            {
                return new FixOrderResultDto
                {
                    Status = "ERRO",
                    Detail = $"Erro inesperado ao enviar ordem: {ex.Message}"
                };
            }
        }
    }
}
