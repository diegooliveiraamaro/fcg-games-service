namespace Games.Api.Infrastructure.Events;

public class GameCreatedEvent : IEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "GameCreated";

    public Guid GameId { get; set; }
    public string Title { get; set; } = default!;
    public string Genre { get; set; } = default!;
}
