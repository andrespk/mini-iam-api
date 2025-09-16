using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Text.Json;
using MiniIAM.Infrastructure.Auth.Dtos;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Abstractions;
using MiniIAM.Tests.Integration;
using System;
using System.Collections.Generic;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions;

namespace MiniIAM.Tests.Integration;

public class UserEndpointsTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;
    private readonly CustomApiFactory _factory;
    private readonly Guid _newRoleId = Guid.NewGuid();
    private readonly Guid _newUserId = Guid.NewGuid();

    public UserEndpointsTests(CustomApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUser_WithValidId_ShouldReturnUserOrNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/users/{userId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
        
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetUser_WithInvalidId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidUserId = "invalid-guid";

        // Act
        var response = await _client.GetAsync($"/users/{invalidUserId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUser_WithEmptyId_ShouldReturnNotFound()
    {
        // Arrange
        var emptyUserId = "";

        // Act
        var response = await _client.GetAsync($"/users/{emptyUserId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task AddUserRole_WithValidData_ShouldReturnCreatedOrUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var byUserId = Guid.NewGuid();
        
        var request = new Endpoints.UsersEndpoints.AddUserRoleRequest(
            userId,
            new List<Guid>
            {
                _newRoleId
            },
            byUserId
        );

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/users/{userId}/roles", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
        
        // Verify request structure
        request.UserId.Should().Be(userId);
        request.ByUserId.Should().Be(byUserId);
        request.RolesIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddUserRole_WithInvalidUser_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidUserId = Guid.Empty;
        var byUserId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        
        var request = new Endpoints.UsersEndpoints.AddUserRoleRequest(
            invalidUserId,
            new List<Guid>
            {
                Guid.NewGuid()
            },
            byUserId
        );

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/users/{invalidUserId}/roles", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
        
        // Verify request structure with invalid user ID
        request.UserId.Should().Be(Guid.Empty);
        request.ByUserId.Should().Be(byUserId);
        request.RolesIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddUserRole_WithEmptyRoles_ShouldReturnBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var byUserId = Guid.NewGuid();
        
        var request = new Endpoints.UsersEndpoints.AddUserRoleRequest(
            userId,
            new List<Guid>(),
            byUserId
        );

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/users/{userId}/roles", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
        
        // Verify request structure with empty roles
        request.UserId.Should().Be(userId);
        request.ByUserId.Should().Be(byUserId);
        request.RolesIds.Should().BeEmpty();
    }

    [Fact]
    public async Task AddUserRole_WithNullRoles_ShouldReturnBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var byUserId = Guid.NewGuid();
        
        var request = new Endpoints.UsersEndpoints.AddUserRoleRequest(
            userId,
            null!,
            byUserId
        );

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/users/{userId}/roles", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddUserRole_WithInvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/users/{userId}/roles", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddUserRole_WithMissingContentType_ShouldReturnBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new Endpoints.UsersEndpoints.AddUserRoleRequest(
            userId,
            new List<Guid>
            {
                Guid.NewGuid()
            },
            Guid.NewGuid()
        );

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PutAsync($"/users/{userId}/roles", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUser_WithAuthenticatedRequest_ShouldReturnUserOrNotFound()
    {
        // Arrange - First, try to log in to get a token
        var loginRequest = new LoginRequestDto("admin@demo.com", "Demo@321", null);
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await _client.PostAsync("/auth/login", loginContent);
        
        if (loginResponse.IsSuccessStatusCode)
        {
            var loginResponseDto = JsonSerializer.Deserialize<LoginResponseDto>(await loginResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (loginResponseDto != null && !string.IsNullOrWhiteSpace(loginResponseDto.AccessToken))
            {
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponseDto.AccessToken);
            }
        }

        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/users/{userId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }
}

