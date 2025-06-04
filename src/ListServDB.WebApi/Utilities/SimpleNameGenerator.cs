namespace ListServDB.WebApi.Utilities;

/// <summary>
/// Generates simple names by combining random adjectives and surnames.
/// </summary>
public static class SimpleNameGenerator
{
    /// <summary>
    /// Array of adjectives used for generating names.
    /// </summary>
    private static readonly string[] Adjectives = new string[]
    {
        "quirky", "happy", "amazing", "sleepy", "nervous",
        "brave", "elegant", "fancy", "gentle", "jolly", "kind", "lively", "nice", "proud", "silly", "vast", "witty",
        "zen", "bold", "calm", "daring", "fierce", "graceful", "honest", "jovial", "keen", "lucky", "merry", "noble", "elsie"
    };

    /// <summary>
    /// Array of surnames used for generating names.
    /// </summary>
    private static readonly string[] Surnames = new string[]
    {
        "turing", "lovelace", "einstein", "hopper", "curie",
        "gates", "bell", "darwin", "tesla", "morse", "newton", "pasteur", "edison", "faraday", "galileo", "kepler",
        "mendel", "nobel", "pascal", "raman", "sagan", "thales", "volta", "watt", "yukawa", "zuse", "coretravis"
    };

    // Use a static Random instance to avoid repetition issues with multiple calls.
    private static readonly Random _random = new Random();

    /// <summary>
    /// Generates a simple generic name by combining a random adjective and surname.
    /// Example output: "quirky_turing"
    /// </summary>
    /// <returns>A generated name string.</returns>
    public static string GenerateName()
    {
        string adjective = Adjectives[_random.Next(Adjectives.Length)];
        string surname = Surnames[_random.Next(Surnames.Length)];
        return $"{adjective}_{surname}";
    }
}