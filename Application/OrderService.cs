using OrderGenerator.Infrastructure;
using OrderGenerator.Models;

namespace OrderGenerator.Application
{
    public class OrderService
    {
        private readonly FixSessionManager _fixManager;

        public OrderService(FixSessionManager fixManager)
        {
            _fixManager = fixManager;
        }

        public async Task<FixOrderResultDto> SendOrderAsync(OrderDto dto)
        {
            return await _fixManager.SendNewOrderAsync(dto);
        }
    }
}
