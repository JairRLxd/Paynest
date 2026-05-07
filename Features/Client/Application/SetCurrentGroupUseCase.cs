using Paynest.Features.Client.Repositories;

namespace Paynest.Features.Client.Application;

public sealed class SetCurrentGroupUseCase
{
	private readonly IClientDebtRepository _repository;

	public SetCurrentGroupUseCase(IClientDebtRepository repository)
	{
		_repository = repository;
	}

	public void Execute(string groupId)
	{
		_repository.SetCurrentGroup(groupId);
	}
}
