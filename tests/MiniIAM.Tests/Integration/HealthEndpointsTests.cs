using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using MiniIAM.Tests.Integration;

namespace MiniIAM.Tests.Integration;

public class HealthEndpointsTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;
    private readonly CustomApiFactory _factory;

    public HealthEndpointsTests(CustomApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_Basic_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrWhiteSpace();
        
        var healthResponse = JsonSerializer.Deserialize<HealthResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        healthResponse.Should().NotBeNull();
        healthResponse!.Status.Should().Be("Healthy");
        healthResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task HealthCheck_Detailed_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrWhiteSpace();
        
        var detailedHealthResponse = JsonSerializer.Deserialize<DetailedHealthResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        detailedHealthResponse.Should().NotBeNull();
        detailedHealthResponse!.Status.Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
        detailedHealthResponse.TotalDuration.Should().BeGreaterThanOrEqualTo(0);
        detailedHealthResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        detailedHealthResponse.Checks.Should().NotBeNull();
    }

    [Fact]
    public async Task HealthCheck_Readiness_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrWhiteSpace();
        
        var readinessResponse = JsonSerializer.Deserialize<HealthResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        readinessResponse.Should().NotBeNull();
        readinessResponse!.Status.Should().Be("Ready");
        readinessResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task HealthCheck_Liveness_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrWhiteSpace();
        
        var livenessResponse = JsonSerializer.Deserialize<HealthResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        livenessResponse.Should().NotBeNull();
        livenessResponse!.Status.Should().Be("Alive");
        livenessResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task HealthCheck_AllEndpoints_ShouldReturnOk()
    {
        // Arrange
        var endpoints = new[]
        {
            "/health/",
            "/health/detailed",
            "/health/ready",
            "/health/live"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Endpoint {endpoint} should return OK");
            
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().NotBeNullOrWhiteSpace($"Endpoint {endpoint} should return content");
        }
    }

    [Fact]
    public async Task HealthCheck_Detailed_ShouldContainChecks()
    {
        // Act
        var response = await _client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        var detailedHealthResponse = JsonSerializer.Deserialize<DetailedHealthResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        detailedHealthResponse.Should().NotBeNull();
        detailedHealthResponse!.Checks.Should().NotBeNull();
        detailedHealthResponse.Checks.Should().NotBeEmpty("Health checks should be configured");
        
        // Verify that at least one check exists (likely the "self" check)
        var hasSelfCheck = detailedHealthResponse.Checks.Any(check => 
            check.Name.Equals("self", StringComparison.OrdinalIgnoreCase));
        hasSelfCheck.Should().BeTrue("Should have a 'self' health check");
    }

    [Fact]
    public async Task HealthCheck_Detailed_ShouldHaveValidCheckStatuses()
    {
        // Act
        var response = await _client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        var detailedHealthResponse = JsonSerializer.Deserialize<DetailedHealthResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        detailedHealthResponse.Should().NotBeNull();
        detailedHealthResponse!.Checks.Should().NotBeNull();
        
        foreach (var check in detailedHealthResponse.Checks)
        {
            check.Status.Should().BeOneOf("Healthy", "Degraded", "Unhealthy", 
                $"Check '{check.Name}' should have a valid status");
            check.Duration.Should().BeGreaterThanOrEqualTo(0, 
                $"Check '{check.Name}' should have a non-negative duration");
        }
    }

    [Fact]
    public async Task HealthCheck_ResponseHeaders_ShouldBeCorrect()
    {
        // Act
        var response = await _client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthCheck_ConcurrentRequests_ShouldAllReturnOk()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var endpoint = "/health/";

        // Act - Make 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync(endpoint));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task HealthCheck_NonExistentEndpoint_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/health/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthCheck_WithInvalidMethod_ShouldReturnMethodNotAllowed()
    {
        // Act
        var response = await _client.PostAsync("/health/", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}

// Helper classes for deserializing health check responses
public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class DetailedHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public double TotalDuration { get; set; }
    public DateTime Timestamp { get; set; }
    public List<HealthCheckEntry> Checks { get; set; } = new();
}

public class HealthCheckEntry
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Duration { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public string? Exception { get; set; }
}
