namespace Games.Api.Domain;

public class StoredEvent
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Data { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
}
