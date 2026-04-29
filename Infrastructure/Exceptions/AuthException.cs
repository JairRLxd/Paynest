using Paynest.Core.Models.Auth;

namespace Paynest.Infrastructure.Exceptions;

public class AuthException(ProblemDetails problem) : Exception(problem.Detail)
{
    public ProblemDetails Problem { get; } = problem;
}
