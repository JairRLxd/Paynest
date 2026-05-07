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
		if (target.Count == source.Count)
		{
			var equals = true;
			for (var i = 0; i < target.Count; i++)
			{
				if (!EqualityComparer<TKey>.Default.Equals(keySelector(target[i]), keySelector(source[i])))
				{
					equals = false;
					break;
				}
			}

			if (equals)
			{
				return false;
			}
		}

		target.Clear();
		for (var i = 0; i < source.Count; i++)
		{
			target.Add(source[i]);
		}
		return true;
	}
}
