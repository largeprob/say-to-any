using System.Collections.ObjectModel;

namespace pc.Models;

public sealed class DictationHistoryGroup
{
    public DictationHistoryGroup(string title, IEnumerable<DictationHistoryItem> items)
    {
        Title = title;
        Items = new ObservableCollection<DictationHistoryItem>(items);
    }

    public string Title { get; }

    public ObservableCollection<DictationHistoryItem> Items { get; }
}
