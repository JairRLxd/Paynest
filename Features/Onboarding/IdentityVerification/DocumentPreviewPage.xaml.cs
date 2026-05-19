namespace Paynest.Features.Onboarding.IdentityVerification;

public partial class DocumentPreviewPage : ContentPage
{
    private CancellationTokenSource? _cts;

    public DocumentPreviewPage(DocumentPreviewViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Loading started externally via LoadAsync before PushModalAsync
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
        _cts = null;
    }
}
