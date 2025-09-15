using MiniIAM.Domain.Abstractions;

namespace MiniIAM.Infrastructure.Data.Abstractions;

public interface IWriteRepository<TEntity, in TUid, TModel>
    where TEntity : IEntity<TUid> where TModel : class
{
    public Task<Result> InsertAsync(TEntity entity, Guid byUserId, CancellationToken ct = default);
    public Task<Result> UpdateAsync(TEntity entity, Guid byUserId, CancellationToken ct = default);
    public Task<Result> DeleteAsync(TUid id, Guid byUserId, CancellationToken ct = default);
}