using Paynest.Models;
using Paynest.Services;

namespace Paynest.Features.Client.ViewModels;

public sealed class NotificationsPageViewModel : BaseViewModel
{
	private readonly IClientDebtService _service;
	private bool _notifyThreeDaysBefore = true;
	private bool _notifySameDay = true;
	private TimeSpan _reminderTime = new(9, 0, 0);
	private string _timezone = "America/Mexico_City";
	private bool _pushEnabled = true;
	private bool _emailEnabled;
	private bool _isSaving;

	public NotificationsPageViewModel(IClientDebtService service)
	{
		_service = service;
	}

	public bool NotifyThreeDaysBefore
	{
		get => _notifyThreeDaysBefore;
		set
		{
			SetProperty(ref _notifyThreeDaysBefore, value);
			RaiseNotificationPreviewChanged();
		}
	}

	public bool NotifySameDay
	{
		get => _notifySameDay;
		set
		{
			SetProperty(ref _notifySameDay, value);
			RaiseNotificationPreviewChanged();
		}
	}

	public TimeSpan ReminderTime
	{
		get => _reminderTime;
		set
		{
			SetProperty(ref _reminderTime, value);
			RaiseNotificationPreviewChanged();
		}
	}

	public string Timezone
	{
		get => _timezone;
		set => SetProperty(ref _timezone, value);
	}

	public bool PushEnabled
	{
		get => _pushEnabled;
		set => SetProperty(ref _pushEnabled, value);
	}

	public bool EmailEnabled
	{
		get => _emailEnabled;
		set => SetProperty(ref _emailEnabled, value);
	}

	public string BuildSummary()
	{
		return $"{ReminderSummary}.";
	}

	public string ReminderTimeText => $"A las {DateTime.Today.Add(ReminderTime):h:mm tt}";
	public string ReminderSummary
	{
		get
		{
			var enabledReminders = new List<string>();
			if (NotifyThreeDaysBefore)
			{
				enabledReminders.Add("3 días antes");
			}

			if (NotifySameDay)
			{
				enabledReminders.Add("el día de vencimiento");
			}

			if (enabledReminders.Count == 0)
			{
				return "No recibirás recordatorios por ahora";
			}

			return $"Te avisaremos {string.Join(" y ", enabledReminders)} a las {DateTime.Today.Add(ReminderTime):h:mm tt}";
		}
	}

	public bool IsSaving
	{
		get => _isSaving;
		private set
		{
			SetProperty(ref _isSaving, value);
			RaisePropertyChanged(nameof(IsNotSaving));
			RaisePropertyChanged(nameof(SaveButtonText));
		}
	}

	public bool IsNotSaving => !IsSaving;
	public string SaveButtonText => IsSaving ? "Guardando..." : "Guardar preferencias";

	public async Task LoadAsync(CancellationToken cancellationToken = default)
	{
		var preferences = await _service.GetNotificationPreferencesAsync(cancellationToken);
		NotifyThreeDaysBefore = preferences.NotifyThreeDaysBefore;
		NotifySameDay = preferences.NotifySameDay;
		ReminderTime = preferences.ReminderTime;
		Timezone = preferences.Timezone;
		PushEnabled = preferences.Channels.Push;
		EmailEnabled = preferences.Channels.Email;
	}

	public async Task<string> SaveAsync(CancellationToken cancellationToken = default)
	{
		if (IsSaving)
		{
			return string.Empty;
		}

		try
		{
			IsSaving = true;
			var saved = await _service.UpdateNotificationPreferencesAsync(new NotificationPreferences
			{
				NotifyThreeDaysBefore = NotifyThreeDaysBefore,
				NotifySameDay = NotifySameDay,
				ReminderTime = ReminderTime,
				Timezone = Timezone,
				Channels = new NotificationChannels
				{
					Push = PushEnabled,
					Email = EmailEnabled
				}
			}, cancellationToken);

			NotifyThreeDaysBefore = saved.NotifyThreeDaysBefore;
			NotifySameDay = saved.NotifySameDay;
			ReminderTime = saved.ReminderTime;
			Timezone = saved.Timezone;
			PushEnabled = saved.Channels.Push;
			EmailEnabled = saved.Channels.Email;
			return BuildSummary();
		}
		finally
		{
			IsSaving = false;
		}
	}

	private void RaiseNotificationPreviewChanged()
	{
		RaisePropertyChanged(nameof(ReminderTimeText));
		RaisePropertyChanged(nameof(ReminderSummary));
	}
}
