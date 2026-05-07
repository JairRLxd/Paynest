namespace Paynest;

public static class ServiceHelper
{
	public static T GetService<T>() where T : notnull
	{
		return MauiProgram.Services.GetRequiredService<T>();
	}
}
