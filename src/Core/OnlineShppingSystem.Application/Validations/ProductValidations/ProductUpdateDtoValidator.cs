using FluentValidation;
using OnlineSohppingSystem.Application.Features.Products.Commands;

namespace OnlineShppingSystem.Application.Validations.ProductValidations;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Məhsul adı boş ola bilməz");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Qiymət 0-dan böyük olmalıdır");

        RuleFor(x => x.StockQuantity)
            .GreaterThan(0).WithMessage("Stok 0-dan böyük olmalıdır");

        RuleFor(x => x)
            .Must(x => x.IsFromStore != x.IsSecondHand)
            .WithMessage("IsFromStore və IsSecondHand sahələrindən yalnız biri true ola bilər");
    }
}
