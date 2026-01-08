using Games.Api.Domain;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Games.Api.Infrastructure.Persistence;

public class GamesDbContext : DbContext
{
    public GamesDbContext(DbContextOptions<GamesDbContext> options)
        : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();
}
