namespace Games.Api.Infrastructure.Events;

public interface IEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
    string Type { get; }
}
