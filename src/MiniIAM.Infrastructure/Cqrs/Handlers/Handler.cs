using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MinimalCqrs;
using Serilog;

namespace MiniIAM.Infrastructure.Cqrs.Handlers;

public abstract class Handler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected readonly IHandlerContext Context;
    protected readonly ILogger Logger;

    protected Handler(IHandlerContext context)
    {
        Context = context;
        Logger = context.Logger;
    }

    public abstract Task<TResponse> HandleAsync(TCommand command, CancellationToken ct = default);
    public abstract TResponse Handle(TCommand command);
}

public abstract class Handler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected readonly IHandlerContext Context;
    protected readonly ILogger Logger;

    protected Handler(IHandlerContext context)
    {
        Context = context;
        Logger = context.Logger;
    }

    public abstract Task HandleAsync(TCommand command, CancellationToken ct = default);
    public abstract void Handle(TCommand command);
}
