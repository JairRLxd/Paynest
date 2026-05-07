using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Profile;
using Paynest.Core.Validation;
using Paynest.Features.Onboarding.PaymentSetup;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Features.Onboarding.IdentityVerification;

public partial class IdentityVerificationViewModel(
    IProfileService profileService,
    IDocumentImageProcessor documentImageProcessor) : ObservableObject
{
    // ── Estado de documentos ─────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdFronteStatusText), nameof(IdFronteStatusColor))]
    private bool _idFronteUploaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdReversoStatusText), nameof(IdReversoStatusColor))]
    private bool _idReversoUploaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelfieStatusText), nameof(SelfieStatusColor))]
    private bool _selfieUploaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressFronteStatusText), nameof(AddressFronteStatusColor))]
    private bool _addressFronteUploaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressReversoStatusText), nameof(AddressReversoStatusColor))]
    private bool _addressReversoUploaded;

    public string IdFronteStatusText      => IdFronteUploaded      ? "Subido ✓" : "Agregar foto";
    public string IdReversoStatusText     => IdReversoUploaded     ? "Subido ✓" : "Agregar foto";
    public string SelfieStatusText        => SelfieUploaded        ? "Subido ✓" : "Tomar selfie";
    public string AddressFronteStatusText => AddressFronteUploaded ? "Subido ✓" : "Agregar foto";
    public string AddressReversoStatusText => AddressReversoUploaded ? "Subido ✓" : "Agregar foto";

    public Color IdFronteStatusColor      => IdFronteUploaded      ? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");
    public Color IdReversoStatusColor     => IdReversoUploaded     ? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");
    public Color SelfieStatusColor        => SelfieUploaded        ? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");
    public Color AddressFronteStatusColor => AddressFronteUploaded ? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");
    public Color AddressReversoStatusColor => AddressReversoUploaded ? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");

    // ── Carga ────────────────────────────────────────────────────────────────

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsNotLoading))] private bool _isLoading;
    public bool IsNotLoading => !IsLoading;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasGeneralError))] private string? _generalError;
    public bool HasGeneralError => GeneralError is not null;

    // ── Comandos ─────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task UploadIdFronteAsync()
        => await PickAndUploadAsync(
            DocumentType.IdentificacionOficialFrente,
            "foto de la parte frontal de tu identificación",
            allowGallery: true,
            preferCamera: true,
            () => IdFronteUploaded = true);

    [RelayCommand]
    async Task UploadIdReversoAsync()
        => await PickAndUploadAsync(
            DocumentType.IdentificacionOficialReverso,
            "foto de la parte trasera de tu identificación",
            allowGallery: true,
            preferCamera: true,
            () => IdReversoUploaded = true);

    [RelayCommand]
    async Task UploadSelfieAsync()
        => await PickAndUploadAsync(
            DocumentType.Selfie,
            "selfie",
            allowGallery: true,
            preferCamera: true,
            () => SelfieUploaded = true);

    [RelayCommand]
    async Task UploadAddressFronteAsync()
        => await PickAndUploadAsync(
            DocumentType.ComprobanteDomicilioFrente,
            "foto del frente de tu comprobante de domicilio",
            allowGallery: true,
            preferCamera: true,
            () => AddressFronteUploaded = true);

    [RelayCommand]
    async Task UploadAddressReversoAsync()
        => await PickAndUploadAsync(
            DocumentType.ComprobanteDomicilioReverso,
            "foto del reverso de tu comprobante de domicilio",
            allowGallery: true,
            preferCamera: true,
            () => AddressReversoUploaded = true);

    [RelayCommand]
    async Task ContinueAsync()
    {
        if (!IdFronteUploaded || !IdReversoUploaded || !SelfieUploaded
            || !AddressFronteUploaded || !AddressReversoUploaded)
        {
            GeneralError = "Debes subir todos los documentos para continuar.";
            return;
        }

        GeneralError = null;
        var next = MauiProgram.Services.GetRequiredService<PaymentSetupPage>();
        if (App.CurrentNavigation is { } navigation)
            await navigation.PushAsync(next);
    }

    [RelayCommand]
    async Task BackAsync()
    {
        if (App.CurrentNavigation is { } navigation)
            await navigation.PopAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task PickAndUploadAsync(
        DocumentType type,
        string fileDescription,
        bool allowGallery,
        bool preferCamera,
        Action onSuccess)
    {
        IsLoading = true;
        GeneralError = null;

        try
        {
            var result = await PickPhotoAsync(fileDescription, allowGallery, preferCamera);

            if (result is null)
                return;

            if (!AppValidators.IsValidUploadSourceImage(result.FileName))
            {
                GeneralError = "Usa una imagen JPG, JPEG, PNG, WEBP, HEIC o HEIF.";
                return;
            }

            await UploadWithCompatibilityFallbackAsync(result, type);
            onSuccess();
        }
        catch (FeatureNotSupportedException)
        {
            GeneralError = "Tu dispositivo no soporta esta función.";
        }
        catch (PermissionException)
        {
            await HandlePermissionDeniedAsync();
        }
        catch (ApiException ex)
        {
            GeneralError = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            GeneralError = ex.Message;
        }
        catch
        {
            GeneralError = "Error al subir la imagen. Intenta de nuevo.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static async Task<FileResult?> PickPhotoAsync(string fileDescription, bool allowGallery, bool preferCamera)
    {
        var page = GetCurrentPage();
        var canCapturePhoto = MediaPicker.Default.IsCaptureSupported;

        if (preferCamera && canCapturePhoto && page is not null)
        {
            var action = await page.DisplayActionSheetAsync(
                "Selecciona cómo quieres continuar",
                "Cancelar",
                null,
                allowGallery ? "Elegir de fotos" : null,
                "Tomar foto");

            return action switch
            {
                "Tomar foto" => await MediaPicker.Default.CapturePhotoAsync(),
                "Elegir de fotos" when allowGallery => await PickSinglePhotoAsync(fileDescription),
                _ => null
            };
        }

        if (canCapturePhoto && !allowGallery)
            return await MediaPicker.Default.CapturePhotoAsync();

        if (allowGallery)
            return await PickSinglePhotoAsync(fileDescription);

        return null;
    }

    private static async Task<FileResult?> PickSinglePhotoAsync(string fileDescription)
    {
        var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
        {
            Title = $"Selecciona {fileDescription}"
        });

        return results?.FirstOrDefault();
    }

    private async Task HandlePermissionDeniedAsync()
    {
        GeneralError = "Necesitamos permiso para usar la cámara o tus fotos. Actívalo en Configuración para continuar.";

        var page = GetCurrentPage();
        if (page is null)
            return;

        var openSettings = await page.DisplayAlertAsync(
            "Permisos requeridos",
            "Paynest necesita acceso a la cámara o tus fotos para subir documentos. Puedes otorgarlo en Configuración.",
            "Abrir configuración",
            "Más tarde");

        if (openSettings)
            AppInfo.Current.ShowSettingsUI();
    }

    private static Page? GetCurrentPage()
        => App.CurrentPage;

    private async Task UploadWithCompatibilityFallbackAsync(FileResult result, DocumentType type)
    {
        try
        {
            var webpFile = await documentImageProcessor.PrepareForUploadAsync(result, type, ImageUploadFormat.Webp);
            await profileService.UploadDocumentAsync(type, webpFile);
        }
        catch (ApiException ex) when (RequiresLegacyImageFormat(ex.Message))
        {
            var jpegFile = await documentImageProcessor.PrepareForUploadAsync(result, type, ImageUploadFormat.Jpeg);
            await profileService.UploadDocumentAsync(type, jpegFile);
        }
    }

    private static bool RequiresLegacyImageFormat(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var normalized = message.ToUpperInvariant();
        return normalized.Contains("JPG")
            || normalized.Contains("JPEG")
            || normalized.Contains("PNG");
    }
}
