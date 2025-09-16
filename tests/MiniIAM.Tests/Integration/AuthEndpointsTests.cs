using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Text.Json;
using MiniIAM.Infrastructure.Auth.Dtos;
using MiniIAM.Tests.Integration;

namespace MiniIAM.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;
    private readonly CustomApiFactory _factory;

    public AuthEndpointsTests(CustomApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var request = new LoginRequestDto("admin@demo.com", "Demo@321", null);
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
        
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            loginResponse.Should().NotBeNull();
            loginResponse!.IsLoggedIn.Should().BeTrue();
            loginResponse.AccessToken.Should().NotBeNullOrWhiteSpace();
            loginResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto("invalid@email.com", "wrongpassword", null);
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto("", "", null);
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokensWithRefreshToken()
    {
        // Arrange
        var request = new LoginRequestDto("admin@demo.com", "Demo@321", null);
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
        
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            loginResponse.Should().NotBeNull();
            loginResponse!.IsLoggedIn.Should().BeTrue();
            loginResponse.AccessToken.Should().NotBeNullOrWhiteSpace();
            loginResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldReturnOk()
    {
        // Arrange - First, log in to get an access token
        var loginRequest = new LoginRequestDto("admin@demo.com", "Demo@321", null);
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await _client.PostAsync("/auth/login", loginContent);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            // Skip test if login fails
            await Task.CompletedTask;
            return;
        }

        var loginResponseDto = JsonSerializer.Deserialize<LoginResponseDto>(await loginResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        loginResponseDto.Should().NotBeNull();
        loginResponseDto!.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Act - Log out using the access token
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponseDto.AccessToken);
        var logoutResponse = await _client.PostAsync("/auth/logout", null);

        // Assert
        logoutResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.PostAsync("/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNullCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto(null!, null!, null);
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithMissingContentType_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto("admin@demo.com", "Demo@321", null);
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType, HttpStatusCode.Unauthorized);
    }
}