namespace MiniIAM.Infrastructure.Data;

public class DataSort(IDictionary<string, SortDirections> definition)
{
    public IDictionary<string, SortDirections> Definition { get; } = definition;

    public void Add(string propertyName, SortDirections direction)
    {
        Definition.Add(propertyName, direction);
    }

    public void Remove(string propertyName)
    {
        if (Definition.ContainsKey(propertyName))
            Definition.Remove(Definition.FirstOrDefault(x => x.Key == propertyName));
    }

    public string GetStringDefinition() =>
        string.Join(",", Definition.Select(x => $"{x.Key} {GetStringifiedDirection(x.Value)}"));

    private string GetStringifiedDirection(SortDirections direction) =>
        direction == SortDirections.Ascending ? "ascending" : "descending";
}