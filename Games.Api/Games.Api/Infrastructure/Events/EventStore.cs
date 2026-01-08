using Games.Api.Domain;
using Games.Api.Infrastructure.Persistence;
using System.Text.Json;

namespace Games.Api.Infrastructure.Events;

public class EventStore
{
    private readonly GamesDbContext _db;

    public EventStore(GamesDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(IEvent @event)
    {
        var storedEvent = new StoredEvent
        {
            Id = @event.Id,
            Type = @event.Type,
            OccurredAt = @event.OccurredAt,
            Data = JsonSerializer.Serialize(@event)
        };

        _db.Set<StoredEvent>().Add(storedEvent);
        await _db.SaveChangesAsync();
    }
}
