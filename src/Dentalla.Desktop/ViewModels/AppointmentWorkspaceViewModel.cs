using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Dentalla.Contracts.Scheduling;
using Dentalla.Contracts.Security;
using Dentalla.Contracts.Workspaces;
using Dentalla.Desktop.Models;

namespace Dentalla.Desktop.ViewModels;

public partial class AppointmentWorkspaceViewModel : ObservableObject
{
    private readonly HttpClient _httpClient;

    public Guid PatientId { get; }
    public string PatientName { get; }
    public string CardNumber { get; }
    public string PatientCaption => string.IsNullOrWhiteSpace(CardNumber)
        ? PatientName
        : $"{PatientName} • карта № {CardNumber}";

    public ObservableCollection<AppointmentStaffOptionDto> Doctors { get; } = [];
    public ObservableCollection<AppointmentRoomOption> Rooms { get; } = [];
    public ObservableCollection<AppointmentScheduleRow> DayAppointments { get; } = [];
    public IReadOnlyList<int> Durations { get; } = [15, 30, 45, 60, 90, 120];

    [ObservableProperty]
    private AppointmentStaffOptionDto? selectedDoctor;

    [ObservableProperty]
    private AppointmentRoomOption? selectedRoom;

    [ObservableProperty]
    private DateTimeOffset? selectedDate = new(DateTime.Today);

    [ObservableProperty]
    private string startTimeText = "09:00";

    [ObservableProperty]
    private int selectedDuration = 60;

    [ObservableProperty]
    private AppointmentScheduleRow? selectedExistingAppointment;

    [ObservableProperty]
    private bool canManageAppointments;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusText = "Загрузка расписания…";

    public bool IsEditing => SelectedExistingAppointment?.PatientId == PatientId;
    public string PrimaryActionText => IsEditing ? "Сохранить изменения" : "Создать запись";
    public bool CanSubmit => CanManageAppointments && !IsBusy && SelectedDoctor is not null && SelectedDate is not null;
    public bool CanChangeSelectedAppointment => CanManageAppointments && IsEditing && !IsBusy;

    public AppointmentWorkspaceViewModel(Guid patientId, string patientName, string? cardNumber)
    {
        PatientId = patientId;
        PatientName = patientName;
        CardNumber = cardNumber ?? string.Empty;

        _httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5080") };
        var token = DesktopSessionStore.AccessToken;
        if (!string.IsNullOrWhiteSpace(token))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        StatusText = "Получаю врачей, кабинеты и права…";
        try
        {
            var options = await _httpClient.GetFromJsonAsync<AppointmentEditorOptionsDto>(
                "/api/scheduling/editor-options",
                cancellationToken) ?? throw new InvalidOperationException("Сервер не вернул параметры записи.");

            Doctors.Clear();
            foreach (var doctor in options.Doctors)
                Doctors.Add(doctor);

            Rooms.Clear();
            Rooms.Add(new AppointmentRoomOption(null, "Не выбран"));
            foreach (var room in options.Rooms)
                Rooms.Add(new AppointmentRoomOption(room, $"Кабинет/кресло #{room}"));

            SelectedRoom ??= Rooms.FirstOrDefault();
            SelectedDoctor ??= Doctors.FirstOrDefault();

            var permissions = await _httpClient.GetFromJsonAsync<List<EffectivePermissionDto>>(
                                  "/api/auth/me/permissions",
                                  cancellationToken) ?? [];
            CanManageAppointments = permissions.Any(x => x.PermissionCode == "Appointment.Manage" && x.IsAllowed);

            await RefreshScheduleAsync(cancellationToken);
            StatusText = CanManageAppointments
                ? "Можно создавать, переносить, подтверждать и отменять записи."
                : "Расписание доступно для просмотра. Изменение записи не разрешено вашей текущей учётной записи.";
        }
        catch (HttpRequestException ex)
        {
            StatusText = $"Dentalla Server недоступен: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusText = $"Не удалось открыть запись на приём: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseActionState();
        }
    }

    public async Task RefreshScheduleAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDate is null)
            return;

        var date = DateOnly.FromDateTime(SelectedDate.Value.Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var schedule = await _httpClient.GetFromJsonAsync<DayScheduleDto>(
            $"/api/workspaces/day-schedule?date={Uri.EscapeDataString(date)}",
            cancellationToken);

        DayAppointments.Clear();
        if (schedule is null)
            return;

        IEnumerable<AppointmentListItemDto> items = schedule.Appointments;
        if (SelectedDoctor is not null)
            items = items.Where(x => x.StaffProfileId == SelectedDoctor.Id);

        foreach (var item in items.OrderBy(x => x.StartLocal))
            DayAppointments.Add(new AppointmentScheduleRow(item, PatientId));
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (!CanManageAppointments)
        {
            StatusText = "Нет разрешения Appointment.Manage.";
            return;
        }
        if (!TryBuildInterval(out var startLocal, out var endLocal) || SelectedDoctor is null)
            return;

        IsBusy = true;
        RaiseActionState();
        try
        {
            HttpResponseMessage response;
            if (IsEditing && SelectedExistingAppointment is not null)
            {
                response = await _httpClient.PutAsJsonAsync(
                    $"/api/scheduling/appointments/{SelectedExistingAppointment.Id:D}",
                    new UpdateAppointmentRequest(
                        SelectedDoctor.Id,
                        startLocal,
                        endLocal,
                        SelectedRoom?.Id),
                    cancellationToken);
            }
            else
            {
                response = await _httpClient.PostAsJsonAsync(
                    "/api/scheduling/appointments",
                    new CreateAppointmentRequest(
                        PatientId,
                        SelectedDoctor.Id,
                        startLocal,
                        endLocal,
                        SelectedRoom?.Id),
                    cancellationToken);
            }

            if (!response.IsSuccessStatusCode)
            {
                StatusText = await ReadErrorAsync(response, cancellationToken);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<AppointmentCommandResultDto>(cancellationToken: cancellationToken);
            StatusText = result is null
                ? "Запись сохранена."
                : $"Запись сохранена: {result.StartLocal:dd.MM.yyyy HH:mm}–{result.EndLocal:HH:mm}.";

            SelectedExistingAppointment = null;
            await RefreshScheduleAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            StatusText = $"Не удалось сохранить запись: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseActionState();
        }
    }

    public async Task ConfirmAsync(CancellationToken cancellationToken = default)
        => await ChangeStatusAsync("confirm", "Запись подтверждена.", cancellationToken);

    public async Task CancelAsync(CancellationToken cancellationToken = default)
        => await ChangeStatusAsync("cancel", "Запись отменена. Данные сохранены в истории; физическое удаление не выполняется.", cancellationToken);

    public void StartNew()
    {
        SelectedExistingAppointment = null;
        StatusText = CanManageAppointments
            ? "Новая запись. Выберите врача, дату, время и длительность."
            : "Новая запись недоступна без Appointment.Manage.";
        RaiseActionState();
    }

    partial void OnSelectedDoctorChanged(AppointmentStaffOptionDto? value)
    {
        RaiseActionState();
    }

    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        RaiseActionState();
    }

    partial void OnCanManageAppointmentsChanged(bool value) => RaiseActionState();
    partial void OnIsBusyChanged(bool value) => RaiseActionState();

    partial void OnSelectedExistingAppointmentChanged(AppointmentScheduleRow? value)
    {
        if (value?.PatientId == PatientId)
        {
            SelectedDate = new DateTimeOffset(value.StartLocal.Date);
            StartTimeText = value.StartLocal.ToString("HH:mm");
            SelectedDuration = Math.Max(15, (int)(value.EndLocal - value.StartLocal).TotalMinutes);
            SelectedDoctor = Doctors.FirstOrDefault(x => x.Id == value.StaffProfileId) ?? SelectedDoctor;
            SelectedRoom = Rooms.FirstOrDefault(x => x.Id == value.RoomId) ?? Rooms.FirstOrDefault();
            StatusText = "Редактируется выбранная запись этого пациента.";
        }
        else if (value is not null)
        {
            StatusText = "Интервал занят другим пациентом. Его запись показана только как занятость расписания.";
        }

        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(PrimaryActionText));
        RaiseActionState();
    }

    private async Task ChangeStatusAsync(string action, string successText, CancellationToken cancellationToken)
    {
        if (!CanChangeSelectedAppointment || SelectedExistingAppointment is null)
            return;

        IsBusy = true;
        try
        {
            using var response = await _httpClient.PostAsync(
                $"/api/scheduling/appointments/{SelectedExistingAppointment.Id:D}/{action}",
                content: null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                StatusText = await ReadErrorAsync(response, cancellationToken);
                return;
            }

            StatusText = successText;
            SelectedExistingAppointment = null;
            await RefreshScheduleAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            StatusText = $"Операция не выполнена: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseActionState();
        }
    }

    private bool TryBuildInterval(out DateTime startLocal, out DateTime endLocal)
    {
        startLocal = default;
        endLocal = default;

        if (SelectedDate is null)
        {
            StatusText = "Выберите дату.";
            return false;
        }

        if (!TimeOnly.TryParseExact(StartTimeText.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
        {
            StatusText = "Время нужно указать в формате ЧЧ:ММ, например 14:30.";
            return false;
        }

        var date = DateOnly.FromDateTime(SelectedDate.Value.Date);
        startLocal = date.ToDateTime(startTime);
        endLocal = startLocal.AddMinutes(Math.Max(5, SelectedDuration));
        return true;
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(body)
            ? $"Сервер отклонил операцию ({(int)response.StatusCode})."
            : $"Сервер отклонил операцию ({(int)response.StatusCode}): {body}";
    }

    private void RaiseActionState()
    {
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanChangeSelectedAppointment));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(PrimaryActionText));
    }
}

public sealed record AppointmentRoomOption(int? Id, string Name);

public sealed class AppointmentScheduleRow
{
    public Guid Id { get; }
    public Guid PatientId { get; }
    public Guid? StaffProfileId { get; }
    public int? RoomId { get; }
    public DateTime StartLocal { get; }
    public DateTime EndLocal { get; }
    public string PatientName { get; }
    public string DoctorName { get; }
    public string StatusCode { get; }
    public bool IsCurrentPatient { get; }

    public string TimeText => $"{StartLocal:HH:mm}–{EndLocal:HH:mm}";
    public string OccupancyText => IsCurrentPatient ? PatientName : "Занято";
    public string DoctorText => string.IsNullOrWhiteSpace(DoctorName) ? "врач —" : DoctorName;
    public string RoomText => RoomId is null ? "" : $"кресло #{RoomId}";
    public string StatusText => StatusCode switch
    {
        "Cancelled" => "Отменён",
        "Confirmed" => "Подтверждён",
        "Arrived" => "Пришёл",
        "Fulfilled" => "Завершён",
        "NoShow" => "Неявка",
        _ => "Запланирован"
    };

    public AppointmentScheduleRow(AppointmentListItemDto item, Guid currentPatientId)
    {
        Id = item.Id;
        PatientId = item.PatientId;
        StaffProfileId = item.StaffProfileId;
        RoomId = item.LegacyRoomId;
        StartLocal = item.StartLocal;
        EndLocal = item.EndLocal;
        PatientName = item.PatientName;
        DoctorName = item.DoctorName ?? string.Empty;
        StatusCode = item.StatusCode;
        IsCurrentPatient = item.PatientId == currentPatientId;
    }
}
