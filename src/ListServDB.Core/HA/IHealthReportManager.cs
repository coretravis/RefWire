using ListServDB.Core.HA.Models;
using ListServDB.Core.Models;

namespace ListServDB.Core.HA;

/// <summary>
/// Interface for managing health reports of the ListServ instance.
/// </summary>
public interface IHealthReportManager
{
    /// <summary>
    /// Generates a health report based on the provided datasets.
    /// </summary>
    /// <param name="datasets">A dictionary of datasets keyed by their unique identifiers.</param>
    /// <returns>A <see cref="HealthReport"/> containing the health details of the system.</returns>
    HealthReport GetHealthReport(Dictionary<string, Dataset> datasets);
}
