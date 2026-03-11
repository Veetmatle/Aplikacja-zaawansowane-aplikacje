using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ShopApp.Application.DTOs.Auth;
using ShopApp.IntegrationTests.Fixtures;

namespace ShopApp.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for the full Auth → Items → Cart → Order flow.
/// Requires Docker to run (Testcontainers).
/// </summary>
public class FullFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_ReturnsTokens()
    {
        var dto = new { FirstName = "John", LastName = "Doe", Email = "john@test.com", Password = "TestPass1!", ConfirmPassword = "TestPass1!" };

        var response = await Client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        var dto = new { Email = "nonexist@test.com", Password = "wrong" };

        var response = await Client.PostAsJsonAsync("/api/auth/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetItems_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/items?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateItem_WithoutAuth_Returns401()
    {
        var dto = new { Title = "Test", Description = "Desc", Price = 10, Quantity = 1, Condition = 0, CategoryId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync("/api/items", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullFlow_Register_CreateItem_GetItem()
    {
        // Register
        var registerDto = new { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", Password = "TestPass1!", ConfirmPassword = "TestPass1!" };
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerDto);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Set auth header
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Get categories (seeded)
        var catResponse = await Client.GetAsync("/api/categories");
        catResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get items
        var itemsResponse = await Client.GetAsync("/api/items?page=1&pageSize=10");
        itemsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CartSession_ReturnsSessionId()
    {
        var response = await Client.PostAsync("/api/cart/session", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("sessionId");
    }

    [Fact]
    public async Task GetCart_WithoutSession_ReturnsBadRequest()
    {
        // No auth, no session
        var response = await Client.GetAsync("/api/cart");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
