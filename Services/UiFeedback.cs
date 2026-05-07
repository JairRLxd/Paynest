namespace Paynest.Services;

public static class UiFeedback
{
    public static void ShowShort(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
#if ANDROID
            Android.Widget.Toast.MakeText(Android.App.Application.Context, message, Android.Widget.ToastLength.Short)?.Show();
#else
            if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
            {
                _ = page.DisplayAlertAsync("Aviso", message, "OK");
            }
#endif
        });
    }
}
