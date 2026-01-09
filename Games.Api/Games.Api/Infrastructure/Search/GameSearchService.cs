using Nest;

namespace Games.Api.Infrastructure.Search;

public class GameSearchService : IGameSearchService
{
    private readonly IElasticClient _client;

    public GameSearchService(IElasticClient client)
    {
        _client = client;
    }

    public async Task IndexGameAsync(GameIndexModel game)
    {
        await _client.IndexDocumentAsync(game);
    }

    public async Task<IEnumerable<GameIndexModel>> SearchAsync(string query)
    {
        var response = await _client.SearchAsync<GameIndexModel>(s =>
            s.Query(q =>
                q.MultiMatch(m => m
                    .Fields(f => f
                        .Field(x => x.Title)
                        .Field(x => x.Genre))
                    .Query(query)
                )
            )
        );

        return response.Documents;
    }

    public async Task<IEnumerable<GameIndexModel>> GetPopularGamesAsync()
    {
        var response = await _client.SearchAsync<GameIndexModel>(s =>
            s.Sort(ss => ss
                .Descending(f => f.Purchases)
            )
            .Size(10)
        );

        return response.Documents;
    }
    public async Task<IEnumerable<GameIndexModel>> RecommendByGenreAsync(string genre)
    {
        var response = await _client.SearchAsync<GameIndexModel>(s =>
            s.Query(q =>
                q.Match(m => m
                    .Field(f => f.Genre)
                    .Query(genre)
                )
            )
            .Size(10)
        );

        return response.Documents;
    }

}
