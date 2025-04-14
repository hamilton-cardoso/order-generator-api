namespace OrderGenerator.Models
{
    public class OrderDto
    {
        public string Symbol { get; set; }
        public string Side { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
