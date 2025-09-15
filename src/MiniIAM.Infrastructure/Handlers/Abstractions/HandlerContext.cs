using Serilog;

namespace MiniIAM.Infrastructure.Handlers.Abstractions;

public sealed class HandlerContext : IHandlerContext
{
    public ILogger Logger { get; private set; }

    public HandlerContext(ILogger logger) => Logger = logger;
}