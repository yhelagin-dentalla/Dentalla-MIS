using System.Collections.ObjectModel;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Dentalla.Contracts.Security;
using Dentalla.Desktop.Models;

namespace Dentalla.Desktop.ViewModels;

public partial class LoginWindowViewModel : ObservableObject
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://127.0.0.1:5080")
    };

    private IReadOnlyList<EmployeeOption> _employees = [];

    public ObservableCollection<RoleOption> Roles { get; } = [];
    public ObservableCollection<EmployeeOption> AvailableEmployees { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinue))]
    private RoleOption? selectedRole;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinue))]
    private EmployeeOption? selectedEmployee;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinue))]
    private bool isLoading = true;

    [ObservableProperty]
    private string statusText = "Получаю сотрудников с сервера…";

    [ObservableProperty]
    private bool hasLoadError;

    public bool CanContinue => !IsLoading && !HasLoadError && SelectedRole is not null && SelectedEmployee is not null;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        HasLoadError = false;
        StatusText = "Получаю сотрудников с сервера…";
        Roles.Clear();
        AvailableEmployees.Clear();
        SelectedRole = null;
        SelectedEmployee = null;

        try
        {
            var directory = await _httpClient.GetFromJsonAsync<LoginDirectoryDto>(
                "/api/auth/dev-login-directory",
                cancellationToken);

            if (directory is null)
                throw new InvalidOperationException("Сервер вернул пустой справочник входа.");

            foreach (var role in directory.Roles)
                Roles.Add(new RoleOption(role.Code, role.Name, role.Description));

            _employees = directory.Employees
                .Select(x => new EmployeeOption(x.Id, x.DisplayName, x.RoleCodes))
                .ToArray();

            if (!directory.SourceReady)
            {
                HasLoadError = true;
                StatusText = directory.Message ?? "Справочник сотрудников ещё не подготовлен.";
                return;
            }

            StatusText = directory.Message ?? $"Загружено сотрудников: {_employees.Count}. Источник: {directory.Source}.";
            SelectedRole = Roles.FirstOrDefault();
        }
        catch (HttpRequestException)
        {
            HasLoadError = true;
            StatusText = "Dentalla Server недоступен на 127.0.0.1:5080. Сначала запустите Dentalla.Api.";
        }
        catch (Exception ex)
        {
            HasLoadError = true;
            StatusText = $"Не удалось загрузить сотрудников: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanContinue));
        }
    }

    partial void OnSelectedRoleChanged(RoleOption? value)
    {
        AvailableEmployees.Clear();
        SelectedEmployee = null;

        if (value is null)
            return;

        foreach (var employee in _employees
                     .Where(x => x.RoleCodes.Contains(value.Code, StringComparer.Ordinal))
                     .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            AvailableEmployees.Add(employee);
        }

        if (AvailableEmployees.Count == 1)
            SelectedEmployee = AvailableEmployees[0];

        OnPropertyChanged(nameof(CanContinue));
    }

    partial void OnHasLoadErrorChanged(bool value) => OnPropertyChanged(nameof(CanContinue));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(CanContinue));

    public async Task<DesktopSessionContext?> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedRole is null || SelectedEmployee is null)
            throw new InvalidOperationException("Для входа необходимо выбрать роль и сотрудника.");
        if (!Guid.TryParse(SelectedEmployee.Id, out var staffProfileId))
            throw new InvalidOperationException("Некорректный StaffProfile выбранного сотрудника.");

        IsLoading = true;
        HasLoadError = false;
        StatusText = "Создаю защищённую сессию Dentalla…";

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/auth/dev-session",
                new DevSessionRequest(staffProfileId, SelectedRole.Code, "Dentalla Desktop dev"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                HasLoadError = true;
                StatusText = $"Сервер не выдал сессию ({(int)response.StatusCode}).";
                return null;
            }

            var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(cancellationToken: cancellationToken);
            if (token is null)
                throw new InvalidOperationException("Сервер вернул пустую сессию.");

            StatusText = "Сессия создана.";
            return new DesktopSessionContext(
                SelectedRole.Code,
                SelectedRole.Name,
                SelectedEmployee.Id,
                SelectedEmployee.Name,
                token.AccessToken,
                token.ExpiresAtUtc);
        }
        catch (HttpRequestException)
        {
            HasLoadError = true;
            StatusText = "Dentalla Server недоступен. Сессия не создана.";
            return null;
        }
        catch (Exception ex)
        {
            HasLoadError = true;
            StatusText = $"Не удалось выполнить вход: {ex.Message}";
            return null;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanContinue));
        }
    }
}

public sealed record RoleOption(string Code, string Name, string Description);
public sealed record EmployeeOption(string Id, string Name, IReadOnlyList<string> RoleCodes);
