using Paynest.Services;

namespace Paynest.Features.Cobrador;

internal sealed class CollectorRefreshController : IDisposable
{
    private readonly CollectorDataRefreshService _refreshService;
    private readonly CollectorRefreshScope _watchedScope;
    private readonly Func<CancellationToken, Task> _refreshAsync;
    private readonly Func<CollectorDataChangedEventArgs, bool>? _filter;
    private CancellationTokenSource? _requestCts;
    private bool _isActive;
    private long _lastRefreshVersion;

    public CollectorRefreshController(
        CollectorDataRefreshService refreshService,
        CollectorRefreshScope watchedScope,
        Func<CancellationToken, Task> refreshAsync,
        Func<CollectorDataChangedEventArgs, bool>? filter = null)
    {
        _refreshService = refreshService;
        _watchedScope   = watchedScope;
        _refreshAsync   = refreshAsync;
        _filter         = filter;
        _refreshService.DataChanged += OnDataChanged;
    }

    public async Task ActivateAsync()
    {
        _isActive = true;
        await RefreshNowAsync();
        _lastRefreshVersion = _refreshService.Version;
    }

    public void Deactivate()
    {
        _isActive = false;
        CancelCurrentRequest();
    }

    public void Dispose()
    {
        _refreshService.DataChanged -= OnDataChanged;
        Deactivate();
    }

    private void OnDataChanged(object? sender, CollectorDataChangedEventArgs e)
    {
        if (!_isActive || _lastRefreshVersion >= e.Version)
            return;
        if ((e.Scope & _watchedScope) == CollectorRefreshScope.None)
            return;
        if (_filter is not null && !_filter(e))
            return;

        _lastRefreshVersion = e.Version;
        MainThread.BeginInvokeOnMainThread(async () => await RefreshNowAsync());
    }

    private async Task RefreshNowAsync()
    {
        CancelCurrentRequest();
        _requestCts = new CancellationTokenSource();
        await _refreshAsync(_requestCts.Token);
    }

    private void CancelCurrentRequest()
    {
        _requestCts?.Cancel();
        _requestCts?.Dispose();
        _requestCts = null;
    }
}
