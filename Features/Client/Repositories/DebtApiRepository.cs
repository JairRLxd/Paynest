#nullable enable
using Paynest.Features.Client.Api;
using Paynest.Models;
using Microsoft.Extensions.Logging;

namespace Paynest.Features.Client.Repositories;

public sealed class DebtApiRepository : IClientDebtRepository
{
	private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(20);
	private static readonly DebtGroup EmptyGroup = new()
	{
		Id = string.Empty,
		Name = "Sin grupo seleccionado",
		FreelancerName = string.Empty,
		TotalAmount = 0m,
		PendingAmount = 0m,
		Frequency = PaymentFrequency.Monthly
	};

	private readonly IDebtApiClient _apiClient;
	private readonly ILogger<DebtApiRepository> _logger;
	private readonly SemaphoreSlim _cacheLock = new(1, 1);
	private readonly Dictionary<string, (DateTimeOffset ExpiresAt, IReadOnlyList<Installment> Data)> _installmentsCache = [];
	private (DateTimeOffset ExpiresAt, IReadOnlyList<DebtGroup> Data)? _groupsCache;
	private DebtGroup? _currentGroup;

	public event EventHandler? CurrentGroupChanged;

	public DebtApiRepository(
		IDebtApiClient apiClient,
		ILogger<DebtApiRepository> logger)
	{
		_apiClient = apiClient;
		_logger = logger;
	}

	public DebtGroup CurrentGroup => _currentGroup ?? EmptyGroup;

	public IReadOnlyList<DebtGroup> GetGroups() => _groupsCache?.Data ?? [];

	public async Task<IReadOnlyList<DebtGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
	{
		var now = DateTimeOffset.UtcNow;
		if (_groupsCache is { } entry && entry.ExpiresAt > now)
		{
			return entry.Data;
		}

		await _cacheLock.WaitAsync(cancellationToken);
		try
		{
			now = DateTimeOffset.UtcNow;
			if (_groupsCache is { } refreshEntry && refreshEntry.ExpiresAt > now)
			{
				return refreshEntry.Data;
			}

			var remote = await _apiClient.GetDebtGroupsAsync(cancellationToken);
			var mapped = remote.Select(x => x.ToModel()).ToList();
			_groupsCache = (now.Add(CacheTtl), mapped);
			EnsureCurrentGroup(mapped);
			return mapped;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Remote groups unavailable");
			if (_groupsCache is not null)
			{
				return _groupsCache.Value.Data;
			}

			throw;
		}
		finally
		{
			_cacheLock.Release();
		}
	}

	public void SetCurrentGroup(string groupId)
	{
		var found = _groupsCache?.Data.FirstOrDefault(g => g.Id == groupId);
		if (found is null || found.Id == CurrentGroup.Id)
		{
			return;
		}

		_currentGroup = found;
		CurrentGroupChanged?.Invoke(this, EventArgs.Empty);
	}

	public IReadOnlyList<Installment> GetInstallmentsByGroup(string groupId)
	{
		return _installmentsCache.TryGetValue(groupId, out var entry) ? entry.Data : [];
	}

	public async Task<IReadOnlyList<Installment>> GetInstallmentsByGroupAsync(string groupId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(groupId))
		{
			return [];
		}

		var now = DateTimeOffset.UtcNow;
		if (_installmentsCache.TryGetValue(groupId, out var entry) && entry.ExpiresAt > now)
		{
			return entry.Data;
		}

		await _cacheLock.WaitAsync(cancellationToken);
		try
		{
			now = DateTimeOffset.UtcNow;
			if (_installmentsCache.TryGetValue(groupId, out entry) && entry.ExpiresAt > now)
			{
				return entry.Data;
			}

			var remote = await _apiClient.GetInstallmentsByGroupAsync(groupId, cancellationToken);
			var mapped = remote.Select(x => x.ToModel()).ToList();
			_installmentsCache[groupId] = (now.Add(CacheTtl), mapped);
			return mapped;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Remote installments unavailable for group {GroupId}", groupId);
			if (_installmentsCache.TryGetValue(groupId, out entry))
			{
				return entry.Data;
			}

			throw;
		}
		finally
		{
			_cacheLock.Release();
		}
	}

	public bool MarkInstallmentAsPaid(string installmentId)
	{
		return false;
	}

	public async Task<PayInstallmentResult> MarkInstallmentAsPaidAsync(string installmentId, CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await _apiClient.MarkInstallmentAsPaidAsync(installmentId, cancellationToken);
			if (result.Success)
			{
				InvalidateCache();
				return result.ToModel();
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Remote pay flow failed for installment {InstallmentId}", installmentId);
			throw;
		}

		return new PayInstallmentResult { Success = false };
	}

	public async Task<IReadOnlyList<Receipt>> GetReceiptsAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var remote = await _apiClient.GetReceiptsAsync(cancellationToken);
			return remote.Select(x => x.ToModel()).ToList();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Remote receipts unavailable");
			throw;
		}
	}

	public async Task<Receipt?> GetReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
	{
		try
		{
			var remote = await _apiClient.GetReceiptAsync(receiptId, cancellationToken);
			return remote?.ToModel();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Remote receipt {ReceiptId} unavailable", receiptId);
			throw;
		}
	}

	public async Task<string?> GetReceiptDownloadUrlAsync(string receiptId, CancellationToken cancellationToken = default)
	{
		try
		{
			return await _apiClient.GetReceiptDownloadUrlAsync(receiptId, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Receipt download URL unavailable for {ReceiptId}", receiptId);
			return null;
		}
	}

	public async Task<NotificationPreferences> GetNotificationPreferencesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var remote = await _apiClient.GetNotificationPreferencesAsync(cancellationToken);
			return remote.ToModel();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Remote notification preferences unavailable");
			return new NotificationPreferences();
		}
	}

	public async Task<NotificationPreferences> UpdateNotificationPreferencesAsync(
		NotificationPreferences preferences,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var remote = await _apiClient.UpdateNotificationPreferencesAsync(preferences.ToDto(), cancellationToken);
			return remote.ToModel();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Remote notification preferences save unavailable");
			throw;
		}
	}

	private void InvalidateCache()
	{
		_groupsCache = null;
		_installmentsCache.Clear();
	}

	private void EnsureCurrentGroup(IReadOnlyList<DebtGroup> groups)
	{
		if (groups.Count == 0)
		{
			_currentGroup = null;
			return;
		}

		if (_currentGroup is not null && groups.Any(g => g.Id == _currentGroup.Id))
		{
			return;
		}

		_currentGroup = groups[0];
	}
}
