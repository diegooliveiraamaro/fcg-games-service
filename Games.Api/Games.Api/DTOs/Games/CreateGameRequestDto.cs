namespace Games.Api.Application.DTOs.Games;

public class CreateGameRequestDto
{
    public string Title { get; set; } = default!;
    public string Genre { get; set; } = default!;
    public decimal Price { get; set; }
}
