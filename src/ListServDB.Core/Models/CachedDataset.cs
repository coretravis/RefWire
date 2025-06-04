using ListServDB.Core.Interfaces;

namespace ListServDB.Core.Models;

public class CachedDataset
{
    // Dataset instance.
    public Dataset Dataset { get; set; } = null!;

    // Index for searching dataset items.
    public IIndex<string, DatasetItem> Index { get; set; } = null!;
}