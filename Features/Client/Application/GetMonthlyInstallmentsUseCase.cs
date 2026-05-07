using Paynest.Features.Client.Repositories;
using Paynest.Models;

namespace Paynest.Features.Client.Application;

public sealed class GetMonthlyInstallmentsUseCase
{
	private readonly IClientDebtRepository _repository;

	public GetMonthlyInstallmentsUseCase(IClientDebtRepository repository)
	{
		_repository = repository;
	}

	public IReadOnlyList<(DebtGroup Group, Installment Installment)> Execute(int month, int year)
	{
		return _repository.GetGroups()
			.SelectMany(g => _repository.GetInstallmentsByGroup(g.Id).Select(i => (Group: g, Installment: i)))
			.Where(x => x.Installment.DueDate.Month == month && x.Installment.DueDate.Year == year)
			.OrderBy(x => x.Installment.DueDate)
			.ToList();
	}
}
