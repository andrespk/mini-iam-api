using Xunit;
using FluentAssertions;
using MiniIAM.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

public class AuthServiceTests
{
    [Fact]
    public void GenerateJwt_Returns_Token()
    {
        var settings = new Dictionary<string, string?> { ["Jwt:Key"] = "unit-test-key" };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var svc = new AuthService(config);
        var token = svc.GenerateJwt("11111111-1111-1111-1111-111111111111");
        token.Should().NotBeNullOrWhiteSpace();
    }
}
