using System.Security.Cryptography;
using System.Text;

namespace ListServDB.Security;

public class ApiKey : IEquatable<ApiKey>
{
    // Primary identifier
    public Guid Id { get; private set; }

    // Metadata
    public string Name { get; private set; }
    public string Description { get; private set; }

    // Security properties
    public string ApiKeyHash { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime DateCreated { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public IReadOnlyCollection<string> Scopes { get; private set; }

    // For EF Core or similar ORMs
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private ApiKey() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public void Update(string name, string description, IEnumerable<string> scopes)
    {
        Name = name;
        Description = description;
        Scopes = scopes != null ? new List<string>(scopes).AsReadOnly() : new List<string>().AsReadOnly();
    }

    public ApiKey(Guid id, string name, string description, string apiKeyHash,
                 DateTime dateCreated, DateTime? expiresAt = null,
                 IEnumerable<string>? scopes = null, bool isRevoked = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(apiKeyHash))
        {
            throw new ArgumentException("API key hash cannot be empty", nameof(apiKeyHash));
        }

        Id = id;
        Name = name;
        Description = description ?? string.Empty;
        ApiKeyHash = apiKeyHash;
        DateCreated = dateCreated;
        ExpiresAt = expiresAt;
        Scopes = scopes != null ? new List<string>(scopes).AsReadOnly() : new List<string>().AsReadOnly();
        IsRevoked = isRevoked;
    }

    public static string HashApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
        }

        // Use SHA256 to hash the API key
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes);
    }

    public static bool VerifyApiKey(string apiKeyHash, string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiKeyHash))
        {
            return false;
        }
        // Hash the provided API key and compare it with the stored hash
        return HashApiKey(apiKey) == apiKeyHash;
    }

    public bool VerifyApiKey(string apiKey)
    {
        return !IsRevoked &&
               !IsExpired() &&
               VerifyApiKey(ApiKeyHash, apiKey);
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    }

    public bool HasScope(string scope)
    {
        return Scopes.Contains(scope);
    }

    public ApiKey Revoke()
    {
        IsRevoked = true;
        return this;
    }

    public static (string ApiKey, string ApiKeyHash) GenerateApiKey()
    {
        // Generate a cryptographically secure random API key
        var keyBytes = RandomNumberGenerator.GetBytes(32);
        var apiKey = Convert.ToBase64String(keyBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        var apiKeyHash = HashApiKey(apiKey);
        return (apiKey, apiKeyHash);
    }

    // IEquatable implementation
    public bool Equals(ApiKey? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is ApiKey apiKey && Equals(apiKey);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(ApiKey left, ApiKey right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(ApiKey left, ApiKey right)
    {
        return !(left == right);
    }
}