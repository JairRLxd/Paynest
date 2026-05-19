using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Core.Interfaces;
using Paynest.Core.Validation;
using Paynest.Core.Models.Cobrador.Clients;
using Paynest.Features.Cobrador.Models;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Features.Cobrador.ViewModels;

public partial class EditClientViewModel : ObservableObject
{
    private readonly ICollectorClientService     _clientService;
    private readonly CollectorDataRefreshService _refreshService;
    private string _clientId = string.Empty;

    [ObservableProperty] private string _name        = string.Empty;
    [ObservableProperty] private string _initials    = string.Empty;
    [ObservableProperty] private Color  _avatarColor = Color.FromArgb("#2F67E9");
    [ObservableProperty] private string _phone       = string.Empty;
    [ObservableProperty] private string _curp        = string.Empty;
    [ObservableProperty] private string _rfc         = string.Empty;
    [ObservableProperty] private string _address     = string.Empty;
    [ObservableProperty] private string _postalCode  = string.Empty;
    [ObservableProperty] private string _colonia     = string.Empty;
    [ObservableProperty] private string _municipio   = string.Empty;
    [ObservableProperty] private string _estado      = string.Empty;
    [ObservableProperty] private string _notes       = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaving;

    public string OfficialIdFrontLabel  = "INE frente";
    public string OfficialIdBackLabel   = "INE reverso";
    public string ProofOfAddressLabel   = "Comprobante domicilio";
    public string ClientPhotoLabel      = "Foto del cliente";

    public EditClientViewModel(ICollectorClientService clientService, CollectorDataRefreshService refreshService)
    {
        _clientService  = clientService;
        _refreshService = refreshService;
    }

    public void Load(ClientProfileSnapshot snapshot)
    {
        _clientId   = snapshot.ClientId;
        Name        = snapshot.Name;
        Initials    = snapshot.Initials;
        AvatarColor = snapshot.AvatarColor;
        Phone       = snapshot.Phone ?? string.Empty;
        Curp        = snapshot.Curp ?? string.Empty;
        Rfc         = snapshot.Rfc ?? string.Empty;
        Address     = snapshot.Address ?? string.Empty;
        PostalCode  = snapshot.PostalCode ?? string.Empty;
        Colonia     = snapshot.Colonia ?? string.Empty;
        Municipio   = snapshot.Municipio ?? string.Empty;
        Estado      = snapshot.Estado ?? string.Empty;
        Notes       = snapshot.Notes ?? string.Empty;
    }

    [RelayCommand]
    async Task BackAsync()
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.Navigation.PopAsync();
            return;
        }

        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
            await page.Navigation.PopAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    async Task SaveAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId)) return;

        IsSaving = true;
        try
        {
            var request = new UpdateClientRequest(
                Name:       InputSanitizer.Text(Name),
                Phone:      InputSanitizer.NullableIdentifier(Phone),
                Curp:       InputSanitizer.NullableIdentifier(Curp),
                Rfc:        InputSanitizer.NullableIdentifier(Rfc, "&"),
                Address:    InputSanitizer.NullableText(Address),
                PostalCode: InputSanitizer.NullableText(PostalCode),
                Colonia:    InputSanitizer.NullableText(Colonia),
                Municipio:  InputSanitizer.NullableText(Municipio),
                Estado:     InputSanitizer.NullableText(Estado),
                Notes:      InputSanitizer.NullableText(Notes));

            await _clientService.UpdateClientAsync(_clientId, request, ct);
            _refreshService.NotifyChanged(CollectorRefreshScope.Clients | CollectorRefreshScope.ClientDetail, _clientId);
            await BackAsync();
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudo guardar", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error de conexión", "No pudimos guardar los cambios. Intenta de nuevo.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    bool CanSave() => !IsSaving;

    [RelayCommand]
    Task ChangePhotoAsync() => ShowPendingAlertAsync("Cambiar foto");

    [RelayCommand]
    Task ChangeOfficialIdFrontAsync() => ShowPendingAlertAsync("Actualizar INE frente");

    [RelayCommand]
    Task ChangeOfficialIdBackAsync() => ShowPendingAlertAsync("Actualizar INE reverso");

    [RelayCommand]
    Task ChangeProofOfAddressAsync() => ShowPendingAlertAsync("Actualizar comprobante de domicilio");

    [RelayCommand]
    Task ChangeClientPhotoAsync() => ShowPendingAlertAsync("Actualizar foto del cliente");

    private static async Task ShowPendingAlertAsync(string title)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        await page.DisplayAlertAsync(
            title,
            "La funcionalidad de documentos se conectará en el siguiente paso.",
            "Entendido");
    }

    private static async Task ShowAlertAsync(string title, string msg)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
            await p.DisplayAlertAsync(title, msg, "Entendido");
    }
}
