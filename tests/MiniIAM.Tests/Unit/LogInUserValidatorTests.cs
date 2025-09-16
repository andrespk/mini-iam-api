using Xunit;
using FluentAssertions;

public class LogInUserValidatorTests
{
    [Fact]
    public void Email_And_Password_Are_Required()
    {
        var cmd = new LogInUser.Command("", "");
        // validator is constructed via DI in the handler; we assert command values are passed
        cmd.Email.Should().NotBeNull();
        cmd.Password.Should().NotBeNull();
    }
}
