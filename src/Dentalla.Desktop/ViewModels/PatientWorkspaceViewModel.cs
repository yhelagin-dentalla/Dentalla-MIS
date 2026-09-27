using System.Collections.ObjectModel;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Dentalla.Contracts.Patients;

namespace Dentalla.Desktop.ViewModels;

public partial class PatientWorkspaceViewModel : ObservableObject
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://127.0.0.1:5080")
    };

    public ObservableCollection<PatientSearchItemDto> SearchResults { get; } = [];
    public ObservableCollection<PatientVisitRowViewModel> Visits { get; } = [];
    public ObservableCollection<PatientServiceRowViewModel> SelectedVisitServices { get; } = [];
    public ObservableCollection<PatientTreatmentCourseDto> TreatmentCourses { get; } = [];
    public ObservableCollection<PatientHistoricalReceiptDto> HistoricalReceipts { get; } = [];

    private IReadOnlyList<PatientServiceDto> _allServices = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusText = "Введите ФИО или № карты, либо откройте первые 50 пациентов.";

    [ObservableProperty]
    private PatientSearchItemDto? selectedPatient;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPatient))]
    [NotifyPropertyChangedFor(nameof(PatientHeader))]
    [NotifyPropertyChangedFor(nameof(PatientMetaText))]
    [NotifyPropertyChangedFor(nameof(SourceIdsText))]
    [NotifyPropertyChangedFor(nameof(HistoricalReceiptText))]
    private PatientWorkspaceDto? patient;

    [ObservableProperty]
    private PatientVisitRowViewModel? selectedVisit;

    public bool HasPatient => Patient is not null;

    public string PatientHeader => Patient?.FullName ?? "Пациент не выбран";

    public string PatientMetaText
    {
        get
        {
            if (Patient is null)
                return "Выберите пациента слева.";

            var birth = Patient.BirthDate?.ToString("dd.MM.yyyy") ?? "дата рождения —";
            var card = string.IsNullOrWhiteSpace(Patient.CardNumber) ? "карта —" : $"карта № {Patient.CardNumber}";
            return $"{card} • {birth} • посещений/записей: {Patient.Visits.Count:N0}";
        }
    }

    public string SourceIdsText
    {
        get
        {
            if (Patient is null || Patient.SourceIds.Count == 0)
                return "Legacy IDs: —";

            return "Legacy IDs: " + string.Join(" • ", Patient.SourceIds.Select(x => $"{x.SystemCode}:{x.ExternalId}"));
        }
    }

    public string HistoricalReceiptText
    {
        get
        {
            if (Patient is null || Patient.HistoricalReceipts.Count == 0)
                return "Исторические поступления GREIS: —";

            return $"Исторические поступления GREIS: {Patient.HistoricalReceiptTotal:N2} ₽";
        }
    }

    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        StatusText = "Ищу пациентов в Dentalla…";

        try
        {
            var q = Uri.EscapeDataString(SearchText?.Trim() ?? string.Empty);
            var rows = await _httpClient.GetFromJsonAsync<List<PatientSearchItemDto>>(
                           $"/api/patients/search?q={q}&take=50",
                           cancellationToken)
                       ?? [];

            SearchResults.Clear();
            foreach (var row in rows)
                SearchResults.Add(row);

            StatusText = rows.Count == 0
                ? "Пациенты не найдены. Поиск сейчас работает по ФИО и № карты."
                : $"Найдено: {rows.Count:N0}. Поиск по ФИО и № карты.";
        }
        catch (HttpRequestException)
        {
            StatusText = "Dentalla Server недоступен. Проверьте Dentalla.Api на 127.0.0.1:5080.";
        }
        catch (Exception ex)
        {
            StatusText = $"Не удалось выполнить поиск: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadSelectedPatientAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedPatient is null)
            return;

        IsLoading = true;
        StatusText = $"Загружаю карточку: {SelectedPatient.FullName}…";

        try
        {
            var dto = await _httpClient.GetFromJsonAsync<PatientWorkspaceDto>(
                $"/api/patients/{SelectedPatient.Id:D}/workspace",
                cancellationToken);

            if (dto is null)
                throw new InvalidOperationException("Сервер вернул пустую карточку пациента.");

            Patient = dto;
            _allServices = dto.Services;

            Visits.Clear();
            foreach (var visit in dto.Visits)
                Visits.Add(new PatientVisitRowViewModel(visit));

            TreatmentCourses.Clear();
            foreach (var course in dto.TreatmentCourses)
                TreatmentCourses.Add(course);

            HistoricalReceipts.Clear();
            foreach (var receipt in dto.HistoricalReceipts)
                HistoricalReceipts.Add(receipt);

            SelectedVisit = Visits.FirstOrDefault();
            RefreshSelectedVisitServices();

            StatusText = $"Карточка загружена: {dto.Visits.Count:N0} записей/посещений, {dto.Services.Count:N0} выполненных услуг.";
        }
        catch (HttpRequestException)
        {
            StatusText = "Dentalla Server недоступен. Карточка пациента не загружена.";
        }
        catch (Exception ex)
        {
            StatusText = $"Не удалось загрузить карточку пациента: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedVisitChanged(PatientVisitRowViewModel? value)
        => RefreshSelectedVisitServices();

    private void RefreshSelectedVisitServices()
    {
        SelectedVisitServices.Clear();

        var encounterId = SelectedVisit?.EncounterId;
        if (encounterId is null)
            return;

        foreach (var service in _allServices.Where(x => x.EncounterId == encounterId.Value))
            SelectedVisitServices.Add(new PatientServiceRowViewModel(service));
    }
}

public sealed class PatientVisitRowViewModel
{
    public PatientVisitDto Source { get; }

    public Guid AppointmentId => Source.AppointmentId;
    public Guid? EncounterId => Source.EncounterId;
    public string DateText => Source.StartLocal.ToString("dd.MM.yyyy");
    public string TimeText => $"{Source.StartLocal:HH:mm}–{Source.EndLocal:HH:mm}";
    public string DoctorText => FormatDoctorName(Source.DoctorName);
    public string RoomText => Source.LegacyRoomId is null ? "кабинет —" : $"кабинет/кресло #{Source.LegacyRoomId}";
    public string CommentText => string.IsNullOrWhiteSpace(Source.LegacyComment) ? "" : Source.LegacyComment;
    public string ServicesText => Source.ServiceCount == 0
        ? "услуг —"
        : $"услуг {Source.ServiceCount:N0} • {Source.ServicesAmount:N2} ₽";

    public string StatusText => Source.StatusCode switch
    {
        "Cancelled" => "Отменён",
        "Confirmed" => "Подтверждён",
        "Arrived" => "Пришёл",
        "Fulfilled" => "Выполнен",
        "NoShow" => "Неявка",
        _ => "Запланирован"
    };

    public PatientVisitRowViewModel(PatientVisitDto source) => Source = source;

    private static string FormatDoctorName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "врач —";

        var parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length <= 1 || parts.Skip(1).Any(x => x.Contains('.')))
            return fullName.Trim();

        var initials = string.Concat(parts.Skip(1).Select(x => $"{char.ToUpperInvariant(x[0])}."));
        return $"{parts[0]} {initials}";
    }
}

public sealed class PatientServiceRowViewModel
{
    public PatientServiceDto Source { get; }

    public string NameText => string.IsNullOrWhiteSpace(Source.ServiceCode)
        ? Source.ServiceName
        : $"{Source.ServiceCode} • {Source.ServiceName}";

    public string MetaText
    {
        get
        {
            var parts = new List<string> { $"× {Source.Quantity:N2}" };
            if (!string.IsNullOrWhiteSpace(Source.Tooth))
                parts.Add($"зуб {Source.Tooth}");
            if (!string.IsNullOrWhiteSpace(Source.DoctorName))
                parts.Add(Source.DoctorName);
            return string.Join(" • ", parts);
        }
    }

    public string AmountText => $"{Source.FinalAmount:N2} ₽";

    public PatientServiceRowViewModel(PatientServiceDto source) => Source = source;
}
