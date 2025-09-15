using Serilog;

namespace MiniIAM.Infrastructure.Handlers.Abstractions;

public interface IHandlerContext
{
    ILogger Logger { get; }
}