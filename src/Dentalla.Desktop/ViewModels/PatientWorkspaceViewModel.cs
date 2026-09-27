using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Dentalla.Contracts.Clinical;
using Dentalla.Contracts.Patients;
using Dentalla.Desktop.Models;

namespace Dentalla.Desktop.ViewModels;

public partial class PatientWorkspaceViewModel : ObservableObject
{
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri("http://127.0.0.1:5080") };
    public ObservableCollection<PatientSearchItemDto> SearchResults { get; } = [];
    public ObservableCollection<PatientVisitRowViewModel> Visits { get; } = [];
    public ObservableCollection<PatientServiceRowViewModel> SelectedVisitServices { get; } = [];
    public ObservableCollection<PatientTreatmentCourseDto> TreatmentCourses { get; } = [];
    public ObservableCollection<PatientHistoricalReceiptDto> HistoricalReceipts { get; } = [];
    private IReadOnlyList<PatientServiceDto> _allServices = [];
    private IReadOnlyList<PatientVisitRowViewModel> _allVisits = [];
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string statusText = "Введите ФИО или № карты, либо откройте первые 50 пациентов.";
    [ObservableProperty] private PatientSearchItemDto? selectedPatient;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasPatient))][NotifyPropertyChangedFor(nameof(PatientHeader))][NotifyPropertyChangedFor(nameof(PatientMetaText))][NotifyPropertyChangedFor(nameof(SourceIdsText))][NotifyPropertyChangedFor(nameof(HistoricalReceiptText))][NotifyPropertyChangedFor(nameof(PatientContextSummaryText))][NotifyPropertyChangedFor(nameof(NextAppointmentText))][NotifyPropertyChangedFor(nameof(LastCompletedVisitText))][NotifyPropertyChangedFor(nameof(ServicesSummaryText))][NotifyPropertyChangedFor(nameof(TreatmentSummaryText))] private PatientWorkspaceDto? patient;
    [ObservableProperty] private PatientVisitRowViewModel? selectedVisit;
    [ObservableProperty] private bool showFulfilledVisits;
    [ObservableProperty] private bool showCancelledVisits;
    [ObservableProperty] private bool showScheduledVisits;
    [ObservableProperty] private EncounterWorkspaceDto? activeEncounter;
    [ObservableProperty] private string clinicalNoteText = string.Empty;
    public bool HasPatient => Patient is not null;
    public string PatientHeader => Patient?.FullName ?? "Пациент не выбран";
    public string PatientMetaText => Patient is null ? "Выберите пациента слева." : $"{(string.IsNullOrWhiteSpace(Patient.CardNumber) ? "карта —" : $"карта № {Patient.CardNumber}")} • {Patient.BirthDate?.ToString("dd.MM.yyyy") ?? "дата рождения —"} • посещений/записей: {Patient.Visits.Count:N0}";
    public string PatientContextSummaryText => Patient is null ? string.Empty : $"{NextAppointmentText} • {ServicesSummaryText}";
    public string NextAppointmentText { get { if (Patient is null) return "Следующая запись —"; var next = Patient.Visits.Where(x => x.StartLocal >= DateTime.Now && x.StatusCode is not "Cancelled" and not "NoShow" and not "Fulfilled").OrderBy(x => x.StartLocal).FirstOrDefault(); return next is null ? "Следующей записи нет" : $"{next.StartLocal:dd.MM.yyyy HH:mm} • {FormatDoctorName(next.DoctorName)}"; } }
    public string LastCompletedVisitText { get { if (Patient is null) return "Последний приём —"; var last = Patient.Visits.Where(x => x.StatusCode == "Fulfilled").OrderByDescending(x => x.StartLocal).FirstOrDefault(); return last is null ? "Завершённых приёмов нет" : $"Последний завершённый приём: {last.StartLocal:dd.MM.yyyy} • {FormatDoctorName(last.DoctorName)}"; } }
    public string ServicesSummaryText => Patient is null || Patient.Services.Count == 0 ? "Нормализованных услуг нет" : $"Оказанных услуг: {Patient.Services.Count:N0} • сумма: {Patient.Services.Sum(x => x.FinalAmount):N2} ₽";
    public string TreatmentSummaryText => Patient is null || Patient.TreatmentCourses.Count == 0 ? "Планов/курсов лечения нет" : $"Планов/курсов лечения: {Patient.TreatmentCourses.Count:N0} • активных: {Patient.TreatmentCourses.Count(x => !x.IsCompleted):N0}";
    public string SourceIdsText => Patient is null || Patient.SourceIds.Count == 0 ? "Legacy IDs: —" : "Legacy IDs: " + string.Join(" • ", Patient.SourceIds.Select(x => $"{x.SystemCode}:{x.ExternalId}"));
    public string HistoricalReceiptText => Patient is null || Patient.HistoricalReceipts.Count == 0 ? "Исторические поступления GREIS: —" : $"Исторические поступления GREIS: {Patient.HistoricalReceiptTotal:N2} ₽";

    public PatientWorkspaceViewModel(DesktopSessionContext session) => _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true; StatusText = "Ищу пациентов в Dentalla…";
        try { var q = Uri.EscapeDataString(SearchText?.Trim() ?? string.Empty); var rows = await _httpClient.GetFromJsonAsync<List<PatientSearchItemDto>>($"/api/patients/search?q={q}&take=50", cancellationToken) ?? []; SearchResults.Clear(); foreach (var row in rows) SearchResults.Add(row); StatusText = rows.Count == 0 ? "Пациенты не найдены. Поиск сейчас работает по ФИО и № карты." : $"Найдено: {rows.Count:N0}. Поиск по ФИО и № карты."; }
        catch (HttpRequestException) { StatusText = "Dentalla Server недоступен. Проверьте Dentalla.Api на 127.0.0.1:5080."; } catch (Exception ex) { StatusText = $"Не удалось выполнить поиск: {ex.Message}"; } finally { IsLoading = false; }
    }

    public async Task LoadSelectedPatientAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedPatient is null) return; IsLoading = true; StatusText = $"Загружаю карточку: {SelectedPatient.FullName}…";
        try { var dto = await _httpClient.GetFromJsonAsync<PatientWorkspaceDto>($"/api/patients/{SelectedPatient.Id:D}/workspace", cancellationToken) ?? throw new InvalidOperationException("Сервер вернул пустую карточку пациента."); Patient = dto; _allServices = dto.Services; _allVisits = dto.Visits.Select(x => new PatientVisitRowViewModel(x)).ToList(); TreatmentCourses.Clear(); foreach (var course in dto.TreatmentCourses) TreatmentCourses.Add(course); HistoricalReceipts.Clear(); foreach (var receipt in dto.HistoricalReceipts) HistoricalReceipts.Add(receipt); ApplyVisitFilters(); StatusText = $"Карточка загружена: {dto.Visits.Count:N0} записей/посещений, {dto.Services.Count:N0} выполненных услуг."; }
        catch (HttpRequestException) { StatusText = "Dentalla Server недоступен. Карточка пациента не загружена."; } catch (Exception ex) { StatusText = $"Не удалось загрузить карточку пациента: {ex.Message}"; } finally { IsLoading = false; }
    }

    public async Task<bool> StartEncounterAsync(CancellationToken cancellationToken = default)
    {
        if (Patient is null) return false;
        var appointment = Patient.Visits.Where(x => x.StatusCode is not "Cancelled" and not "NoShow" and not "Fulfilled").OrderBy(x => Math.Abs((x.StartLocal - DateTime.Now).TotalMinutes)).FirstOrDefault();
        if (appointment is null) { StatusText = "Для начала приёма сначала создайте запись пациента на приём."; return false; }
        using var response = await _httpClient.PostAsJsonAsync($"/api/clinical/patients/{Patient.Id:D}/encounters", new StartEncounterRequest(appointment.AppointmentId, null), cancellationToken);
        if (!response.IsSuccessStatusCode) { StatusText = $"Не удалось начать приём ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}"; return false; }
        ActiveEncounter = await response.Content.ReadFromJsonAsync<EncounterWorkspaceDto>(cancellationToken: cancellationToken); ClinicalNoteText = ActiveEncounter?.ClinicalNote.Text ?? string.Empty; StatusText = "Приём открыт. Можно заполнять дневник."; return ActiveEncounter is not null;
    }

    public async Task SaveClinicalNoteAsync(CancellationToken cancellationToken = default)
    {
        if (ActiveEncounter is null) return; using var response = await _httpClient.PutAsJsonAsync($"/api/clinical/encounters/{ActiveEncounter.Id:D}/note", new UpdateClinicalNoteRequest(ClinicalNoteText), cancellationToken); if (!response.IsSuccessStatusCode) { StatusText = $"Дневник не сохранён ({(int)response.StatusCode})."; return; } ActiveEncounter = await response.Content.ReadFromJsonAsync<EncounterWorkspaceDto>(cancellationToken: cancellationToken); StatusText = "Дневник сохранён.";
    }
    public async Task SignClinicalNoteAsync(CancellationToken cancellationToken = default) { if (ActiveEncounter is null) return; using var response = await _httpClient.PostAsync($"/api/clinical/encounters/{ActiveEncounter.Id:D}/note/sign", null, cancellationToken); if (!response.IsSuccessStatusCode) { StatusText = $"Дневник не подписан ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}"; return; } ActiveEncounter = await response.Content.ReadFromJsonAsync<EncounterWorkspaceDto>(cancellationToken: cancellationToken); StatusText = "Дневник подписан."; }
    public async Task CompleteEncounterAsync(CancellationToken cancellationToken = default) { if (ActiveEncounter is null) return; using var response = await _httpClient.PostAsync($"/api/clinical/encounters/{ActiveEncounter.Id:D}/complete", null, cancellationToken); if (!response.IsSuccessStatusCode) { StatusText = $"Приём не завершён ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}"; return; } ActiveEncounter = await response.Content.ReadFromJsonAsync<EncounterWorkspaceDto>(cancellationToken: cancellationToken); StatusText = "Приём завершён."; }

    partial void OnSelectedVisitChanged(PatientVisitRowViewModel? value) => RefreshSelectedVisitServices(); partial void OnShowFulfilledVisitsChanged(bool value) => ApplyVisitFilters(); partial void OnShowCancelledVisitsChanged(bool value) => ApplyVisitFilters(); partial void OnShowScheduledVisitsChanged(bool value) => ApplyVisitFilters();
    private void ApplyVisitFilters() { var selectedAppointmentId = SelectedVisit?.AppointmentId; var hasActiveFilter = ShowFulfilledVisits || ShowCancelledVisits || ShowScheduledVisits; IEnumerable<PatientVisitRowViewModel> rows = _allVisits; if (hasActiveFilter) rows = rows.Where(x => (ShowFulfilledVisits && x.Source.StatusCode == "Fulfilled") || (ShowCancelledVisits && x.Source.StatusCode == "Cancelled") || (ShowScheduledVisits && x.Source.StatusCode == "Scheduled")); Visits.Clear(); foreach (var row in rows) Visits.Add(row); SelectedVisit = selectedAppointmentId is null ? Visits.FirstOrDefault() : Visits.FirstOrDefault(x => x.AppointmentId == selectedAppointmentId.Value) ?? Visits.FirstOrDefault(); RefreshSelectedVisitServices(); }
    private void RefreshSelectedVisitServices() { SelectedVisitServices.Clear(); var encounterId = SelectedVisit?.EncounterId; if (encounterId is null) return; foreach (var service in _allServices.Where(x => x.EncounterId == encounterId.Value)) SelectedVisitServices.Add(new(service)); }
    private static string FormatDoctorName(string? fullName) { if (string.IsNullOrWhiteSpace(fullName)) return "врач —"; var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); if (parts.Length <= 1 || parts.Skip(1).Any(x => x.Contains('.'))) return fullName.Trim(); var initials = string.Concat(parts.Skip(1).Select(x => $"{char.ToUpperInvariant(x[0])}.")); return $"{parts[0]} {initials}"; }
}

public sealed class PatientVisitRowViewModel { public PatientVisitDto Source { get; } public Guid AppointmentId => Source.AppointmentId; public Guid? EncounterId => Source.EncounterId; public string DateText => Source.StartLocal.ToString("dd.MM.yyyy"); public string TimeText => $"{Source.StartLocal:HH:mm}–{Source.EndLocal:HH:mm}"; public string DoctorText => Source.DoctorName ?? "врач —"; public string RoomText => Source.LegacyRoomId is null ? "кабинет —" : $"кабинет/кресло #{Source.LegacyRoomId}"; public string CommentText => string.IsNullOrWhiteSpace(Source.LegacyComment) ? "" : Source.LegacyComment; public string ServicesText => Source.ServiceCount == 0 ? "услуг —" : $"услуг {Source.ServiceCount:N0} • {Source.ServicesAmount:N2} ₽"; public string StatusText => Source.StatusCode switch { "Cancelled" => "Отменён", "Confirmed" => "Подтверждён", "Arrived" => "Пришёл", "Fulfilled" => "Завершён", "NoShow" => "Неявка", _ => "Запланирован" }; public PatientVisitRowViewModel(PatientVisitDto source) => Source = source; }
public sealed class PatientServiceRowViewModel { public PatientServiceDto Source { get; } public string NameText => string.IsNullOrWhiteSpace(Source.ServiceCode) ? Source.ServiceName : $"{Source.ServiceCode} • {Source.ServiceName}"; public string MetaText { get { var parts = new List<string> { $"× {Source.Quantity:N2}" }; if (!string.IsNullOrWhiteSpace(Source.Tooth)) parts.Add($"зуб {Source.Tooth}"); if (!string.IsNullOrWhiteSpace(Source.DoctorName)) parts.Add(Source.DoctorName); return string.Join(" • ", parts); } } public string AmountText => $"{Source.FinalAmount:N2} ₽"; public PatientServiceRowViewModel(PatientServiceDto source) => Source = source; }
