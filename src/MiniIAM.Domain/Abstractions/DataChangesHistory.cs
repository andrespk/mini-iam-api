namespace MiniIAM.Domain.Abstractions;

public sealed class DataChangesHistory(
    DateTime? createdAtUtc,
    Guid? createdByUserId = null,
    DateTime? updatedAtUtc = null,
    Guid? updatedByUserId = null,
    DateTime? deletedAtUtc = null,
    Guid? deletedByUserId = null)
{
    public DateTime? CreatedAtUtc { get; set; } = createdAtUtc;
    public Guid? CreatedByUserId { get; set; } = createdByUserId;
    public DateTime? UpdatedAtUtc { get; set; } = createdAtUtc;
    public Guid? UpdatedByUserId { get;  set; } = createdByUserId;
    public DateTime? DeletedAtUtc { get; set; } = createdAtUtc;
    public Guid? DeletedByUserId { get; set; } = createdByUserId;

    public DataChangesHistory(Guid? createdByUserId = null) : this(DateTime.UtcNow, createdByUserId)
    {
        
    }

    public void SetInsertChangesHistory(Guid byUserId)
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedByUserId = byUserId;
    }

    public void SetUpdateChangesHistory(Guid byUserId)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedByUserId = byUserId;
    }

    public void SetDeleteChangesHistory(Guid byUserId)
    {
        DeletedAtUtc = DateTime.UtcNow;
        DeletedByUserId = byUserId;
    }
}