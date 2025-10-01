using FluentValidation;
using OnlineSohppingSystem.Application.DTOs.Product;

namespace OnlineShppingSystem.Application.Validations.ProductValidations;

public class ProductCreateDtoValidator : AbstractValidator<ProductCreateDto>
{
    public ProductCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Məhsul adı boş ola bilməz");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Qiymət 0-dan böyük olmalıdır");

        RuleFor(x => x.StockQuantity)
            .GreaterThan(0).WithMessage("Stok 0-dan böyük olmalıdır");

        RuleFor(x => x.Images)
            .NotNull().WithMessage("Şəkillər boş ola bilməz")
            .Must(i => i.Count >= 3).WithMessage("Ən azı 3 şəkil əlavə edilməlidir");

        RuleFor(x => x)
            .Must(x => x.IsFromStore != x.IsSecondHand)
            .WithMessage("IsSecondHand və IsFromStore sahələrindən yalnız biri true ola bilər");
    }
}

