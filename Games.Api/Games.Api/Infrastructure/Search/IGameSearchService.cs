namespace Games.Api.Infrastructure.Search;

public interface IGameSearchService
{
    Task IndexGameAsync(GameIndexModel game);
    Task<IEnumerable<GameIndexModel>> SearchAsync(string query);
    Task<IEnumerable<GameIndexModel>> GetPopularGamesAsync();

    Task<IEnumerable<GameIndexModel>> RecommendByGenreAsync(string genre);
}
