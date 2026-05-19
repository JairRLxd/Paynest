using System.Net;
using Microsoft.Extensions.Logging;
using Paynest.Core.Interfaces;
using Paynest.Features.Auth.ForgotPassword;
using Paynest.Features.Auth.Login;
using Paynest.Features.Auth.Register;
using Paynest.Features.Client.Api;
using Paynest.Features.Client.Application;
using Paynest.Features.Client.Repositories;
using Paynest.Features.Cobrador.UseCases;
using Paynest.Features.Cobrador.Pages;
using Paynest.Features.Cobrador.ViewModels;
using Paynest.Features.Onboarding;
using Paynest.Features.Onboarding.CompleteProfile;
using Paynest.Features.Onboarding.IdentityVerification;
using DocumentPreviewPage = Paynest.Features.Onboarding.IdentityVerification.DocumentPreviewPage;
using DocumentPreviewViewModel = Paynest.Features.Onboarding.IdentityVerification.DocumentPreviewViewModel;
using Paynest.Features.Onboarding.PaymentSetup;
using Paynest.Features.Splash;
using Paynest.Infrastructure;
using Paynest.Infrastructure.Http;
using Paynest.Services;
using ZXing.Net.Maui.Controls;

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
            .UseBarcodeReader()
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
        builder.Services.AddSingleton<CollectorDataRefreshService>();
        builder.Services.AddSingleton<ReceiptActionService>();

        builder.Services.AddSingleton<ICollectorInviteService, CollectorInviteService>();
        builder.Services.AddSingleton<ICollectorDebtService, CollectorDebtApiClient>();
        builder.Services.AddSingleton<ICollectorPaymentService, CollectorPaymentApiClient>();
        builder.Services.AddSingleton<ICollectorCollectionsService, CollectorCollectionsApiClient>();
        builder.Services.AddSingleton<ICollectorAgendaService, CollectorAgendaApiClient>();
        builder.Services.AddSingleton<ICollectorClientService, CollectorClientApiClient>();
        builder.Services.AddSingleton<ICollectorDashboardService, CollectorDashboardApiClient>();
        builder.Services.AddSingleton<IDebtApiClient, HttpDebtApiClient>();

        builder.Services.AddSingleton<IDocumentImageProcessor, DocumentImageProcessor>();
        builder.Services.AddSingleton<IProfileService, ProfileApiClient>();
        builder.Services.AddSingleton<PostalCodeClient>();

        builder.Services.AddSingleton<GetClientListUseCase>();
        builder.Services.AddSingleton<GetClientDetailUseCase>();
        builder.Services.AddSingleton<GetDashboardUseCase>();
        builder.Services.AddSingleton<GetCollectionsDashboardUseCase>();
        builder.Services.AddSingleton<GetCollectionsUseCase>();
        builder.Services.AddSingleton<GetAgendaMonthUseCase>();
        builder.Services.AddSingleton<GetAgendaDayUseCase>();
        builder.Services.AddSingleton<RescheduleAgendaUseCase>();
        builder.Services.AddSingleton<GetRecentCollectorPaymentsUseCase>();
        builder.Services.AddSingleton<PreviewPaymentUseCase>();
        builder.Services.AddSingleton<RegisterPaymentUseCase>();
        builder.Services.AddSingleton<DownloadTicketUseCase>();

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
        builder.Services.AddTransient<ForgotPasswordViewModel>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<LinkCollectorPage>();

        builder.Services.AddSingleton<OnboardingSession>();
        builder.Services.AddTransient<CompleteProfileViewModel>();
        builder.Services.AddTransient<CompleteProfilePage>();
        builder.Services.AddTransient<IdentityVerificationViewModel>();
        builder.Services.AddTransient<IdentityVerificationPage>();
        builder.Services.AddTransient<DocumentPreviewViewModel>();
        builder.Services.AddTransient<DocumentPreviewPage>();
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
        builder.Services.AddTransient<ScheduleViewModel>();
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
