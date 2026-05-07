using Paynest.Features.Client.Repositories;
using Paynest.Models;

namespace Paynest.Features.Client.Application;

public sealed class GetGroupInstallmentsUseCase
{
	private readonly IClientDebtRepository _repository;

	public GetGroupInstallmentsUseCase(IClientDebtRepository repository)
	{
		_repository = repository;
	}

	public IReadOnlyList<Installment> Execute(string groupId)
	{
		return _repository.GetInstallmentsByGroup(groupId);
	}
}
