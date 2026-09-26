MIS Dentalla — Director Role Context Switch
24.09.2026

Назначение
----------
Director может без logout/login переключать рабочий UI-контекст:
  Директор -> Администратор -> Главный врач -> Маркетолог -> Врач

Это НЕ impersonation и НЕ назначение пяти ролей пользователю.
Identity остаётся Director; выбранный context определяет рабочий workspace.

Реализация
----------
1. Permission catalog:
   RBAC.SwitchRoleContext (Sensitive, Director default).

2. Development server endpoint:
   POST /api/auth/dev-role-context-switch
   - доступен только loopback в текущем passwordless dev-login;
   - проверяет активный UserAccount + активный Director role assignment;
   - пишет AuditEvent Security.RoleContextChanged;
   - при отказе UI остаётся в прежнем контексте.

3. Desktop:
   - в TopBar Director появляется селектор рабочего режима;
   - кнопка «↩ Директор» возвращает в Director одним кликом;
   - в нижнем identity block показывается «Директор -> <контекст>»;
   - контролы/навигация переключаются на реальный workspace выбранной роли;
   - для каждого role-context запоминается последний выбранный Appointment;
   - Director в Doctor context видит clinic-wide расписание, а не только собственные приёмы.

4. Clinical safety:
   Director access не заменяет ClinicalPrivilege/аккредитацию. Медицинская подпись,
   вмешательства и иные профессионально ограниченные действия должны отдельно
   проверяться сервером при реализации соответствующих команд.

Важно
-----
Текущий endpoint имеет префикс dev- намеренно: парольный auth Desktop ещё не подключён.
При переходе Desktop на bearer session этот же workflow должен быть перенесён на
защищённый authenticated endpoint с проверкой EffectivePermission RBAC.SwitchRoleContext.
