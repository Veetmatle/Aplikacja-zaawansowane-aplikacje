using FluentAssertions;
using NSubstitute;
using ShopApp.Application.DTOs.Item;
using ShopApp.Application.Services;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;
using ShopApp.Core.Interfaces.Repositories;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.UnitTests.Services;

public class ItemServiceTests
{
    private readonly IItemRepository _itemRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IViewCountTracker _viewCountTracker;
    private readonly IFileStorageService _fileStorage;
    private readonly ItemService _sut;

    public ItemServiceTests()
    {
        _itemRepo = Substitute.For<IItemRepository>();
        _categoryRepo = Substitute.For<ICategoryRepository>();
        _viewCountTracker = Substitute.For<IViewCountTracker>();
        _fileStorage = Substitute.For<IFileStorageService>();
        _sut = new ItemService(_itemRepo, _categoryRepo, _viewCountTracker, _fileStorage);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        _itemRepo.GetWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Item?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_TracksViewCount()
    {
        var item = CreateTestItem();
        _itemRepo.GetWithDetailsAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _sut.GetByIdAsync(item.Id);

        result.IsSuccess.Should().BeTrue();
        _viewCountTracker.Received(1).Track(item.Id);
        // ViewCount is NOT incremented synchronously in memory anymore
        await _itemRepo.DidNotReceive().UpdateAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryNotFound_ReturnsFailure()
    {
        var dto = new CreateItemDto("Test", "Desc", 10m, 1, ItemCondition.New, "Warsaw", Guid.NewGuid(), null);
        _categoryRepo.GetByIdAsync(dto.CategoryId, Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var result = await _sut.CreateAsync(Guid.NewGuid(), dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Category not found");
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsCreatedItem()
    {
        var sellerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var dto = new CreateItemDto("Test Item", "Description", 99.99m, 5, ItemCondition.New, "Warsaw", categoryId, null);

        _categoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(new Category { Id = categoryId, Name = "Electronics", Slug = "electronics" });

        _itemRepo.AddAsync(Arg.Any<Item>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Item>());

        _itemRepo.GetWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var item = CreateTestItem();
                item.Title = dto.Title;
                item.Price = dto.Price;
                return item;
            });

        var result = await _sut.CreateAsync(sellerId, dto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Test Item");
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwner_ReturnsForbidden()
    {
        var item = CreateTestItem();
        _itemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _sut.DeleteAsync(item.Id, Guid.NewGuid()); // different userId

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteAsync_WhenOwner_ReturnsSuccess()
    {
        var item = CreateTestItem();
        _itemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _sut.DeleteAsync(item.Id, item.SellerId);

        result.IsSuccess.Should().BeTrue();
        await _itemRepo.Received(1).DeleteAsync(item, Arg.Any<CancellationToken>());
    }

    private static Item CreateTestItem() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Test Item",
        Description = "Test Description",
        Price = 49.99m,
        Quantity = 10,
        SellerId = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        Category = new Category { Name = "Test", Slug = "test" },
        Seller = new ApplicationUser { FirstName = "John", LastName = "Doe" },
        Photos = new List<ItemPhoto>()
    };
}
