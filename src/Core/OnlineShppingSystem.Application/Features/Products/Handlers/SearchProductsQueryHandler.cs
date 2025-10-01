using MediatR;
using Microsoft.Extensions.Logging;
using Nest;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Features.Products.Queries;
using OnlineSohppingSystem.Application.Models.Elasticsearch;
using OnlineSohppingSystem.Application.Shared;
using System.Net;

namespace OnlineSohppingSystem.Application.Features.Products.Handlers;

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, BaseResponse<PagedResponse<ProductResultDto>>>
{
    private readonly IElasticClient _elasticClient;
    private readonly ILogger<SearchProductsQueryHandler> _logger;

    public SearchProductsQueryHandler(IElasticClient elasticClient, ILogger<SearchProductsQueryHandler> logger)
    {
        _elasticClient = elasticClient;
        _logger = logger;
    }

    public async Task<BaseResponse<PagedResponse<ProductResultDto>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var filterDto = request.Filter;

        var mustQueries = new List<QueryContainer>();

        if (!string.IsNullOrWhiteSpace(filterDto.Search))
        {
            mustQueries.Add(new MatchQuery
            {
                Field = "name",
                Query = filterDto.Search
            });
        }

        if (filterDto.CategoryId.HasValue && filterDto.CategoryId != Guid.Empty)
        {
            mustQueries.Add(new TermQuery
            {
                Field = "categoryId.keyword",
                Value = filterDto.CategoryId.Value.ToString()
            });
        }

        if (filterDto.IsSecondHand.HasValue)
        {
            mustQueries.Add(new TermQuery
            {
                Field = "isSecondHand",
                Value = filterDto.IsSecondHand.Value
            });
        }

        if (filterDto.IsFromStore.HasValue)
        {
            mustQueries.Add(new TermQuery
            {
                Field = "isFromStore",
                Value = filterDto.IsFromStore.Value
            });
        }

        if (filterDto.MinPrice.HasValue || filterDto.MaxPrice.HasValue)
        {
            mustQueries.Add(new NumericRangeQuery
            {
                Field = "price",
                GreaterThanOrEqualTo = filterDto.MinPrice.HasValue ? (double?)filterDto.MinPrice.Value : null,
                LessThanOrEqualTo = filterDto.MaxPrice.HasValue ? (double?)filterDto.MaxPrice.Value : null
            });
        }

        var sortField = (filterDto.SortBy?.ToLower()) switch
        {
            "name" => "name.keyword",
            "categoryname" => "categoryName.keyword",
            "price" => "price",
            _ => "name.keyword"
        };

        var sortOrder = filterDto.SortDirection?.ToLower() == "desc" ? SortOrder.Descending : SortOrder.Ascending;

        var response = await _elasticClient.SearchAsync<ProductIndexModel>(s => s
            .Index("products")
            .Query(q => q.Bool(b => b.Must(mustQueries.ToArray())))
            .Sort(ss => ss.Field(sortField, sortOrder))
            .Highlight(h => h
                .Fields(f => f
                    .Field(p => p.Name)
                    .PreTags("<em>")
                    .PostTags("</em>")
                )
            )
            .From((filterDto.Page - 1) * filterDto.PageSize)
            .Size(filterDto.PageSize)
        );

        if (!response.IsValid)
        {
            _logger.LogError("Elasticsearch query failed: {Message}", response.ServerError?.ToString());
            return BaseResponse<PagedResponse<ProductResultDto>>.Fail("Search failed", HttpStatusCode.InternalServerError);
        }

        var results = response.Hits.Select(hit =>
        {
            var doc = hit.Source;

            return new ProductResultDto
            {
                Id = doc.Id,
                Name = doc.Name,
                Description = doc.Description,
                Price = doc.Price,
                OriginalPrice = doc.OriginalPrice,
                StockQuantity = doc.StockQuantity,
                IsSecondHand = doc.IsSecondHand,
                IsFromStore = doc.IsFromStore,
                Condition = doc.Condition,
                Status = doc.Status,
                CategoryId = doc.CategoryId,
                CategoryName = doc.CategoryName,
                MainImageUrl = doc.MainImageUrl,
                CreatedAt = doc.CreatedAt,
                SellerId = doc.SellerId,
                SellerName = doc.SellerName,
                DiscountPercentage = (doc.OriginalPrice.HasValue && doc.OriginalPrice > 0)
                    ? Math.Round((double)((doc.OriginalPrice.Value - doc.Price) / doc.OriginalPrice.Value) * 100, 2)
                    : 0,
                HighlightedName = hit.Highlight.TryGetValue("name", out var highlights)
                    ? string.Join(" ", highlights)
                    : doc.Name
            };
        }).ToList();

        var paged = new PagedResponse<ProductResultDto>(results, filterDto.Page, filterDto.PageSize, response.Total);
        return BaseResponse<PagedResponse<ProductResultDto>>.CreateSuccess(paged, "Search successful", HttpStatusCode.OK);
    }
}
