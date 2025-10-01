using OnlineShppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.DTOs.Product;

public class ProductFilterDto
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsSecondHand { get; set; }
    public bool? IsFromStore { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }


    public string? SortBy { get; set; } 
    public string? SortDirection { get; set; } = "asc"; 
}
