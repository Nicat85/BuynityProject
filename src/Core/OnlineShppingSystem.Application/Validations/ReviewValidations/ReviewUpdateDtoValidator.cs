using FluentValidation;
using OnlineSohppingSystem.Application.DTOs.Review;

namespace OnlineSohppingSystem.Application.Validations.ReviewValidations;

public sealed class ReviewUpdateDtoValidator : AbstractValidator<ReviewUpdateDto>
{
    public ReviewUpdateDtoValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(1000);
    }
}