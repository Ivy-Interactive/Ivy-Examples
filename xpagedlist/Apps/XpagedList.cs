[App(icon: Icons.Sheet, title: "XpagedList Example", path: ["Apps"])]
public class XpagedList : ViewBase
{
    public override object? Build()
    {
        var items = UseState<List<string>>(Enumerable.Range(1, 53).Select(i => $"Item {i}").ToList(), true);
        var pageNumber = UseState<int>(2, true);
        const int pageSize = 10;

        //Create a paged list
        IPagedList<string> page = new PagedList<string>(items.Value, pageNumber.Value, pageSize);

        if (page.PageCount > 0)
        {
            if (pageNumber.Value > page.PageCount) pageNumber.Set(page.PageCount);
            if (pageNumber.Value < 1) pageNumber.Set(1);
        }

        return new Card().Title("X.PagedList — Ivy demo")
        | (Layout.Vertical()
            //Show current page info
            | Text.Label($"Page {page.PageNumber} of {page.PageCount} (Total items: {page.TotalItemCount})")

            //Show paged items
            | (Layout.Vertical()
                | page.Select(s => Text.Label(s))
              )

            //Configure Prev and Next buttons
            | (Layout.Horizontal().Align(Align.Center)
                | new Button("Prev", _ => pageNumber.Set(Math.Max(1, pageNumber.Value - 1))).Disabled(!page.HasPreviousPage)
                | new Button("Next", _ => pageNumber.Set(Math.Min(page.PageCount, pageNumber.Value + 1))).Disabled(!page.HasNextPage)
              )
          );
    }
}