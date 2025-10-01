using FluentValidation;
using OnlineSohppingSystem.Application.DTOs.Review;

namespace OnlineSohppingSystem.Application.Validations.ReviewValidations;

public sealed class ReviewCreateDtoValidator : AbstractValidator<ReviewCreateDto>
{
    public ReviewCreateDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(100);
    }
}