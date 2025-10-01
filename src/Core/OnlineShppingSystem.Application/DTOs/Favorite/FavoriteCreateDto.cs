using System.ComponentModel.DataAnnotations;

namespace OnlineSohppingSystem.Application.DTOs.Favorite;

public sealed class FavoriteCreateDto
{
    [Required]
    public Guid ProductId { get; set; }
}
