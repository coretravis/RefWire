namespace ListServDB.Core.Models;

/// <summary>
/// Represents a field within a dataset.
/// </summary>
public class DatasetField
{
    /// <summary>
    /// The name of the field.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// The data type of the field.
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    /// <summary>
    /// The data type of the field.
    /// </summary>
    public bool IsId { get; set; }
    /// <summary>
    /// The data type of the field.
    /// </summary>
    public bool IsName { get; set; }
    /// <summary>
    /// The data type of the field.
    /// </summary>
    public bool IsRequired { get; set; }
    /// <summary>
    /// The data type of the field.
    /// </summary>
    public bool IsIncluded { get; set; } = true;
    /// <summary>
    /// The data type of the field.
    /// </summary>
    public List<string> SampleValues { get; set; } = new List<string>();
}