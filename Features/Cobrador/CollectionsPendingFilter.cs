using Paynest.Features.Cobrador.ViewModels;

namespace Paynest.Features.Cobrador;

internal static class CollectionsPendingFilter
{
    private static CollectionListFilter? _pending;

    public static void Request(CollectionListFilter filter) => _pending = filter;

    public static CollectionListFilter? Consume()
    {
        var value = _pending;
        _pending = null;
        return value;
    }
}
