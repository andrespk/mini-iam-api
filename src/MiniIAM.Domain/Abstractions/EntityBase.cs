namespace MiniIAM.Domain.Abstractions;

public abstract class EntityBase<TUid>(
    TUid id,
    DataChangesHistory? changesHistory = null) : IEntity<TUid>
{
    
    public TUid Id { get; set; } = id;
    public DataChangesHistory ChangesHistory { get; set; } = changesHistory ?? new DataChangesHistory();
    public bool IsDeleted => ChangesHistory.DeletedAtUtc.HasValue;


    public abstract object ToDto();
    public virtual void SetInsertChangeHistory(Guid byUserId) => ChangesHistory.SetInsertChangesHistory(byUserId);
    public virtual void UpdateChangeHistory(Guid byUserId) => ChangesHistory.SetUpdateChangesHistory(byUserId);
    public virtual void DeleteChangeHistory(Guid byUserId) => ChangesHistory.SetDeleteChangesHistory(byUserId);
}