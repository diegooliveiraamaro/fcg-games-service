using Amazon.EventBridge;
using Amazon.Lambda;
using Games.Api.Application.DTOs.Games;
using Games.Api.Domain;
using Games.Api.Infrastructure.Persistence;
using Games.Api.Infrastructure.Search;
using Games.Api.Messaging;
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
        private readonly RabbitPublisher _publisher;


        public GamesController(
            GamesDbContext db,
            IGameSearchService search,
            ILogger<GamesController> logger,
            RabbitPublisher publisher)

        {
            _db = db;
            _search = search;
            _logger = logger;
            _publisher = publisher;

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

            return CreatedAtAction(nameof(GetById), new { id = game.Id }, game);
        }


        [HttpPost("{id}/purchase")]
        public async Task<IActionResult> Purchase(Guid id, [FromQuery] Guid userId)//, [FromServices] IAmazonEventBridge eventBridge)
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

            _publisher.Publish(gamePurchasedEvent);            

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

            return Ok();
        }
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
