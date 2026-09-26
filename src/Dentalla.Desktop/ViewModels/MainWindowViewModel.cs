using System.Collections.ObjectModel;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Dentalla.Contracts.Security;
using Dentalla.Contracts.Workspaces;
using Dentalla.Desktop.Models;

namespace Dentalla.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://127.0.0.1:5080")
    };

    private readonly Dictionary<string, Guid?> _lastSelectedAppointmentByRole = new(StringComparer.Ordinal);

    public string CurrentEmployeeName { get; }
    public string CurrentEmployeeId { get; }
    public string PrimaryRoleCode { get; }
    public string PrimaryRoleName { get; }

    [ObservableProperty]
    private string currentRoleCode;

    [ObservableProperty]
    private string currentRoleName;

    public ObservableCollection<RoleContextOption> RoleContexts { get; } = [];

    [ObservableProperty]
    private RoleContextOption? selectedRoleContext;

    [ObservableProperty]
    private bool isRoleContextSwitching;

    [ObservableProperty]
    private string roleContextStatusText = string.Empty;

    public ObservableCollection<AppointmentRowViewModel> TodayAppointments { get; } = [];

    [ObservableProperty]
    private AppointmentRowViewModel? selectedAppointment;

    [ObservableProperty]
    private bool isDataLoading;

    [ObservableProperty]
    private string dataStatusText = "Загрузка данных…";

    [ObservableProperty]
    private int normalizedPatientCount;

    [ObservableProperty]
    private int activeStaffCount;

    public bool HasAppointments => TodayAppointments.Count > 0;
    public bool HasNoAppointments => !HasAppointments && !IsDataLoading;

    public bool IsDoctor => CurrentRoleCode == "Doctor";
    public bool IsAdministrator => CurrentRoleCode == "Administrator";
    public bool IsChiefMedicalOfficer => CurrentRoleCode == "ChiefMedicalOfficer";
    public bool IsMarketer => CurrentRoleCode == "Marketer";
    public bool IsDirector => CurrentRoleCode == "Director";

    public bool CanSwitchRoleContext => PrimaryRoleCode == "Director";
    public bool IsDirectorActingInAnotherRole => CanSwitchRoleContext && !IsDirector;

    public string IdentityRoleText => IsDirectorActingInAnotherRole
        ? $"{PrimaryRoleName} → {CurrentRoleName}"
        : CurrentRoleName;

    public string RoleContextCaption => IsDirectorActingInAnotherRole
        ? $"Контекст: {CurrentRoleName} • права Director сохраняются"
        : "Рабочий контекст Director";

    public string WorkspaceTitle => CurrentRoleCode switch
    {
        "Doctor" => "Мой день",
        "Administrator" => "Расписание",
        "ChiefMedicalOfficer" => "Качество",
        "Marketer" => "Маркетинг",
        "Director" => "Управление",
        _ => "Dentalla MIS"
    };

    public string WorkspaceSubtitle => CurrentRoleCode switch
    {
        "Doctor" => CanSwitchRoleContext
            ? "Контрольный доступ к расписанию и клиническому workspace; профессиональные действия проверяются отдельно"
            : "Расписание врача и быстрый лечебный приём",
        "Administrator" => "Расписание, очередь визита и работа с пациентами",
        "ChiefMedicalOfficer" => "Контроль документации, экспертиза и клинические стандарты",
        "Marketer" => "Рабочая очередь, воронка, источники и кампании",
        "Director" => "Dashboard, финансы, операции и отчёты",
        _ => string.Empty
    };

    public MainWindowViewModel(DesktopSessionContext session)
    {
        CurrentEmployeeName = session.EmployeeName;
        CurrentEmployeeId = session.EmployeeId;
        PrimaryRoleCode = session.RoleCode;
        PrimaryRoleName = session.RoleName;
        currentRoleCode = session.RoleCode;
        currentRoleName = session.RoleName;

        if (CanSwitchRoleContext)
        {
            RoleContexts.Add(new RoleContextOption("Director", "Директор"));
            RoleContexts.Add(new RoleContextOption("Administrator", "Администратор"));
            RoleContexts.Add(new RoleContextOption("ChiefMedicalOfficer", "Главный врач"));
            RoleContexts.Add(new RoleContextOption("Marketer", "Маркетолог"));
            RoleContexts.Add(new RoleContextOption("Doctor", "Врач"));

            SelectedRoleContext = RoleContexts.First(x => x.Code == CurrentRoleCode);
            RoleContextStatusText = "Можно мгновенно перейти в рабочее пространство другой роли без повторного входа.";
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsDataLoading = true;
        DataStatusText = "Получаю нормализованные данные Dentalla Server…";
        TodayAppointments.Clear();
        SelectedAppointment = null;
        RaiseAppointmentState();

        try
        {
            var date = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var uri = $"/api/workspaces/day-schedule?date={Uri.EscapeDataString(date)}";

            // Ordinary Doctor sees own day. Director in Doctor context has clinic-wide
            // supervisory visibility and is not impersonating a particular doctor.
            if (IsDoctor && !CanSwitchRoleContext && Guid.TryParse(CurrentEmployeeId, out var staffId))
                uri += $"&staffProfileId={staffId:D}";

            var schedule = await _httpClient.GetFromJsonAsync<DayScheduleDto>(uri, cancellationToken)
                           ?? throw new InvalidOperationException("Сервер вернул пустое расписание.");

            NormalizedPatientCount = schedule.TotalPatients;
            ActiveStaffCount = schedule.ActiveStaff;

            foreach (var item in schedule.Appointments)
                TodayAppointments.Add(new AppointmentRowViewModel(item));

            if (_lastSelectedAppointmentByRole.TryGetValue(CurrentRoleCode, out var rememberedId) && rememberedId is not null)
                SelectedAppointment = TodayAppointments.FirstOrDefault(x => x.Id == rememberedId.Value);

            SelectedAppointment ??= TodayAppointments.FirstOrDefault();
            DataStatusText = TodayAppointments.Count == 0
                ? $"На {DateTime.Today:dd.MM.yyyy} записей нет. Пациентов в новой БД: {NormalizedPatientCount:N0}."
                : $"Записей на сегодня: {TodayAppointments.Count:N0}. Пациентов в новой БД: {NormalizedPatientCount:N0}.";
        }
        catch (HttpRequestException)
        {
            DataStatusText = "Dentalla Server недоступен. Проверьте, что Dentalla.Api запущен на 127.0.0.1:5080.";
        }
        catch (Exception ex)
        {
            DataStatusText = $"Не удалось загрузить рабочие данные: {ex.Message}";
        }
        finally
        {
            IsDataLoading = false;
            RaiseAppointmentState();
        }
    }

    public async Task SwitchRoleContextAsync(RoleContextOption target, CancellationToken cancellationToken = default)
    {
        if (!CanSwitchRoleContext || IsRoleContextSwitching)
            return;

        if (target.Code == CurrentRoleCode)
        {
            SelectedRoleContext = target;
            return;
        }

        if (!Guid.TryParse(CurrentEmployeeId, out var staffProfileId))
        {
            RoleContextStatusText = "Невозможно определить StaffProfile для переключения рабочего контекста.";
            SelectedRoleContext = RoleContexts.FirstOrDefault(x => x.Code == CurrentRoleCode);
            return;
        }

        _lastSelectedAppointmentByRole[CurrentRoleCode] = SelectedAppointment?.Id;
        var previousCode = CurrentRoleCode;
        var previousName = CurrentRoleName;
        var previousOption = RoleContexts.FirstOrDefault(x => x.Code == previousCode);

        IsRoleContextSwitching = true;
        RoleContextStatusText = $"Переключаю в режим «{target.Name}»…";

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/auth/dev-role-context-switch",
                new DevRoleContextSwitchRequest(staffProfileId, previousCode, target.Code),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                RoleContextStatusText = $"Сервер отклонил переключение ({(int)response.StatusCode}). Рабочий контекст не изменён.";
                SelectedRoleContext = previousOption;
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<RoleContextSwitchResponse>(cancellationToken: cancellationToken);
            if (result is null || !result.Allowed)
            {
                RoleContextStatusText = result?.Message ?? "Сервер не подтвердил переключение рабочего контекста.";
                SelectedRoleContext = previousOption;
                return;
            }

            CurrentRoleCode = target.Code;
            CurrentRoleName = target.Name;
            SelectedRoleContext = target;
            RoleContextStatusText = IsDirector
                ? "Возвращено рабочее пространство Director."
                : $"Активен контекст «{target.Name}». Identity остаётся Director; клинические допуски проверяются отдельно.";

            await LoadAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            CurrentRoleCode = previousCode;
            CurrentRoleName = previousName;
            SelectedRoleContext = previousOption;
            RoleContextStatusText = "Dentalla Server недоступен. Переключение отменено.";
        }
        catch (Exception ex)
        {
            CurrentRoleCode = previousCode;
            CurrentRoleName = previousName;
            SelectedRoleContext = previousOption;
            RoleContextStatusText = $"Не удалось переключить рабочий контекст: {ex.Message}";
        }
        finally
        {
            IsRoleContextSwitching = false;
        }
    }

    public Task ReturnToDirectorAsync(CancellationToken cancellationToken = default)
    {
        var director = RoleContexts.FirstOrDefault(x => x.Code == "Director");
        return director is null ? Task.CompletedTask : SwitchRoleContextAsync(director, cancellationToken);
    }

    partial void OnSelectedAppointmentChanged(AppointmentRowViewModel? value)
    {
        _lastSelectedAppointmentByRole[CurrentRoleCode] = value?.Id;
        RaiseAppointmentState();
    }

    partial void OnCurrentRoleCodeChanged(string value)
    {
        OnPropertyChanged(nameof(IsDoctor));
        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(IsChiefMedicalOfficer));
        OnPropertyChanged(nameof(IsMarketer));
        OnPropertyChanged(nameof(IsDirector));
        OnPropertyChanged(nameof(IsDirectorActingInAnotherRole));
        OnPropertyChanged(nameof(IdentityRoleText));
        OnPropertyChanged(nameof(RoleContextCaption));
        OnPropertyChanged(nameof(WorkspaceTitle));
        OnPropertyChanged(nameof(WorkspaceSubtitle));
    }

    partial void OnCurrentRoleNameChanged(string value)
    {
        OnPropertyChanged(nameof(IdentityRoleText));
        OnPropertyChanged(nameof(RoleContextCaption));
        OnPropertyChanged(nameof(WorkspaceTitle));
        OnPropertyChanged(nameof(WorkspaceSubtitle));
    }

    private void RaiseAppointmentState()
    {
        OnPropertyChanged(nameof(HasAppointments));
        OnPropertyChanged(nameof(HasNoAppointments));
    }
}

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
