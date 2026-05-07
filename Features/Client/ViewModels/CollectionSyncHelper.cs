using System.Collections.ObjectModel;

namespace Paynest.Features.Client.ViewModels;

public static class CollectionSyncHelper
{
	public static bool SyncByKey<TItem, TKey>(
		ObservableCollection<TItem> target,
		IReadOnlyList<TItem> source,
		Func<TItem, TKey> keySelector)
		where TKey : notnull
	{
		target.Clear();
		for (var i = 0; i < source.Count; i++)
		{
			target.Add(source[i]);
		}
		return true;
	}
}
