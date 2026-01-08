namespace Games.Api.Domain;

public class Purchase
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Guid UserId { get; set; }
    public DateTime PurchasedAt { get; set; }
}
