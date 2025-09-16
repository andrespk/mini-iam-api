using Xunit;
using FluentAssertions;
using MiniIAM.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniIAM.Infrastructure.Caching.Abstractions;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;
using Moq;
using System.Collections.Generic;

namespace MiniIAM.Tests.Unit;

public class AuthServiceTests
{
    [Fact]
    public void GenerateJwt_Returns_Token()
    {
        var settings = new Dictionary<string, string?> { ["Jwt:Key"] = "unit-test-key" };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var logger = new Mock<ILogger<AuthService>>();
        var cacheService = new Mock<ICachingService>();
        var userReadRepository = new Mock<IUserReadRepository>();
        
        var svc = new AuthService(config, logger.Object, cacheService.Object, userReadRepository.Object);
        var token = svc.GenerateJwt("11111111-1111-1111-1111-111111111111");
        token.Should().NotBeNull();
    }
}
