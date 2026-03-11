using FluentAssertions;
using ShopApp.Application.DTOs.Auth;
using ShopApp.Application.DTOs.Cart;
using ShopApp.Application.DTOs.Item;
using ShopApp.Application.DTOs.Order;
using ShopApp.Application.Validators;
using ShopApp.Core.Enums;

namespace ShopApp.UnitTests.Validators;

public class ValidatorTests
{
    [Fact]
    public void RegisterDto_WithEmptyEmail_ShouldFail()
    {
        var validator = new RegisterDtoValidator();
        var dto = new RegisterDto("John", "Doe", "", "Password1!", "Password1!");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void RegisterDto_WithValidData_ShouldPass()
    {
        var validator = new RegisterDtoValidator();
        var dto = new RegisterDto("John", "Doe", "john@test.com", "Password1!", "Password1!");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterDto_WithMismatchedPasswords_ShouldFail()
    {
        var validator = new RegisterDtoValidator();
        var dto = new RegisterDto("John", "Doe", "john@test.com", "Password1!", "Different1!");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
    }

    [Fact]
    public void CreateItemDto_WithZeroPrice_ShouldFail()
    {
        var validator = new CreateItemDtoValidator();
        var dto = new CreateItemDto("Title", "Desc", 0, 1, ItemCondition.New, "Loc", Guid.NewGuid(), null);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void CreateItemDto_WithValidData_ShouldPass()
    {
        var validator = new CreateItemDtoValidator();
        var dto = new CreateItemDto("Title", "Desc", 10m, 1, ItemCondition.New, "Loc", Guid.NewGuid(), null);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddToCartDto_WithZeroQuantity_ShouldFail()
    {
        var validator = new AddToCartDtoValidator();
        var dto = new AddToCartDto(Guid.NewGuid(), 0);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateOrderDto_WithInvalidPostalCode_ShouldFail()
    {
        var validator = new CreateOrderDtoValidator();
        var dto = new CreateOrderDto("John", "Doe", "Street 1", "Warsaw", "12345");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode");
    }

    [Fact]
    public void CreateOrderDto_WithValidData_ShouldPass()
    {
        var validator = new CreateOrderDtoValidator();
        var dto = new CreateOrderDto("John", "Doe", "Street 1", "Warsaw", "00-001");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginDto_WithEmptyPassword_ShouldFail()
    {
        var validator = new LoginDtoValidator();
        var dto = new LoginDto("test@test.com", "");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }
}
