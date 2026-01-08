namespace Games.Api.Domain
{
    public class Game
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;
        public string Genre { get; set; } = null!;
        public decimal Price { get; set; }

        // ✅ Quantidade de compras (métricas / popularidade)
        public int Purchases { get; set; } = 0;
    }
}
