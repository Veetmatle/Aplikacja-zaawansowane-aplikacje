using FluentValidation;
using ShopApp.Application.DTOs.Order;

namespace ShopApp.Application.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(300);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100);

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .MaximumLength(20)
            .Matches(@"^\d{2}-\d{3}$").WithMessage("Postal code must be in format XX-XXX.");

        RuleFor(x => x.Country)
            .MaximumLength(10);
    }
}

public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid order status.");
    }
}
