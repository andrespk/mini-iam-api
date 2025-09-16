using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MinimalCqrs;
using Serilog;

namespace MiniIAM.Infrastructure.Cqrs.Handlers;

public abstract class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    protected readonly IHandlerContext Context;
    protected readonly ILogger Logger;

    protected QueryHandler(IHandlerContext context)
    {
        Context = context;
        Logger = context.Logger;
    }

    public abstract Task<TResponse> Handle(TQuery query, CancellationToken ct = default);
}