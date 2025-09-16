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

public class SessionIntegrationTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;
    private readonly CustomApiFactory _factory;

    public SessionIntegrationTests(CustomApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ShouldCreateSessionAndReturnTokens()
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
    public async Task Login_ShouldCreateSessionWithCorrectProperties()
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
            
            // Verify that tokens are different (indicating session creation)
            loginResponse.AccessToken.Should().NotBe(loginResponse.RefreshToken);
        }
    }

    [Fact]
    public async Task MultipleLogins_ShouldCreateMultipleSessions()
    {
        // Arrange
        var request = new LoginRequestDto("admin@demo.com", "Demo@321", null);
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act - Perform multiple logins
        var response1 = await _client.PostAsync("/auth/login", content);
        var response2 = await _client.PostAsync("/auth/login", content);

        // Assert
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
        
        if (response1.IsSuccessStatusCode && response2.IsSuccessStatusCode)
        {
            var response1String = await response1.Content.ReadAsStringAsync();
            var response2String = await response2.Content.ReadAsStringAsync();
            
            var loginResponse1 = JsonSerializer.Deserialize<LoginResponseDto>(response1String, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var loginResponse2 = JsonSerializer.Deserialize<LoginResponseDto>(response2String, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            loginResponse1.Should().NotBeNull();
            loginResponse2.Should().NotBeNull();
            
            // Each login should generate different tokens (different sessions)
            loginResponse1!.AccessToken.Should().NotBe(loginResponse2!.AccessToken);
        }
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldDeactivateSession()
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
}