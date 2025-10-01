using FluentValidation;
using OnlineSohppingSystem.Application.DTOs.Favorite;

namespace OnlineSohppingSystem.Application.Validations.FavoriteValidations;

public sealed class FavoriteCreateDtoValidator : AbstractValidator<FavoriteCreateDto>
{
    public FavoriteCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId boş ola bilməz.");
    }
}
