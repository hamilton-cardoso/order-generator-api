using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Application;
using OrderGenerator.Models;

namespace OrderGenerator.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService) => _orderService = orderService;

        [HttpPost]
        public async Task<IActionResult> PostOrder([FromBody] OrderDto dto)
        {
            var result = await _orderService.SendOrderAsync(dto);

            if (result.Status == "ERRO")
                return StatusCode(503, result);

            return Ok(result);
        }
    }
}
