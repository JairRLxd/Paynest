using Paynest.Models;

namespace Paynest.Features.Client.Api;

public static class DebtDtoMapper
{
    public static DebtGroup ToModel(this DebtGroupDto dto)
    {
        return new DebtGroup
        {
            Id = dto.Id,
            Name = dto.Name,
            FreelancerName = dto.FreelancerName,
            TotalAmount = dto.TotalAmount,
            PendingAmount = dto.PendingAmount,
            Frequency = ParseFrequency(dto.Frequency)
        };
    }

    public static Installment ToModel(this InstallmentDto dto)
    {
        return new Installment
        {
            Id = dto.Id,
            GroupId = dto.GroupId,
            Title = dto.Title,
            DueDate = dto.DueDate,
            Amount = dto.Amount,
            Status = ParseStatus(dto.Status)
        };
    }

    public static Receipt ToModel(this ReceiptDto dto)
    {
        return new Receipt
        {
            Id = dto.Id,
            InstallmentId = dto.InstallmentId,
            GroupId = dto.GroupId,
            Title = dto.Title,
            Amount = dto.Amount,
            PaidAt = dto.PaidAt,
            Folio = dto.Folio,
            FileUrl = dto.FileUrl
        };
    }

    public static PayInstallmentResult ToModel(this PayInstallmentResponseDto dto)
    {
        return new PayInstallmentResult
        {
            Success = dto.Success,
            WalletBalance = dto.Wallet?.Balance,
            WalletCurrency = dto.Wallet?.Currency ?? dto.Payment?.Currency ?? dto.Movement?.Currency ?? "MXN",
            PaymentId = dto.Payment?.Id,
            PaymentStatus = dto.Payment?.Status,
            PaymentMethod = dto.Payment?.Method,
            ReceiptId = dto.Receipt?.Id,
            ReceiptFolio = dto.Receipt?.Folio,
            ReceiptFileUrl = dto.Receipt?.FileUrl,
            MovementId = dto.Movement?.Id,
            MovementAmount = dto.Movement?.Amount
        };
    }

    public static NotificationPreferences ToModel(this NotificationPreferencesDto dto)
    {
        return new NotificationPreferences
        {
            NotifyThreeDaysBefore = dto.NotifyThreeDaysBefore,
            NotifySameDay = dto.NotifySameDay,
            ReminderTime = ParseReminderTime(dto.ReminderTime),
            Timezone = dto.Timezone,
            Channels = new NotificationChannels
            {
                Push = dto.Channels.Push,
                Email = dto.Channels.Email
            }
        };
    }

    public static NotificationPreferencesDto ToDto(this NotificationPreferences model)
    {
        return new NotificationPreferencesDto
        {
            NotifyThreeDaysBefore = model.NotifyThreeDaysBefore,
            NotifySameDay = model.NotifySameDay,
            ReminderTime = $"{model.ReminderTime.Hours:00}:{model.ReminderTime.Minutes:00}",
            Timezone = model.Timezone,
            Channels = new NotificationChannelsDto
            {
                Push = model.Channels.Push,
                Email = model.Channels.Email
            }
        };
    }

    private static PaymentFrequency ParseFrequency(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "weekly" or "semanal" => PaymentFrequency.Weekly,
            "biweekly" or "quincenal" => PaymentFrequency.Biweekly,
            _ => PaymentFrequency.Monthly
        };
    }

    private static InstallmentStatus ParseStatus(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "paid" or "pagado" => InstallmentStatus.Paid,
            "overdue" or "vencido" => InstallmentStatus.Overdue,
            _ => InstallmentStatus.Pending
        };
    }

    private static TimeSpan ParseReminderTime(string value)
    {
        return TimeSpan.TryParse(value, out var parsed)
            ? parsed
            : new TimeSpan(9, 0, 0);
    }
}
