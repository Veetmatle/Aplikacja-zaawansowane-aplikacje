using FluentValidation;
using ShopApp.Application.DTOs.Cart;

namespace ShopApp.Application.Validators;

public class AddToCartDtoValidator : AbstractValidator<AddToCartDto>
{
    public AddToCartDtoValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Item ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1.");
    }
}

public class UpdateCartItemDtoValidator : AbstractValidator<UpdateCartItemDto>
{
    public UpdateCartItemDtoValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must be 0 or more (0 removes item).");
    }
}
