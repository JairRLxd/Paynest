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
    public bool IsEditMode { get; set; }

    // URLs para previsualización (llegan del backend en el campo url de cada documento)
    private string? _idFronteUrl;
    private string? _idReversoUrl;
    private string? _selfieUrl;
    private string? _addressFronteUrl;
    private string? _addressReversoUrl;
    // ── Estado de documentos ─────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdFronteStatusText), nameof(IdFronteStatusColor))]
    private bool _idFronteUploaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdFronteStatusText))]
    private DateTimeOffset? _idFronteUploadedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdReversoStatusText), nameof(IdReversoStatusColor))]
    private bool _idReversoUploaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdReversoStatusText))]
    private DateTimeOffset? _idReversoUploadedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelfieStatusText), nameof(SelfieStatusColor))]
    private bool _selfieUploaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelfieStatusText))]
    private DateTimeOffset? _selfieUploadedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressFronteStatusText), nameof(AddressFronteStatusColor))]
    private bool _addressFronteUploaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressFronteStatusText))]
    private DateTimeOffset? _addressFronteUploadedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressReversoStatusText), nameof(AddressReversoStatusColor))]
    private bool _addressReversoUploaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressReversoStatusText))]
    private DateTimeOffset? _addressReversoUploadedAt;

    // ── Progreso ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DocumentProgress), nameof(DocumentProgressText))]
    private int _uploadedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DocumentProgress), nameof(DocumentProgressText))]
    private int _requiredCount = 5;

    public double DocumentProgress  => RequiredCount > 0 ? (double)UploadedCount / RequiredCount : 0;
    public string DocumentProgressText => $"{UploadedCount} de {RequiredCount} documentos";

    // ── Status texts / colors ─────────────────────────────────────────────────

    private static string FormatDate(DateTimeOffset? date)
        => date.HasValue ? $" · {date.Value.LocalDateTime:d MMM}" : string.Empty;

    public string IdFronteStatusText       => IdFronteUploaded      ? $"Subido ✓{FormatDate(IdFronteUploadedAt)}"       : "Agregar foto";
    public string IdReversoStatusText      => IdReversoUploaded     ? $"Subido ✓{FormatDate(IdReversoUploadedAt)}"      : "Agregar foto";
    public string SelfieStatusText         => SelfieUploaded        ? $"Subido ✓{FormatDate(SelfieUploadedAt)}"         : "Tomar selfie";
    public string AddressFronteStatusText  => AddressFronteUploaded ? $"Subido ✓{FormatDate(AddressFronteUploadedAt)}"  : "Agregar foto";
    public string AddressReversoStatusText => AddressReversoUploaded? $"Subido ✓{FormatDate(AddressReversoUploadedAt)}" : "Agregar foto";

    public Color IdFronteStatusColor       => IdFronteUploaded      ? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");
    public Color IdReversoStatusColor      => IdReversoUploaded     ? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");
    public Color SelfieStatusColor         => SelfieUploaded        ? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");
    public Color AddressFronteStatusColor  => AddressFronteUploaded ? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");
    public Color AddressReversoStatusColor => AddressReversoUploaded? Color.FromArgb("#2A6349") : Color.FromArgb("#1C1C1E");

    // ── Carga ────────────────────────────────────────────────────────────────

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsNotLoading))] private bool _isLoading;
    public bool IsNotLoading => !IsLoading;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasGeneralError))] private string? _generalError;
    public bool HasGeneralError => GeneralError is not null;

    // ── Carga de estado (modo edición) ───────────────────────────────────────

    public async Task LoadDocumentsStatusAsync()
    {
        IsLoading    = true;
        GeneralError = null;
        try
        {
            var data = await profileService.GetDocumentsStatusAsync();
            UploadedCount = data.UploadedCount;
            RequiredCount = data.RequiredCount;

            foreach (var doc in data.Documents)
            {
                switch (doc.Type)
                {
                    case "IdentificacionOficialFrente":
                        IdFronteUploaded   = doc.Uploaded;
                        IdFronteUploadedAt = doc.UploadedAt;
                        _idFronteUrl       = doc.Url;
                        break;
                    case "IdentificacionOficialReverso":
                        IdReversoUploaded   = doc.Uploaded;
                        IdReversoUploadedAt = doc.UploadedAt;
                        _idReversoUrl       = doc.Url;
                        break;
                    case "Selfie":
                        SelfieUploaded   = doc.Uploaded;
                        SelfieUploadedAt = doc.UploadedAt;
                        _selfieUrl       = doc.Url;
                        break;
                    case "ComprobanteDomicilioFrente":
                        AddressFronteUploaded   = doc.Uploaded;
                        AddressFronteUploadedAt = doc.UploadedAt;
                        _addressFronteUrl       = doc.Url;
                        break;
                    case "ComprobanteDomicilioReverso":
                        AddressReversoUploaded   = doc.Uploaded;
                        AddressReversoUploadedAt = doc.UploadedAt;
                        _addressReversoUrl       = doc.Url;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            GeneralError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Tap commands (ver si subido, subir si no) ────────────────────────────

    [RelayCommand]
    Task TapIdFronteAsync()    => CanPreview(IdFronteUploaded, _idFronteUrl)
        ? ShowPreviewAsync("ID oficial — Frente",    _idFronteUrl!, UploadIdFronteAsync)
        : UploadIdFronteAsync();

    [RelayCommand]
    Task TapIdReversoAsync()   => CanPreview(IdReversoUploaded, _idReversoUrl)
        ? ShowPreviewAsync("ID oficial — Reverso",   _idReversoUrl!, UploadIdReversoAsync)
        : UploadIdReversoAsync();

    [RelayCommand]
    Task TapSelfieAsync()      => CanPreview(SelfieUploaded, _selfieUrl)
        ? ShowPreviewAsync("Selfie",                 _selfieUrl!, UploadSelfieAsync)
        : UploadSelfieAsync();

    [RelayCommand]
    Task TapAddressFronteAsync() => CanPreview(AddressFronteUploaded, _addressFronteUrl)
        ? ShowPreviewAsync("Comprobante — Frente",   _addressFronteUrl!, UploadAddressFronteAsync)
        : UploadAddressFronteAsync();

    [RelayCommand]
    Task TapAddressReversoAsync() => CanPreview(AddressReversoUploaded, _addressReversoUrl)
        ? ShowPreviewAsync("Comprobante — Reverso",  _addressReversoUrl!, UploadAddressReversoAsync)
        : UploadAddressReversoAsync();

    private static bool CanPreview(bool uploaded, string? url)
        => uploaded && !string.IsNullOrWhiteSpace(url);

    private static async Task ShowPreviewAsync(string title, string url, Func<Task> onReplace)
    {
        var page = MauiProgram.Services.GetRequiredService<DocumentPreviewPage>();
        if (page.BindingContext is DocumentPreviewViewModel previewVm)
        {
            previewVm.Title     = title;
            previewVm.OnReplace = onReplace;
            _ = previewVm.LoadAsync(url);
        }

        if (App.CurrentNavigation is { } nav)
            await nav.PushModalAsync(page);
    }

    // ── Comandos ─────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task UploadIdFronteAsync()
        => await PickAndUploadAsync(
            DocumentType.IdentificacionOficialFrente,
            "foto de la parte frontal de tu identificación",
            allowGallery: true, preferCamera: true,
            wasUploaded: IdFronteUploaded,
            onSuccess: () => { IdFronteUploaded = true; IdFronteUploadedAt = DateTimeOffset.Now; });

    [RelayCommand]
    async Task UploadIdReversoAsync()
        => await PickAndUploadAsync(
            DocumentType.IdentificacionOficialReverso,
            "foto de la parte trasera de tu identificación",
            allowGallery: true, preferCamera: true,
            wasUploaded: IdReversoUploaded,
            onSuccess: () => { IdReversoUploaded = true; IdReversoUploadedAt = DateTimeOffset.Now; });

    [RelayCommand]
    async Task UploadSelfieAsync()
        => await PickAndUploadAsync(
            DocumentType.Selfie,
            "selfie",
            allowGallery: true, preferCamera: true,
            wasUploaded: SelfieUploaded,
            onSuccess: () => { SelfieUploaded = true; SelfieUploadedAt = DateTimeOffset.Now; });

    [RelayCommand]
    async Task UploadAddressFronteAsync()
        => await PickAndUploadAsync(
            DocumentType.ComprobanteDomicilioFrente,
            "foto del frente de tu comprobante de domicilio",
            allowGallery: true, preferCamera: true,
            wasUploaded: AddressFronteUploaded,
            onSuccess: () => { AddressFronteUploaded = true; AddressFronteUploadedAt = DateTimeOffset.Now; });

    [RelayCommand]
    async Task UploadAddressReversoAsync()
        => await PickAndUploadAsync(
            DocumentType.ComprobanteDomicilioReverso,
            "foto del reverso de tu comprobante de domicilio",
            allowGallery: true, preferCamera: true,
            wasUploaded: AddressReversoUploaded,
            onSuccess: () => { AddressReversoUploaded = true; AddressReversoUploadedAt = DateTimeOffset.Now; });

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

        if (IsEditMode)
        {
            if (App.CurrentNavigation is { } nav)
                await nav.PopAsync();
            return;
        }

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
        bool wasUploaded,
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
            if (!wasUploaded)
                UploadedCount++;
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
