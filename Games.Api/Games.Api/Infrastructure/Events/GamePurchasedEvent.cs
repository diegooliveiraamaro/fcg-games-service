namespace Games.Api.Infrastructure.Events;

public class GamePurchasedEvent : IEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "GamePurchased";

    public Guid GameId { get; set; }
    public Guid UserId { get; set; }
}
