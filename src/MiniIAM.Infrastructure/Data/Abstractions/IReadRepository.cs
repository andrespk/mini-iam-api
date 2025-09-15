using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using MiniIAM.Domain.Abstractions;
using MiniIAM.Infrastructure.Data.Paging;

namespace MiniIAM.Infrastructure.Data.Abstractions;

public interface IReadRepository<TEntity, in TUid, TModel>
    where TEntity : IEntity<TUid> where TModel : class
{
    public Task<Result<TModel>> GetByIdAsync(TUid id, CancellationToken ct = default);

    public Task<ResultList<TModel>> GetPagedAsync(PageMeta pageMeta, Expression<Func<TEntity, bool>>? filter,
        DataSort? sort,
        string? searchText = null, CancellationToken? ct = default);

    public Task<ResultList<TModel>> GetAsync(Expression<Func<TEntity, bool>>? filter, DataSort? sort,
        string? searchText = null,
        CancellationToken? ct = default);
}