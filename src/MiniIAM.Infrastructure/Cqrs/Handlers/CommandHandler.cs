using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MinimalCqrs;
using Serilog;

namespace MiniIAM.Infrastructure.Cqrs.Handlers;

public abstract class CommandHandler<TCommand, TResponse> : Handler<TCommand, TResponse>
    where TCommand : IHandlerMessage<IHandlerResponse<TResponse>>
{
    protected readonly ILogger Logger;

    protected CommandHandler(IHandlerContext context) => Logger = context.Logger;
}

public abstract class CommandHandler<TCommand> : Handler<TCommand> where TCommand : IHandlerMessage<IHandlerResponse>
{
    protected readonly ILogger Logger;

    protected CommandHandler(IHandlerContext context) =>Logger = context.Logger;
}