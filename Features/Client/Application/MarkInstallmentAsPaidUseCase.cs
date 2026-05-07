using Paynest.Features.Client.Repositories;

namespace Paynest.Features.Client.Application;

public sealed class MarkInstallmentAsPaidUseCase
{
	private readonly IClientDebtRepository _repository;

	public MarkInstallmentAsPaidUseCase(IClientDebtRepository repository)
	{
		_repository = repository;
	}

	public bool Execute(string installmentId)
	{
		return _repository.MarkInstallmentAsPaid(installmentId);
	}
}
