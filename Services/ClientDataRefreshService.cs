#nullable enable
namespace Paynest.Services;

[Flags]
public enum ClientDataRefreshScope
{
	None = 0,
	Debts = 1,
	Installments = 2,
	Receipts = 4,
	Wallet = 8,
	All = Debts | Installments | Receipts | Wallet
}

public sealed class ClientDataChangedEventArgs : EventArgs
{
	public ClientDataChangedEventArgs(ClientDataRefreshScope scope, long version, string highlightedMovementId = "")
	{
		Scope = scope;
		Version = version;
		HighlightedMovementId = highlightedMovementId;
	}

	public ClientDataRefreshScope Scope { get; }
	public long Version { get; }
	public string HighlightedMovementId { get; }
}

public sealed class ClientDataRefreshService
{
	private long _version;

	public event EventHandler<ClientDataChangedEventArgs>? DataChanged;

	public long Version => Interlocked.Read(ref _version);

	public void NotifyChanged(ClientDataRefreshScope scope, string highlightedMovementId = "")
	{
		if (scope == ClientDataRefreshScope.None)
		{
			return;
		}

		var version = Interlocked.Increment(ref _version);
		DataChanged?.Invoke(this, new ClientDataChangedEventArgs(scope, version, highlightedMovementId));
	}
}
