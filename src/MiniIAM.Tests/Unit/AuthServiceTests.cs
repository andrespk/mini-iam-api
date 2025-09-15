using Xunit;
using FluentAssertions;
using MiniIAM.Infrastructure.Auth;
using MiniIAM.Infrastructure.Auth.Dtos;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

public class AuthServiceTests
{
    [Fact]
    public void GenerateJwt_ReturnsToken()
    {
        var inMemorySettings = new Dictionary<string, string?> { ["Jwt:Key"] = "unit-test-key" };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var svc = new AuthService(config);
        var token = svc.GenerateJwt("11111111-1111-1111-1111-111111111111");
        token.Should().NotBeNullOrWhiteSpace();
    }
}
