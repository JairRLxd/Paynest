namespace Paynest.Services;

[Flags]
public enum CollectorRefreshScope
{
    None        = 0,
    Clients     = 1,
    Collections = 2,
    Dashboard   = 4,
    ClientDetail = 8,
    Agenda      = 16,
    All = Clients | Collections | Dashboard | ClientDetail | Agenda
}

public sealed class CollectorDataChangedEventArgs(
    CollectorRefreshScope scope,
    long version,
    string clientId = "") : EventArgs
{
    public CollectorRefreshScope Scope    { get; } = scope;
    public long                  Version  { get; } = version;
    public string                ClientId { get; } = clientId;
}

public sealed class CollectorDataRefreshService
{
    private long _version;

    public event EventHandler<CollectorDataChangedEventArgs>? DataChanged;

    public long Version => Interlocked.Read(ref _version);

    public void NotifyChanged(CollectorRefreshScope scope, string clientId = "")
    {
        if (scope == CollectorRefreshScope.None) return;

        var version = Interlocked.Increment(ref _version);
        DataChanged?.Invoke(this, new CollectorDataChangedEventArgs(scope, version, clientId));
    }
}
