using Paynest.Features.Cobrador.Models;
using Paynest.Features.Cobrador.ViewModels;
using Paynest.Services;

namespace Paynest.Features.Cobrador.Pages;

public partial class SchedulePage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;
    private readonly CollectorRefreshController _refreshController;
    private TaskCompletionSource<bool>? _rescheduleCompletion;

    public SchedulePage(ScheduleViewModel vm, CollectorDataRefreshService refreshService)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
        _refreshController = new CollectorRefreshController(
            refreshService,
            CollectorRefreshScope.Agenda,
            vm.RefreshAsync);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _refreshController.ActivateAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshController.Deactivate();
    }

    private async void OnCalendarDayTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: AgendaCalendarDayItem day })
            return;

        await _viewModel.SelectDayCommand.ExecuteAsync(day);
    }

    private async void OnRegisterPaymentClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: AgendaCollectionItem item })
            return;

        await _viewModel.OpenRegisterPaymentCommand.ExecuteAsync(item);
    }

    private async void OnRescheduleClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: AgendaCollectionItem item })
            return;

        // Show the sheet and wait; sheet closes when user confirms or cancels.
        var confirmed = await ShowRescheduleSheetAsync(item);
        if (!confirmed)
            return;

        // Read values while elements are still in DOM (just IsVisible=false).
        var newDate = RescheduleDatePicker.Date ?? DateTime.Today.AddDays(1);
        var reason = RescheduleReasonEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            reason = null;

        await _viewModel.RescheduleAsync(item, newDate, reason);
    }

    private async Task<bool> ShowRescheduleSheetAsync(AgendaCollectionItem item)
    {
        _rescheduleCompletion?.TrySetResult(false);
        _rescheduleCompletion = new TaskCompletionSource<bool>();

        RescheduleTitleLabel.Text = item.ClientName;
        RescheduleDatePicker.Date = item.DueDate > DateTime.Today ? item.DueDate : DateTime.Today.AddDays(1);
        RescheduleReasonEntry.Text = string.Empty;

        RescheduleOverlay.Opacity = 0;
        ReschedulePanel.TranslationY = 48;
        RescheduleOverlay.IsVisible = true;
        await Task.WhenAll(
            RescheduleOverlay.FadeToAsync(1, 150),
            ReschedulePanel.TranslateToAsync(0, 0, 220, Easing.CubicOut));

        return await _rescheduleCompletion.Task;
    }

    private async Task CloseRescheduleSheetAsync(bool confirmed)
    {
        var completion = _rescheduleCompletion;
        if (completion is null)
            return;

        _rescheduleCompletion = null;
        await Task.WhenAll(
            ReschedulePanel.TranslateToAsync(0, 48, 180, Easing.CubicIn),
            RescheduleOverlay.FadeToAsync(0, 160));
        RescheduleOverlay.IsVisible = false;
        completion.TrySetResult(confirmed);
    }

    private async void OnRescheduleConfirmed(object? sender, EventArgs e)
    {
        await CloseRescheduleSheetAsync(true);
    }

    private async void OnRescheduleOverlayDismissed(object? sender, EventArgs e)
    {
        await CloseRescheduleSheetAsync(false);
    }
}
