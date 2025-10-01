namespace OnlineSohppingSystem.Application.DTOs.Product;

public class ProductSearchDto
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsSecondHand { get; set; }
    public bool? IsFromStore { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? SortBy { get; set; } 
    public bool IsDescending { get; set; } = false;
}

