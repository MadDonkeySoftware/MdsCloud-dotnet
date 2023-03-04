#pragma warning disable CS8618
namespace MdsCloud.Identity.Domain;

public class LandscapeUrl
{
    public LandscapeUrl()
        : this("", "") { }

    public LandscapeUrl(string scope, string key)
        : this(scope, key, "") { }

    public LandscapeUrl(string scope, string key, string value)
    {
        this.Scope = scope;
        this.Key = key;
        this.Value = value;
    }

    public virtual string Scope { get; protected set; }

    public virtual string Key { get; protected set; }

    public virtual string Value { get; set; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        var other = obj as LandscapeUrl;
        if (other == null)
            return false;

        return other.Scope == this.Scope && other.Value == this.Value;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (this.Scope.GetHashCode() * 17) + (this.Key.GetHashCode() * 17);
        }
    }
}
