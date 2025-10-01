using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Shared.Helpers; 
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Features.Products.Commands;
using OnlineSohppingSystem.Application.Features.Products.Queries;

namespace OnlineShppingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ProductController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] ProductFilterDto dto)
    {
        var result = await _mediator.Send(new SearchProductsQuery { Filter = dto });
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("second-hand")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSecondHandProducts()
    {
        var result = await _mediator.Send(new GetSecondHandProductsQuery());
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("store-products")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStoreProducts()
    {
        var result = await _mediator.Send(new GetStoreProductsQuery());
        return StatusCode((int)result.StatusCode, result);
    }

  
    [HttpPost]
    [Authorize(Policy = Permissions.Products.Create)]
    public async Task<IActionResult> Create([FromForm] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.Products.Update)]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateProductCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("my")]
    [Authorize(Policy = Permissions.Products.ReadMy)]
    public async Task<IActionResult> GetMyProducts()
    {
        var result = await _mediator.Send(new GetMyProductsQuery { UserId = _currentUser.UserId });
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.Products.ReadById)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery
        {
            Id = id,
            UserId = _currentUser.UserId
        });
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Products.Delete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new SoftDeleteProductCommand
        {
            Id = id,
            UserId = _currentUser.UserId
        });
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("restore/{id:guid}")]
    [Authorize(Policy = Permissions.Products.Restore)]
    public async Task<IActionResult> Restore(Guid id)
    {
        var result = await _mediator.Send(new RestoreProductCommand
        {
            Id = id,
            UserId = _currentUser.UserId
        });
        return StatusCode((int)result.StatusCode, result);
    }
}
