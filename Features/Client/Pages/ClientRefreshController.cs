#nullable enable
using Paynest.Services;

namespace Paynest;

internal sealed class ClientRefreshController : IDisposable
{
	private readonly ClientDataRefreshService _refreshService;
	private readonly ClientDataRefreshScope _watchedScope;
	private readonly Func<CancellationToken, Task> _refreshAsync;
	private readonly Func<ClientDataChangedEventArgs, Task>? _afterRefreshAsync;
	private CancellationTokenSource? _requestCts;
	private bool _isActive;
	private long _lastRefreshVersion;

	public ClientRefreshController(
		ClientDataRefreshService refreshService,
		ClientDataRefreshScope watchedScope,
		Func<CancellationToken, Task> refreshAsync,
		Func<ClientDataChangedEventArgs, Task>? afterRefreshAsync = null)
	{
		_refreshService = refreshService;
		_watchedScope = watchedScope;
		_refreshAsync = refreshAsync;
		_afterRefreshAsync = afterRefreshAsync;
		_refreshService.DataChanged += OnClientDataChanged;
	}

	public CancellationToken Token => _requestCts?.Token ?? CancellationToken.None;

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
		_refreshService.DataChanged -= OnClientDataChanged;
		Deactivate();
	}

	private void OnClientDataChanged(object? sender, ClientDataChangedEventArgs e)
	{
		if (!_isActive || _lastRefreshVersion >= e.Version || !ShouldRefresh(e.Scope))
		{
			return;
		}

		_lastRefreshVersion = e.Version;
		MainThread.BeginInvokeOnMainThread(async () => await RefreshNowAsync(e));
	}

	private bool ShouldRefresh(ClientDataRefreshScope scope)
	{
		return (scope & _watchedScope) != ClientDataRefreshScope.None;
	}

	private async Task RefreshNowAsync()
		=> await RefreshNowAsync(null);

	private async Task RefreshNowAsync(ClientDataChangedEventArgs? args)
	{
		CancelCurrentRequest();
		_requestCts = new CancellationTokenSource();
		await _refreshAsync(_requestCts.Token);
		if (args is not null && _afterRefreshAsync is not null)
		{
			await _afterRefreshAsync(args);
		}
	}

	private void CancelCurrentRequest()
	{
		_requestCts?.Cancel();
		_requestCts?.Dispose();
		_requestCts = null;
	}
}
