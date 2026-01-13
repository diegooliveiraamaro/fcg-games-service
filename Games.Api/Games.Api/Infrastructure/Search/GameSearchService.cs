using Nest;

namespace Games.Api.Infrastructure.Search;

public class GameSearchService : IGameSearchService
{
    private const string IndexName = "games";
    private readonly IElasticClient _client;

    public GameSearchService(IElasticClient client)
    {
        _client = client;
    }

    // =======================
    // INDEXAÇÃO
    // =======================
    public async Task IndexGameAsync(GameIndexModel game)
    {
        var response = await _client.IndexAsync(game, i => i
            .Index(IndexName)
            .Refresh(Elasticsearch.Net.Refresh.WaitFor)
        );

        if (!response.IsValid)
            throw new Exception($"Erro ao indexar jogo: {response.DebugInformation}");
    }

    // =======================
    // BUSCA TEXTO LIVRE
    // =======================
    public async Task<IEnumerable<GameIndexModel>> SearchAsync(string query)
    {
        var response = await _client.SearchAsync<GameIndexModel>(s =>
            s.Index(IndexName)
             .Query(q =>
                q.MultiMatch(m => m
                    .Fields(f => f
                        .Field(x => x.Title)
                        .Field(x => x.Genre)
                    )
                    .Query(query)
                )
             )
        );

        return response.Documents;
    }

    // =======================
    // JOGOS POPULARES
    // =======================
    public async Task<IEnumerable<GameIndexModel>> GetPopularGamesAsync()
    {
        var response = await _client.SearchAsync<GameIndexModel>(s =>
            s.Index(IndexName)
             .Sort(ss => ss.Descending(f => f.Purchases))
             .Size(10)
        );

        return response.Documents;
    }

    // =======================
    // RECOMENDAÇÃO POR GÊNERO
    // =======================
    public async Task<IEnumerable<GameIndexModel>> RecommendByGenreAsync(string genre)
    {
        var response = await _client.SearchAsync<GameIndexModel>(s =>
            s.Index(IndexName)
             .Query(q =>
                q.Match(m => m
                    .Field(f => f.Genre)
                    .Query(genre)
                )
             )
             .Size(10)
        );

        return response.Documents;
    }

    // =======================
    // CRIAÇÃO DO ÍNDICE
    // =======================
    public static async Task EnsureElasticIndexAsync(IElasticClient client)
    {
        var exists = await client.Indices.ExistsAsync(IndexName);
        if (exists.Exists) return;

        var response = await client.Indices.CreateAsync(IndexName, c => c
            .Map<GameIndexModel>(m => m
                .AutoMap()
                .Properties(ps => ps
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer("standard")
                    )
                    .Text(t => t
                        .Name(n => n.Genre)
                        .Analyzer("standard")
                    )
                    .Number(n => n
                        .Name(p => p.Purchases)
                        .Type(NumberType.Integer)
                    )
                )
            )
        );

        if (!response.IsValid)
            throw new Exception($"Erro ao criar índice: {response.DebugInformation}");
    }
}
