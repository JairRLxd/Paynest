using Paynest.Features.Client.Repositories;
using Paynest.Models;

namespace Paynest.Features.Client.Application;

public sealed class GetClientDashboardUseCase
{
	private readonly IClientDebtRepository _repository;

	public GetClientDashboardUseCase(IClientDebtRepository repository)
	{
		_repository = repository;
	}

	public IReadOnlyList<DebtGroup> GetGroups() => _repository.GetGroups();

	public DebtGroup GetCurrentGroup() => _repository.CurrentGroup;

	public IReadOnlyList<Installment> GetUpcomingFromCurrentGroup(int take)
	{
		return _repository.GetInstallmentsByGroup(_repository.CurrentGroup.Id)
			.Take(take)
			.ToList();
	}
}
