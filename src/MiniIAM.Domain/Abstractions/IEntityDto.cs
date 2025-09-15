namespace MiniIAM.Domain.Abstractions;

public interface IEntityDto<TEntity>
{
    public TEntity ToEntity();
}