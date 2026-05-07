using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Models.Profile;
using Paynest.Core.Validation;
using Paynest.Features.Onboarding.IdentityVerification;
using Paynest.Infrastructure.Http;
using Paynest.Services;

namespace Paynest.Features.Onboarding.CompleteProfile;

public partial class CompleteProfileViewModel(
    OnboardingSession session,
    PostalCodeClient postalClient,
    AuthStateService authState) : ObservableObject
{
    // ── Datos personales ────────────────────────────────────────────────────

    [ObservableProperty] private string _visibleName = string.Empty;
    [ObservableProperty] private string _phone       = string.Empty;
    [ObservableProperty] private string _address     = string.Empty;
    [ObservableProperty] private string _curp        = string.Empty;
    [ObservableProperty] private string _rfc         = string.Empty;

    // ── Ubicación ───────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPostalCodeError))]
    private string _postalCode = string.Empty;

    [ObservableProperty] private string _municipio = string.Empty;
    [ObservableProperty] private string _estado    = string.Empty;

    // Colonias disponibles para el CP ingresado
    [ObservableProperty] private List<string> _colonias = [];
    [ObservableProperty] private string?      _colonia;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLookingUp))]
    private bool _isLookingUp;

    public bool IsNotLookingUp => !IsLookingUp;

    partial void OnPostalCodeChanged(string value)
    {
        if (value.Length == 5)
            LookupPostalCodeCommand.Execute(null);
        else
        {
            Colonias   = [];
            Colonia    = null;
            Municipio  = string.Empty;
            Estado     = string.Empty;
        }
    }

    [RelayCommand]
    async Task LookupPostalCodeAsync()
    {
        PostalCodeError = null;
        IsLookingUp     = true;

        var result = await postalClient.LookupAsync(
            PostalCode.Trim(), authState.AccessToken!, CancellationToken.None);

        IsLookingUp = false;

        if (result is null)
        {
            PostalCodeError = "Código postal no encontrado";
            Colonias        = [];
            Colonia         = null;
            Municipio       = string.Empty;
            Estado          = string.Empty;
        }
        else
        {
            Municipio = result.Municipio;
            Estado    = result.Estado;
            Colonias  = result.Colonias.Select(c => c.Nombre).ToList();
            Colonia   = Colonias.FirstOrDefault();
        }
    }

    // ── Tipo de operación ───────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpresa),
                              nameof(PersonaBgColor), nameof(EmpresaBgColor),
                              nameof(PersonaTextColor), nameof(EmpresaTextColor))]
    private OperationType _operationType = OperationType.Persona;

    public bool IsEmpresa => OperationType == OperationType.Empresa;

    public Color PersonaBgColor   => !IsEmpresa ? Color.FromArgb("#2A6349") : Colors.Transparent;
    public Color EmpresaBgColor   =>  IsEmpresa ? Color.FromArgb("#2A6349") : Colors.Transparent;
    public Color PersonaTextColor => !IsEmpresa ? Colors.White : Color.FromArgb("#6B6B6B");
    public Color EmpresaTextColor =>  IsEmpresa ? Colors.White : Color.FromArgb("#6B6B6B");

    // ── Datos de negocio ────────────────────────────────────────────────────

    [ObservableProperty] private string _businessName     = string.Empty;
    [ObservableProperty] private string _businessType     = string.Empty;
    [ObservableProperty] private string _businessLogoPath = string.Empty;

    public List<string> BusinessTypes { get; } =
    [
        "Comercio al por menor","Servicios financieros","Construcción",
        "Restaurante / Alimentos","Salud y bienestar","Educación",
        "Transporte","Tecnología","Manufactura","Otro"
    ];

    // ── Errores ─────────────────────────────────────────────────────────────

    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasVisibleNameError))]  private string? _visibleNameError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasPhoneError))]        private string? _phoneError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasPostalCodeError))]   private string? _postalCodeError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasColoniaError))]      private string? _coloniaError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasAddressError))]      private string? _addressError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasCurpError))]         private string? _curpError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasRfcError))]          private string? _rfcError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasBusinessNameError))] private string? _businessNameError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasBusinessTypeError))] private string? _businessTypeError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasGeneralError))]      private string? _generalError;

    public bool HasVisibleNameError  => VisibleNameError  is not null;
    public bool HasPhoneError        => PhoneError        is not null;
    public bool HasPostalCodeError   => PostalCodeError   is not null;
    public bool HasColoniaError      => ColoniaError      is not null;
    public bool HasAddressError      => AddressError      is not null;
    public bool HasCurpError         => CurpError         is not null;
    public bool HasRfcError          => RfcError          is not null;
    public bool HasBusinessNameError => BusinessNameError is not null;
    public bool HasBusinessTypeError => BusinessTypeError is not null;
    public bool HasGeneralError      => GeneralError      is not null;

    // ── Carga ───────────────────────────────────────────────────────────────

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsNotLoading))] private bool _isLoading;
    public bool IsNotLoading => !IsLoading;

    // ── Comandos ────────────────────────────────────────────────────────────

    [RelayCommand] void SelectPersona() => OperationType = OperationType.Persona;
    [RelayCommand] void SelectEmpresa() => OperationType = OperationType.Empresa;

    [RelayCommand]
    async Task PickBusinessLogoAsync()
    {
        var result = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Selecciona el logo del negocio",
            FileTypes   = FilePickerFileType.Images
        });
        if (result is not null)
            BusinessLogoPath = result.FullPath;
    }

    [RelayCommand]
    async Task ContinueAsync()
    {
        if (!Validate()) return;

        GeneralError = null;
        try
        {
            session.PersonalInfo = new UserProfileRequest(
                VisibleName.Trim(), Phone.Trim(),
                PostalCode.Trim(), Colonia!, Municipio, Estado,
                Address.Trim(), Curp.Trim().ToUpper(), Rfc.Trim().ToUpper(),
                OperationType,
                BusinessName:     IsEmpresa ? BusinessName.Trim() : null,
                BusinessType:     IsEmpresa ? BusinessType.Trim() : null,
                BusinessLogoPath: IsEmpresa ? BusinessLogoPath    : null
            );

            var next = MauiProgram.Services.GetRequiredService<IdentityVerificationPage>();
            if (App.CurrentNavigation is { } navigation)
                await navigation.PushAsync(next);
        }
        catch (Exception ex)
        {
            GeneralError = ex.Message;
        }
    }

    [RelayCommand]
    async Task BackAsync()
    {
        var loginPage = MauiProgram.Services.GetRequiredService<Features.Auth.Login.LoginPage>();
        App.SetRootPage(new NavigationPage(loginPage)
        {
            BarBackgroundColor = Colors.Transparent,
            BackgroundColor    = Color.FromArgb("#F2F0EB")
        });
    }

    // ── Validación ──────────────────────────────────────────────────────────

    private bool Validate()
    {
        VisibleNameError = string.IsNullOrWhiteSpace(VisibleName) ? "Ingresa tu nombre visible" : null;

        if (string.IsNullOrWhiteSpace(Phone))
            PhoneError = "Ingresa tu teléfono";
        else if (!AppValidators.IsValidPhone(Phone))
            PhoneError = "El teléfono debe tener 10 dígitos";
        else
            PhoneError = null;

        PostalCodeError = string.IsNullOrWhiteSpace(PostalCode) ? "Ingresa tu código postal" : null;
        if (PostalCodeError is null && string.IsNullOrWhiteSpace(Municipio))
            PostalCodeError = "Código postal no válido";

        ColoniaError = string.IsNullOrWhiteSpace(Colonia) ? "Selecciona una colonia"  : null;
        AddressError = string.IsNullOrWhiteSpace(Address) ? "Ingresa tu dirección"    : null;

        if (string.IsNullOrWhiteSpace(Curp))
            CurpError = "Ingresa tu CURP";
        else if (!AppValidators.IsValidCurp(Curp))
            CurpError = "CURP inválida — revisa el formato (18 caracteres)";
        else
            CurpError = null;

        if (string.IsNullOrWhiteSpace(Rfc))
            RfcError = "Ingresa tu RFC";
        else if (!AppValidators.IsValidRfc(Rfc, IsEmpresa))
            RfcError = IsEmpresa ? "RFC de empresa inválido (12 caracteres)" : "RFC personal inválido (13 caracteres)";
        else
            RfcError = null;

        if (IsEmpresa)
        {
            BusinessNameError = string.IsNullOrWhiteSpace(BusinessName) ? "Ingresa el nombre del negocio"  : null;
            BusinessTypeError = string.IsNullOrWhiteSpace(BusinessType) ? "Selecciona el giro del negocio" : null;
        }
        else
        {
            BusinessNameError = null;
            BusinessTypeError = null;
        }

        return VisibleNameError is null && PhoneError is null && PostalCodeError is null
            && ColoniaError is null && AddressError is null && CurpError is null && RfcError is null
            && BusinessNameError is null && BusinessTypeError is null;
    }
}
