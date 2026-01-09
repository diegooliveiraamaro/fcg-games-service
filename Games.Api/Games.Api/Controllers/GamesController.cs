using Amazon.EventBridge;
using Amazon.Lambda;
using Games.Api.Application.DTOs.Games;
using Games.Api.Domain;
using Games.Api.Infrastructure.Persistence;
using Games.Api.Infrastructure.Search;
using Microsoft.AspNetCore.Mvc;

namespace Games.Api.Controllers
{
    [ApiController]
    [Route("games")]
    public class GamesController : ControllerBase
    {
        private readonly ILogger<GamesController> _logger;
        private readonly GamesDbContext _db;
        private readonly IGameSearchService _search;
        private readonly IAmazonLambda _lambdaClient;

        public GamesController(
            GamesDbContext db,
            IGameSearchService search,
            IAmazonLambda lambdaClient,
            ILogger<GamesController> logger)
        {
            _db = db;
            _search = search;
            _lambdaClient = lambdaClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_db.Games.ToList());
        }

        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            var game = _db.Games.Find(id);
            if (game == null) return NotFound();

            return Ok(game);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateGameRequestDto dto)
        {
            var game = new Game
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Genre = dto.Genre,
                Price = dto.Price,
                Purchases = 0
            };

            _db.Games.Add(game);
            await _db.SaveChangesAsync();

            // 🔎 Indexa no Elasticsearch
            var indexModel = new GameIndexModel
            {
                Id = game.Id,
                Title = game.Title,
                Genre = game.Genre,
                Price = game.Price,
                Purchases = 0
            };

            await _search.IndexGameAsync(indexModel);

            return CreatedAtAction(nameof(GetById), new { id = game.Id }, game);
        }


        [HttpPost("{id}/purchase")]
        public async Task<IActionResult> Purchase(Guid id, [FromQuery] Guid userId, [FromServices] IAmazonEventBridge eventBridge)
        {
            var game = await _db.Games.FindAsync(id);
            if (game == null) return NotFound();

            game.Purchases++;

            var purchase = new Purchase
            {
                Id = Guid.NewGuid(),
                GameId = id,
                UserId = userId,
                PurchasedAt = DateTime.UtcNow
            };

            _db.Purchases.Add(purchase);
            await _db.SaveChangesAsync();

            await _search.IndexGameAsync(new GameIndexModel
            {
                Id = game.Id,
                Title = game.Title,
                Genre = game.Genre,
                Price = game.Price,
                Purchases = game.Purchases
            });

            // 🔔 Evento
            var gamePurchasedEvent = new
            {
                GameId = game.Id,
                UserId = userId,
                PurchasedAt = purchase.PurchasedAt
            };

            var request = new Amazon.EventBridge.Model.PutEventsRequest
            {
                Entries = new List<Amazon.EventBridge.Model.PutEventsRequestEntry>
                {
                    new()
                    {
                        Source = "fcg.games",
                        DetailType = "GamePurchased",
                        Detail = System.Text.Json.JsonSerializer.Serialize(gamePurchasedEvent),
                        EventBusName = "fcg-event-bus"
                    }
                }
            };

            await eventBridge.PutEventsAsync(request);

            return Ok();
        }


        //[HttpPost("{id}/purchase")]
        //public async Task<IActionResult> Purchase(Guid id, [FromQuery] Guid userId)
        //{
        //    var game = await _db.Games.FindAsync(id);
        //    if (game == null)
        //        return NotFound();

        //    game.Purchases++;

        //    var purchase = new Purchase
        //    {
        //        Id = Guid.NewGuid(),
        //        GameId = id,
        //        UserId = userId,
        //        PurchasedAt = DateTime.UtcNow
        //    };

        //    _db.Purchases.Add(purchase);

        //    await _db.SaveChangesAsync();

        //    await _search.IndexGameAsync(new GameIndexModel
        //    {
        //        Id = game.Id,
        //        Title = game.Title,
        //        Genre = game.Genre,
        //        Price = game.Price,
        //        Purchases = game.Purchases
        //    });

        //    var payload = new
        //    {
        //        UserId = userId,
        //        GameId = game.Id,
        //        PurchasedAt = purchase.PurchasedAt
        //    };

        //    try
        //    {
        //        var request = new Amazon.Lambda.Model.InvokeRequest
        //        {
        //            FunctionName = "fcg-game-purchase-notification",
        //            Payload = System.Text.Json.JsonSerializer.Serialize(payload)
        //        };

        //        await _lambdaClient.InvokeAsync(request);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao chamar Lambda de notificação");
        //    }

        //    return Ok();
        //}


        //[HttpPost("{id}/purchase")]
        //public async Task<IActionResult> Purchase(Guid id, [FromQuery] Guid userId)
        //{
        //    // 1️⃣ Buscar o jogo
        //    var game = await _db.Games.FindAsync(id);
        //    if (game == null) return NotFound();

        //    // 2️⃣ Incrementar contagem de compras
        //    game.Purchases++;

        //    // 3️⃣ Criar registro de compra
        //    var purchase = new Purchase
        //    {
        //        Id = Guid.NewGuid(),
        //        GameId = id,
        //        UserId = userId,
        //        PurchasedAt = DateTime.UtcNow
        //    };
        //    _db.Purchases.Add(purchase);

        //    // 4️⃣ Salvar alterações no banco
        //    await _db.SaveChangesAsync();

        //    // 5️⃣ Atualizar índice no Elasticsearch
        //    await _search.IndexGameAsync(new GameIndexModel
        //    {
        //        Id = game.Id,
        //        Title = game.Title,
        //        Genre = game.Genre,
        //        Price = game.Price,
        //        Purchases = game.Purchases
        //    });

        //    // 6️⃣ Chamar Lambda para notificação
        //    var payload = new
        //    {
        //        UserId = userId,
        //        GameId = game.Id,
        //        PurchasedAt = purchase.PurchasedAt
        //    };

        //    var request = new Amazon.Lambda.Model.InvokeRequest
        //    {
        //        FunctionName = "fcg-game-purchase-notification",
        //        Payload = System.Text.Json.JsonSerializer.Serialize(payload)
        //    };

        //    await _lambdaClient.InvokeAsync(request);

        //    return Ok();
        //}


        //[HttpPost("{id}/purchase")]
        //public async Task<IActionResult> Purchase(Guid id, [FromQuery] Guid userId)
        //{
        //    var game = await _db.Games.FindAsync(id);
        //    if (game == null) return NotFound();

        //    game.Purchases++;

        //    _db.Purchases.Add(new Purchase
        //    {
        //        Id = Guid.NewGuid(),
        //        GameId = id,
        //        UserId = userId,
        //        PurchasedAt = DateTime.UtcNow
        //    });

        //    await _db.SaveChangesAsync();

        //    // Atualiza índice
        //    await _search.IndexGameAsync(new GameIndexModel
        //    {
        //        Id = game.Id,
        //        Title = game.Title,
        //        Genre = game.Genre,
        //        Price = game.Price,
        //        Purchases = game.Purchases
        //    });

        //    return Ok();
        //}

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var games = await _search.SearchAsync(query);
            return Ok(games);
        }

        [HttpGet("popular")]
        public async Task<IActionResult> Popular()
        {
            var games = await _search.GetPopularGamesAsync();
            return Ok(games);
        }
        [HttpGet("recommendations/{genre}")]
        public async Task<IActionResult> Recommend(string genre)
        {
            var games = await _search.RecommendByGenreAsync(genre);
            return Ok(games);
        }
    }
}
