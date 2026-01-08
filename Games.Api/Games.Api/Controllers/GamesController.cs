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
        private readonly GamesDbContext _db;
        private readonly IGameSearchService _search;

        public GamesController(
            GamesDbContext db,
            IGameSearchService search)
        {
            _db = db;
            _search = search;
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
        public async Task<IActionResult> Purchase(Guid id, [FromQuery] Guid userId)
        {
            var game = await _db.Games.FindAsync(id);
            if (game == null) return NotFound();

            game.Purchases++;

            _db.Purchases.Add(new Purchase
            {
                Id = Guid.NewGuid(),
                GameId = id,
                UserId = userId,
                PurchasedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Atualiza índice
            await _search.IndexGameAsync(new GameIndexModel
            {
                Id = game.Id,
                Title = game.Title,
                Genre = game.Genre,
                Price = game.Price,
                Purchases = game.Purchases
            });

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
    }
}
