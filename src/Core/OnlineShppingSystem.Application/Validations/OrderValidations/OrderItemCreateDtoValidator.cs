using FluentValidation;
using OnlineSohppingSystem.Application.DTOs.Order;

namespace OnlineSohppingSystem.Application.Validations.OrderValidations;

public class OrderItemCreateDtoValidator : AbstractValidator<OrderItemCreateDto>
{
    public OrderItemCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Məhsul Id boş ola bilməz.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miqdar 0-dan böyük olmalıdır.")
            .LessThanOrEqualTo(1000).WithMessage("Bir məhsul üçün maksimum 1000 ədəd sifariş verilə bilər.");
    }
}
