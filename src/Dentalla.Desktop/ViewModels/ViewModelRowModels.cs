using Dentalla.Contracts.Patients;
using Dentalla.Contracts.Workspaces;

namespace Dentalla.Desktop.ViewModels;

public sealed record RoleContextOption(string Code, string Name);

public sealed class AppointmentRowViewModel
{
    public Guid Id { get; }
    public Guid PatientId { get; }
    public string PatientName { get; }
    public string CardNumber { get; }
    public string DoctorName { get; }
    public DateTime StartLocal { get; }
    public DateTime EndLocal { get; }
    public string StatusCode { get; }
    public int? LegacyRoomId { get; }
    public string TimeText => StartLocal.ToString("HH:mm");
    public string TimeRangeText => $"{StartLocal:HH:mm}–{EndLocal:HH:mm}";
    public string CardText => string.IsNullOrWhiteSpace(CardNumber) ? "карта —" : $"№ {CardNumber}";
    public string RoomText => LegacyRoomId is null ? "кабинет —" : $"кабинет/кресло #{LegacyRoomId}";
    public string StatusText => StatusCode switch
    {
        "Cancelled" => "Отменён",
        "Confirmed" => "Подтверждён",
        "Arrived" => "Пришёл",
        "Fulfilled" => "Выполнен",
        "NoShow" => "Неявка",
        _ => "Запланирован"
    };

    public AppointmentRowViewModel(AppointmentListItemDto item)
    {
        Id = item.Id;
        PatientId = item.PatientId;
        PatientName = item.PatientName;
        CardNumber = item.CardNumber;
        DoctorName = item.DoctorName ?? "врач —";
        StartLocal = item.StartLocal;
        EndLocal = item.EndLocal;
        StatusCode = item.StatusCode;
        LegacyRoomId = item.LegacyRoomId;
    }
}

public sealed class PatientVisitRowViewModel
{
    public PatientVisitDto Source { get; }
    public Guid AppointmentId => Source.AppointmentId;
    public Guid? EncounterId => Source.EncounterId;
    public string DateText => Source.StartLocal.ToString("dd.MM.yyyy");
    public string TimeText => $"{Source.StartLocal:HH:mm}–{Source.EndLocal:HH:mm}";
    public string DoctorText => Source.DoctorName ?? "врач —";
    public string RoomText => Source.LegacyRoomId is null ? "кабинет —" : $"кабинет/кресло #{Source.LegacyRoomId}";
    public string CommentText => string.IsNullOrWhiteSpace(Source.LegacyComment) ? "" : Source.LegacyComment;
    public string ServicesText => Source.ServiceCount == 0 ? "услуг —" : $"услуг {Source.ServiceCount:N0} • {Source.ServicesAmount:N2} ₽";
    public string StatusText => Source.StatusCode switch
    {
        "Cancelled" => "Отменён",
        "Confirmed" => "Подтверждён",
        "Arrived" => "Пришёл",
        "Fulfilled" => "Завершён",
        "NoShow" => "Неявка",
        _ => "Запланирован"
    };
    public PatientVisitRowViewModel(PatientVisitDto source) => Source = source;
}

public sealed class PatientServiceRowViewModel
{
    public PatientServiceDto Source { get; }
    public string NameText => string.IsNullOrWhiteSpace(Source.ServiceCode) ? Source.ServiceName : $"{Source.ServiceCode} • {Source.ServiceName}";
    public string MetaText
    {
        get
        {
            var parts = new List<string> { $"× {Source.Quantity:N2}" };
            if (!string.IsNullOrWhiteSpace(Source.Tooth)) parts.Add($"зуб {Source.Tooth}");
            if (!string.IsNullOrWhiteSpace(Source.DoctorName)) parts.Add(Source.DoctorName);
            return string.Join(" • ", parts);
        }
    }
    public string AmountText => $"{Source.FinalAmount:N2} ₽";
    public PatientServiceRowViewModel(PatientServiceDto source) => Source = source;
}
