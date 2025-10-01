using System.ComponentModel.DataAnnotations;

namespace OnlineSohppingSystem.Application.DTOs.Review;

public sealed class ReviewCreateDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, MaxLength(1000)]
    public string Comment { get; set; } = null!;
}
