using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MinimalCqrs;
using Serilog;

namespace MiniIAM.Infrastructure.Cqrs.Handlers;

public abstract class CommandHandler<TCommand, TResponse> : Handler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected CommandHandler(IHandlerContext context) : base(context) { }
}

public abstract class CommandHandler<TCommand> : Handler<TCommand> where TCommand : ICommand
{
    protected CommandHandler(IHandlerContext context) : base(context) { }
}