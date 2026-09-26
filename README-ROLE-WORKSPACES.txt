Dentalla MIS — Server-driven login + first role workspaces
24.09.2026

Что меняется
1. Удалён захардкоженный список сотрудников из Desktop.
2. LoginWindow получает роли/сотрудников от Dentalla.Api: GET /api/auth/dev-login-directory.
3. Сервер строит временную login projection из Dentalla.ident_raw:
   Staffs + Persons + Items + ProfessionNames.
4. IDENT role mapping bootstrap:
   8 -> Doctor
   7 -> Administrator
   104 -> Marketer
   9 -> Director
   ChiefMedicalOfficer временно задаётся DentallaServer:DevelopmentChiefMedicalOfficerLegacyStaffId=1.
5. После входа открывается единый Shell с одним из пяти workspace:
   Doctor / Administrator / ChiefMedicalOfficer / Marketer / Director.
6. В ролевых экранах нет фиктивных пациентов/денег. Там, где серверный read model ещё не подключён, показано пустое состояние "—".

Предусловие
Полный IDENT snapshot должен быть выполнен так, чтобы в Dentalla существовали:
  ident_raw.Staffs
  ident_raw.Persons
  ident_raw.Items
  ident_raw.ProfessionNames

Порядок
1. Остановить Dentalla.Desktop и Dentalla.Api.
2. Распаковать патч с заменой файлов в C:\Projects\MIS Dentalla
3. Собрать:
   dotnet build .\Dentalla.Server.slnf
4. Запустить API:
   dotnet run --project .\src\Dentalla.Api\Dentalla.Api.csproj
5. Проверить каталог входа в браузере:
   http://127.0.0.1:5080/api/auth/dev-login-directory
6. Во втором PowerShell запустить Desktop:
   dotnet run --project .\src\Dentalla.Desktop\Dentalla.Desktop.csproj

Следующий инкремент
Нормализация ident_raw -> StaffProfile/UserAccount/UserRoleAssignment, затем Patient/Appointment/Encounter API для живых ролевых экранов.
