using Paynest.Features.Client.Repositories;
using Paynest.Models;

namespace Paynest.Features.Client.Application;

public sealed class GetPaidReceiptsUseCase
{
	private readonly IClientDebtRepository _repository;

	public GetPaidReceiptsUseCase(IClientDebtRepository repository)
	{
		_repository = repository;
	}

	public IReadOnlyList<Installment> ExecuteForCurrentGroup()
	{
		return _repository.GetInstallmentsByGroup(_repository.CurrentGroup.Id)
			.Where(i => i.Status == InstallmentStatus.Paid)
			.ToList();
	}
}
