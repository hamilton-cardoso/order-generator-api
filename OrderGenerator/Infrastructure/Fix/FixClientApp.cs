using static System.Net.Mime.MediaTypeNames;
using QuickFix;
using System.Collections.Concurrent;
using QuickFix.Fields;

namespace OrderGenerator.Infrastructure.Fix
{
    public class FixClientApp : MessageCracker, IApplication
    {
        private readonly ConcurrentDictionary<SessionID, bool> _logonSessions = new();

        public bool IsSessionLoggedOn(SessionID sessionID)
        {
            return _logonSessions.TryGetValue(sessionID, out var isLoggedOn) && isLoggedOn;
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            Console.WriteLine($"[RECEBIDO] Tipo: {message.GetType()} - Msg: {message.ToString()}");
            try
            {
                Crack(message, sessionID);
            }
            catch (UnsupportedMessageType e)
            {
                Console.WriteLine($"[ERRO] UnsupportedMessageType: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Erro genérico ao processar mensagem: {ex.Message}");
            }
        }

        private void ProcessExecutionReport(QuickFix.FIX44.ExecutionReport message)
        {
            try
            {
                // Vamos acessar o OrdStatus de uma forma diferente
                if (message.IsSetField(QuickFix.Fields.Tags.OrdStatus))
                {
                    char ordStatus = message.GetChar(QuickFix.Fields.Tags.OrdStatus);

                    switch (ordStatus)
                    {
                        case OrdStatus.NEW:
                            Console.WriteLine($"[ORDEM] Nova ordem aceita. OrderID: {message.GetString(QuickFix.Fields.Tags.OrderID)}");
                            break;
                        case OrdStatus.PARTIALLY_FILLED:
                            Console.WriteLine($"[ORDEM] Ordem parcialmente executada. OrderID: {message.GetString(QuickFix.Fields.Tags.OrderID)}, Quantidade executada: {message.GetDecimal(QuickFix.Fields.Tags.CumQty)}");
                            break;
                        case OrdStatus.FILLED:
                            Console.WriteLine($"[ORDEM] Ordem totalmente executada. OrderID: {message.GetString(QuickFix.Fields.Tags.OrderID)}, Quantidade total: {message.GetDecimal(QuickFix.Fields.Tags.CumQty)}");
                            break;
                        case OrdStatus.REJECTED:
                            string motivo = "Sem motivo especificado";
                            if (message.IsSetField(QuickFix.Fields.Tags.Text))
                            {
                                motivo = message.GetString(QuickFix.Fields.Tags.Text);
                            }
                            Console.WriteLine($"[ORDEM] Ordem rejeitada. OrderID: {message.GetString(QuickFix.Fields.Tags.OrderID)}, Motivo: {motivo}");
                            break;
                        case OrdStatus.CANCELED:
                            Console.WriteLine($"[ORDEM] Ordem cancelada. OrderID: {message.GetString(QuickFix.Fields.Tags.OrderID)}");
                            break;
                        default:
                            Console.WriteLine($"[ORDEM] Status da ordem: {ordStatus}. OrderID: {message.GetString(QuickFix.Fields.Tags.OrderID)}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Erro ao processar ExecutionReport: {ex.Message}");
            }
        }

        public void OnMessage(QuickFix.FIX44.ExecutionReport message, SessionID sessionID)
        {
            var execType = message.ExecType.getValue();
            var orderId = message.ClOrdID.getValue();

            string status;

            if (execType == ExecType.REJECTED)
            {
                var motivo = message.IsSetField(Tags.Text) ? message.GetString(Tags.Text) : "Motivo não informado";
                status = $"REJEITADA: {motivo}";
            }
            else if (execType == ExecType.NEW)
            {
                status = "ACEITA";
            }
            else
            {
                status = $"STATUS DESCONHECIDO: {execType}";
            }

            if (FixSessionManager._pendingResponses.TryGetValue(orderId, out var tcs))
            {
                tcs.TrySetResult(status);
            }
        }

        public void OnMessage(QuickFix.FIX44.OrderCancelReject message, SessionID sessionID)
        {
            ProcessOrderCancelReject(message);
        }


        private void ProcessOrderCancelReject(QuickFix.FIX44.OrderCancelReject message)
        {
            try
            {
                string motivo = "Sem motivo especificado";
                if (message.IsSetField(QuickFix.Fields.Tags.Text))
                {
                    motivo = message.GetString(QuickFix.Fields.Tags.Text);
                }

                Console.WriteLine($"[ORDEM] Cancelamento rejeitado. OrderID: {message.GetString(QuickFix.Fields.Tags.OrderID)}, Motivo: {motivo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Erro ao processar OrderCancelReject: {ex.Message}");
            }
        }

        public void OnCreate(SessionID sessionID) { }

        public void OnLogon(SessionID sessionID)
        {
            Console.WriteLine($"[CLIENT] Logon na sessão: {sessionID}");
            _logonSessions[sessionID] = true;
        }

        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine($"[CLIENT] Logout na sessão: {sessionID}");
            _logonSessions[sessionID] = false;
        }

        public void ToAdmin(Message message, SessionID sessionID) { }

        public void FromAdmin(Message message, SessionID sessionID) { }

        public void ToApp(Message message, SessionID sessionID) { }
    }
}
