MIS Dentalla — Login UI patch

Что меняется:
- приложение стартует с современного окна входа;
- сначала выбирается роль;
- список сотрудников автоматически фильтруется по выбранной роли;
- пароль на текущем этапе отсутствует;
- после входа выбранные сотрудник и роль отображаются в основном Shell.

Сотрудники в этом UI-инкременте — временный development directory на основе уже исследованной структуры IDENT.
Следующий шаг — заменить локальный directory на server API/StaffProfile без изменения интерфейса.

Установка:
1. Закрыть Dentalla.Desktop.
2. Распаковать архив в C:\Projects\MIS Dentalla с заменой файлов.
3. dotnet build .\Dentalla.Server.slnf
4. dotnet run --project .\src\Dentalla.Desktop\Dentalla.Desktop.csproj
