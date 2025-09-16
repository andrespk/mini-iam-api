using Microsoft.Extensions.DependencyInjection;
using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MinimalCqrs;

namespace MiniIAM.Infrastructure.Cqrs.Dispatchers;

public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    public QueryDispatcher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return await handler.Handle(query, CancellationToken.None);
    }
}