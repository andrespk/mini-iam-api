using Serilog;

namespace MiniIAM.Infrastructure.Cqrs.Abstractions;

public interface IHandlerContext
{
    ILogger Logger { get; }
}