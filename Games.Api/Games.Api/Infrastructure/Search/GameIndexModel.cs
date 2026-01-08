namespace Games.Api.Infrastructure.Search
{
    public class GameIndexModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Genre { get; set; } = null!;
        public decimal Price { get; set; }
        public int Purchases { get; set; }
    }
}
