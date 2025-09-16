using MinimalCqrs;

namespace MiniIAM.Infrastructure.Cqrs.Abstractions;

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
    
}