namespace Paynest;

internal static class PageMotion
{
	public static async Task FadeInUpAsync(
		VisualElement root,
		uint durationMs = 220,
		double offsetY = 10)
	{
		root.Opacity = 0;
		root.TranslationY = offsetY;
		await Task.WhenAll(
			root.FadeToAsync(1, durationMs),
			root.TranslateToAsync(0, 0, durationMs));
	}

	public static async Task StaggerInAsync(
		IEnumerable<View> sections,
		int take = 6,
		uint durationMs = 150,
		double offsetY = 8,
		int staggerDelayMs = 45)
	{
		var list = sections.Take(take).ToList();
		foreach (var section in list)
		{
			section.Opacity = 0;
			section.TranslationY = offsetY;
		}

		await Task.WhenAll(list.Select((section, index) => AnimateSectionAsync(section, index, durationMs, staggerDelayMs)));
	}

	private static async Task AnimateSectionAsync(View section, int index, uint durationMs, int staggerDelayMs)
	{
		if (index > 0)
		{
			await Task.Delay(index * staggerDelayMs);
		}

		await Task.WhenAll(
			section.FadeToAsync(1, durationMs),
			section.TranslateToAsync(0, 0, durationMs));
	}
}
