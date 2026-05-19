#nullable enable
using System.Net.Http.Headers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Services;

namespace Paynest.Features.Onboarding.IdentityVerification;

public partial class DocumentPreviewViewModel(
    HttpClient httpClient,
    AuthStateService authState) : ObservableObject
{
    public Func<Task>? OnReplace { get; set; }
    public string Title { get; set; } = "Documento";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    private ImageSource? _documentImage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasImage     => DocumentImage is not null;
    public bool IsNotLoading => !IsLoading;
    public bool HasError     => ErrorMessage is not null;

    public async Task LoadAsync(string url, CancellationToken ct = default)
    {
        IsLoading    = true;
        ErrorMessage = null;
        DocumentImage = null;

        try
        {
            var token = authState.AccessToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorMessage = "Sesión expirada. Vuelve a iniciar sesión.";
                return;
            }

            // /view acepta el token como query param en lugar de header Authorization
            var separator = url.Contains('?') ? "&" : "?";
            var urlWithToken = $"{url}{separator}token={Uri.EscapeDataString(token)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, urlWithToken);
            using var resp = await httpClient.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            DocumentImage = ImageSource.FromStream(() => new MemoryStream(bytes, writable: false));
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            ErrorMessage = "No se pudo cargar la imagen. Verifica tu conexión.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task CloseAsync()
    {
        if (App.CurrentNavigation is { } nav)
            await nav.PopModalAsync();
    }

    [RelayCommand]
    async Task ReplaceAsync()
    {
        if (App.CurrentNavigation is { } nav)
            await nav.PopModalAsync();
        if (OnReplace is not null)
            await OnReplace();
    }
}
