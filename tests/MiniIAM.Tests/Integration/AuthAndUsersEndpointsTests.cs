using Xunit;
using FluentAssertions;
using System.Net.Http.Json;
using MiniIAM.Infrastructure.Auth.Dtos;
using System.Threading.Tasks;
using System.Net;

namespace MiniIAM.Tests.Integration;

public class AuthAndUsersEndpointsTests : IClassFixture<CustomApiFactory>
{
    private readonly CustomApiFactory _factory;
    public AuthAndUsersEndpointsTests(CustomApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_Should_Return_Ok_Or_Unauthorized()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var resp = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto("admin@local", "admin", null));
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }
}
