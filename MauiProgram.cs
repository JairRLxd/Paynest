using System.Net;
using Microsoft.Extensions.Logging;
using Paynest.Core.Interfaces;
using Paynest.Features.Auth.Login;
using Paynest.Features.Auth.Register;
using Paynest.Features.Client.Api;
using Paynest.Features.Client.Application;
using Paynest.Features.Client.Mocks;
using Paynest.Features.Client.Repositories;
using Paynest.Features.Cobrador.Mocks;
using Paynest.Features.Cobrador.Clients.AddClient.ViewModels;
using Paynest.Features.Cobrador.Clients.AddClient.Views;
using Paynest.Features.Cobrador.Clients.ClientPicker.ViewModels;
using Paynest.Features.Cobrador.Clients.ClientPicker.Views;
using Paynest.Features.Cobrador.Clients.ComprobanteViewer.ViewModels;
using Paynest.Features.Cobrador.Clients.ComprobanteViewer.Views;
using Paynest.Features.Cobrador.Clients.CreateDebt.ViewModels;
using Paynest.Features.Cobrador.Clients.CreateDebt.Views;
using Paynest.Features.Cobrador.Clients.Detail.ViewModels;
using Paynest.Features.Cobrador.Clients.Detail.Views;
using Paynest.Features.Cobrador.Clients.Edit.ViewModels;
using Paynest.Features.Cobrador.Clients.Edit.Views;
using Paynest.Features.Cobrador.Clients.RegisterPayment.ViewModels;
using Paynest.Features.Cobrador.Clients.RegisterPayment.Views;
using Paynest.Features.Cobrador.Clients.ViewModels;
using Paynest.Features.Cobrador.Clients.Views;
using Paynest.Features.Cobrador.Collections.ViewModels;
using Paynest.Features.Cobrador.Collections.Views;
using Paynest.Features.Cobrador.Home.ViewModels;
using Paynest.Features.Cobrador.Home.Views;
using Paynest.Features.Cobrador.Profile.Views;
using Paynest.Features.Cobrador.Schedule.Views;
using Paynest.Features.Onboarding;
using Paynest.Features.Onboarding.CompleteProfile;
using Paynest.Features.Onboarding.IdentityVerification;
using Paynest.Features.Onboarding.PaymentSetup;
using Paynest.Features.Splash;
using Paynest.Infrastructure;
using Paynest.Infrastructure.Http;
using Paynest.Services;

namespace Paynest;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var apiBaseUri = ApiConstants.BaseUri;

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler { CookieContainer = cookieContainer };
        var httpClient = new HttpClient(handler) { BaseAddress = apiBaseUri };

        builder.Services.AddSingleton(httpClient);
        builder.Services.AddSingleton<IAuthService, AuthApiClient>();
        builder.Services.AddSingleton<AuthStateService>();
        builder.Services.AddSingleton<CollectorPaymentSettings>();
        builder.Services.AddSingleton<ClientDataRefreshService>();
        builder.Services.AddSingleton<ReceiptActionService>();

        // MOCK_SWAP_POINT: cambiar PaynestUseMocks=false y eliminar Features/*/Mocks para backend real puro.
        if (ApiConstants.UseMocks)
        {
            builder.Services.AddSingleton<ICollectorInviteService, MockCollectorInviteService>();
            builder.Services.AddSingleton<ICollectorDebtService, MockCollectorDebtService>();
            builder.Services.AddSingleton<ICollectorPaymentService, MockCollectorPaymentService>();
            builder.Services.AddSingleton<ICollectorCollectionsService, MockCollectorCollectionsService>();
            builder.Services.AddSingleton<ICollectorClientService, MockCollectorClientService>();
            builder.Services.AddSingleton<ICollectorDashboardService, MockCollectorDashboardService>();
            builder.Services.AddSingleton<IDebtApiClient, MockDebtApiClient>();
        }
        else
        {
            builder.Services.AddSingleton<ICollectorInviteService, CollectorInviteService>();
            builder.Services.AddSingleton<ICollectorDebtService, CollectorDebtApiClient>();
            builder.Services.AddSingleton<ICollectorPaymentService, CollectorPaymentApiClient>();
            builder.Services.AddSingleton<ICollectorCollectionsService, CollectorCollectionsApiClient>();
            builder.Services.AddSingleton<ICollectorClientService, CollectorClientApiClient>();
            builder.Services.AddSingleton<ICollectorDashboardService, CollectorDashboardApiClient>();
            builder.Services.AddSingleton<IDebtApiClient, HttpDebtApiClient>();
        }

        builder.Services.AddSingleton<IDocumentImageProcessor, DocumentImageProcessor>();
        builder.Services.AddSingleton<IProfileService, ProfileApiClient>();
        builder.Services.AddSingleton<PostalCodeClient>();

        builder.Services.AddSingleton<IClientDebtRepository, DebtApiRepository>();
        builder.Services.AddSingleton<GetClientDashboardUseCase>();
        builder.Services.AddSingleton<GetGroupInstallmentsUseCase>();
        builder.Services.AddSingleton<GetMonthlyInstallmentsUseCase>();
        builder.Services.AddSingleton<GetPaidReceiptsUseCase>();
        builder.Services.AddSingleton<SetCurrentGroupUseCase>();
        builder.Services.AddSingleton<MarkInstallmentAsPaidUseCase>();
        builder.Services.AddSingleton<IClientDebtService, ClientDebtService>();

        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<LinkCollectorPage>();

        builder.Services.AddSingleton<OnboardingSession>();
        builder.Services.AddTransient<CompleteProfileViewModel>();
        builder.Services.AddTransient<CompleteProfilePage>();
        builder.Services.AddTransient<IdentityVerificationViewModel>();
        builder.Services.AddTransient<IdentityVerificationPage>();
        builder.Services.AddTransient<PaymentSetupViewModel>();
        builder.Services.AddTransient<PaymentSetupPage>();

        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ClientsViewModel>();
        builder.Services.AddTransient<ClientsPage>();
        builder.Services.AddTransient<AddClientViewModel>();
        builder.Services.AddTransient<AddClientPage>();
        builder.Services.AddTransient<ClientPickerViewModel>();
        builder.Services.AddTransient<ClientPickerPage>();
        builder.Services.AddTransient<ClientDetailViewModel>();
        builder.Services.AddTransient<ClientDetailPage>();
        builder.Services.AddTransient<EditClientViewModel>();
        builder.Services.AddTransient<EditClientPage>();
        builder.Services.AddTransient<RegisterPaymentViewModel>();
        builder.Services.AddTransient<RegisterPaymentPage>();
        builder.Services.AddTransient<ComprobanteViewerViewModel>();
        builder.Services.AddTransient<ComprobanteViewerPage>();
        builder.Services.AddTransient<CreateDebtViewModel>();
        builder.Services.AddTransient<CreateDebtPage>();
        builder.Services.AddTransient<CalendarPickerViewModel>();
        builder.Services.AddTransient<CalendarPickerPage>();
        builder.Services.AddTransient<CollectionsViewModel>();
        builder.Services.AddTransient<CollectionsPage>();
        builder.Services.AddTransient<SchedulePage>();
        builder.Services.AddTransient<UserProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        Services = app.Services;
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogInformation("Paynest API Base URL resolved to {BaseUrl}", apiBaseUri);
        return app;
    }
}
