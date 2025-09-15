namespace MiniIAM.Infrastructure.Data.Paging;

public class PageMeta(int page, int pageSize)
{
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public int TotalItems { get; private set; }
    public int PagesLeft { get; private set; }
    public bool HasNext { get; private set; }
    public bool HasPrevious { get; private set; }

    public void Update(int totalItems)
    {
        if (totalItems > 0)
        {
            TotalItems = totalItems;
            PagesLeft = decimal.ToInt32(Math.Round((decimal)(totalItems - Page * PageSize) / PageSize));
            HasNext = PagesLeft > 0;
            HasPrevious = Page > 1;
        }
    }
}