using FluentValidation;
using OnlineSohppingSystem.Application.DTOs.Order;

namespace OnlineSohppingSystem.Application.Validations.OrderValidations;

public class OrderCreateDtoValidator : AbstractValidator<OrderCreateDto>
{
    
    private static readonly string[] AllowedPaymentMethods = new[] { "COD", "Card" };

    public OrderCreateDtoValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Sifariş üçün ən azı 1 məhsul seçilməlidir.")
            .Must(items => items != null && items.Select(i => i.ProductId).Distinct().Count() == items.Count)
                .WithMessage("Eyni məhsul birdən çox dəfə daxil edilə bilməz.")
            .Must(items => items == null || items.Count <= 100)
                .WithMessage("Bir sifarişdə maksimum 100 müxtəlif məhsul ola bilər.");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemCreateDtoValidator());

      
    }
}
