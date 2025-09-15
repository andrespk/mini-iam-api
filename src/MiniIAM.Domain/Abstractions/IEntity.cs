namespace MiniIAM.Domain.Abstractions;

public interface IEntity<TUid>
{
    public TUid Id { get; }
    public DataChangesHistory ChangesHistory { get; }
    public bool IsDeleted { get; }

    public object ToDto();
}