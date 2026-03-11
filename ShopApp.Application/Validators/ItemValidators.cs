using FluentValidation;
using ShopApp.Application.DTOs.Item;

namespace ShopApp.Application.Validators;

public class CreateItemDtoValidator : AbstractValidator<CreateItemDto>
{
    public CreateItemDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(5000);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Condition)
            .IsInEnum().WithMessage("Invalid item condition.");
    }
}

public class UpdateItemDtoValidator : AbstractValidator<UpdateItemDto>
{
    public UpdateItemDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(300)
            .When(x => x.Title is not null);

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1.")
            .When(x => x.Quantity.HasValue);

        RuleFor(x => x.Condition)
            .IsInEnum()
            .When(x => x.Condition.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);
    }
}

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);
    }
}

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);
    }
}
