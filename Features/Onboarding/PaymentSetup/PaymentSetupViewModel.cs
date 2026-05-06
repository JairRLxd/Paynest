using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Profile;
using Paynest.Core.Validation;

namespace Paynest.Features.Onboarding.PaymentSetup;

public partial class PaymentSetupViewModel(
    IProfileService profileService,
    OnboardingSession session,
    Paynest.Services.AuthStateService authState,
    Paynest.Services.CollectorPaymentSettings paymentSettings) : ObservableObject
{
    // ── Efectivo ────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _efectivoEnabled = true;

    // ── Transferencia bancaria ───────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBankFields))]
    private bool _transferenciaEnabled = true;

    public bool ShowBankFields => TransferenciaEnabled;

    [ObservableProperty] private string _bankName       = string.Empty;
    [ObservableProperty] private string _accountHolder  = string.Empty;
    [ObservableProperty] private string _clabe          = string.Empty;

    public List<string> Banks { get; } =
    [
        "BBVA","Banorte","Santander","Citibanamex","HSBC",
        "Scotiabank","Inbursa","BanBajío","Afirme","Banco Azteca","Nu","Clip"
    ];

    // ── Terminal de cobro ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTerminalFields))]
    private bool _terminalEnabled = true;

    public bool ShowTerminalFields => TerminalEnabled;

    [ObservableProperty] private string _terminalProvider  = string.Empty;
    [ObservableProperty] private string _terminalReference = string.Empty;

    public List<string> TerminalProviders { get; } =
    [
        "Clip","Conekta","Sr. Pago","iZettle","Cuadricula"
    ];

    // ── Estado ──────────────────────────────────────────────────────────────

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsNotLoading))] private bool _isLoading;
    public bool IsNotLoading => !IsLoading;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasBankNameError))]        private string? _bankNameError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasAccountHolderError))]   private string? _accountHolderError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasClabeError))]           private string? _clabeError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasTerminalProviderError))] private string? _terminalProviderError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasGeneralError))]         private string? _generalError;

    public bool HasBankNameError        => BankNameError        is not null;
    public bool HasAccountHolderError   => AccountHolderError   is not null;
    public bool HasClabeError           => ClabeError           is not null;
    public bool HasTerminalProviderError => TerminalProviderError is not null;
    public bool HasGeneralError         => GeneralError         is not null;

    // ── Comandos ────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task SaveAsync()
    {
        if (!Validate()) return;

        IsLoading    = true;
        GeneralError = null;

        try
        {
            if (session.PersonalInfo is not null)
                await profileService.SavePersonalInfoAsync(session.PersonalInfo);

            await profileService.SavePaymentConfigAsync(new PaymentConfigRequest(
                EfectivoEnabled,
                TransferenciaEnabled,
                TransferenciaEnabled ? BankName.Trim()      : null,
                TransferenciaEnabled ? AccountHolder.Trim() : null,
                TransferenciaEnabled ? Clabe.Trim()         : null,
                TerminalEnabled,
                TerminalEnabled ? TerminalProvider.Trim()   : null,
                TerminalEnabled ? TerminalReference.Trim()  : null
            ));

            authState.MarkProfileCompleted();
            paymentSettings.Update(EfectivoEnabled, TransferenciaEnabled, TerminalEnabled);
            NavigateToMain();
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

    [RelayCommand]
    async Task BackAsync()
        => await Application.Current!.MainPage!.Navigation.PopAsync();

    // ── Validación ──────────────────────────────────────────────────────────

    private bool Validate()
    {
        GeneralError = null;

        if (!EfectivoEnabled && !TransferenciaEnabled && !TerminalEnabled)
        {
            GeneralError = "Selecciona al menos un método de cobro.";
            return false;
        }

        if (TransferenciaEnabled)
        {
            BankNameError      = string.IsNullOrWhiteSpace(BankName)      ? "Selecciona tu banco"             : null;
            AccountHolderError = string.IsNullOrWhiteSpace(AccountHolder) ? "Ingresa el nombre del titular"   : null;

            if (string.IsNullOrWhiteSpace(Clabe))
                ClabeError = "Ingresa tu CLABE";
            else if (!AppValidators.IsValidClabe(Clabe))
                ClabeError = "La CLABE debe tener 18 dígitos";
            else
                ClabeError = null;
        }
        else
        {
            BankNameError = AccountHolderError = ClabeError = null;
        }

        TerminalProviderError = TerminalEnabled && string.IsNullOrWhiteSpace(TerminalProvider)
            ? "Selecciona el proveedor de terminal"
            : null;

        return BankNameError is null && AccountHolderError is null
            && ClabeError is null && TerminalProviderError is null;
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static void NavigateToMain()
    {
        var shell = MauiProgram.Services.GetRequiredService<AppShell>();
        Application.Current!.MainPage = shell;
    }
}
