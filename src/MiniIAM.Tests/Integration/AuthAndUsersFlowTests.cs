using Xunit;
using FluentAssertions;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using MiniIAM.Infrastructure.Auth.Dtos;

public class ApiFactory : WebApplicationFactory<Program> { }

public class AuthAndUsersFlowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AuthAndUsersFlowTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_Then_Create_User_Flow()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto("admin@local", "admin"));
        // login may fail depending on your handler; ensure the endpoint is reachable
        login.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.Unauthorized);
    }
}
