using MinimalCqrs;

namespace MiniIAM.Infrastructure.Cqrs.Abstractions;

public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>;
}