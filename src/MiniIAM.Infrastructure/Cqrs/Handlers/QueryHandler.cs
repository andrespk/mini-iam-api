using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MinimalCqrs;
using Serilog;

namespace MiniIAM.Infrastructure.Cqrs.Handlers;

public abstract class QueryHandler<TQuery, TResponse> : Handler<TQuery, TResponse>
    where TQuery : IHandlerMessage<IHandlerResponse<TResponse>>
{
    protected readonly ILogger Logger;

    protected QueryHandler(IHandlerContext context) => Logger = context.Logger;
}