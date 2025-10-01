namespace OnlineSohppingSystem.Application.DTOs.Review;

public sealed class ReviewSummaryDto
{
    public Guid ProductId { get; set; }
    public int Count { get; set; }
    public double Average { get; set; }
    public Dictionary<int, int> Distribution { get; set; } = new() 
    {
        [1] = 0,
        [2] = 0,
        [3] = 0,
        [4] = 0,
        [5] = 0
    };
}
