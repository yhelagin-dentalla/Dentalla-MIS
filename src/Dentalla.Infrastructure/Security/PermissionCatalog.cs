using Dentalla.Domain.Security;

namespace Dentalla.Infrastructure.Security;

public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionDefinition> Definitions =
    [
        new("Patient.View", "Patient", "Просмотр карточки пациента"),
        new("Patient.EditDemographics", "Patient", "Редактирование персональных и контактных данных"),
        new("Patient.MergeDuplicates", "Patient", "Объединение дублей пациентов", isSensitive: true),
        new("Appointment.View", "Schedule", "Просмотр записей"),
        new("Appointment.Manage", "Schedule", "Создание, перенос и отмена записей"),
        new("ScheduleOperations.BatchBlock", "Schedule", "Массовая блокировка интервалов"),
        new("Clinical.Record.View", "Clinical", "Просмотр клинической документации"),
        new("Clinical.Note.Edit", "Clinical", "Редактирование текущего дневника"),
        new("Clinical.Note.Sign", "Clinical", "Подписание медицинской записи", isSensitive: true, isClinicalPrivilegeBound: true),
        new("Clinical.Diagnosis.Manage", "Clinical", "Ведение диагнозов", isClinicalPrivilegeBound: true),
        new("Clinical.TreatmentPlan.Manage", "Clinical", "Ведение плана лечения", isClinicalPrivilegeBound: true),
        new("Payment.Accept", "Finance", "Приём оплаты", isSensitive: true),
        new("Refund.Create", "Finance", "Создание возврата", isSensitive: true),
        new("Refund.Approve", "Finance", "Утверждение возврата", isSensitive: true),
        new("CashShift.Close", "Finance", "Закрытие кассовой смены", isSensitive: true),
        new("Discount.Apply", "Finance", "Применение разрешённой скидки", isSensitive: true),
        new("Discount.Approve", "Finance", "Утверждение скидки сверх лимита", isSensitive: true),
        new("Price.Publish", "Finance", "Публикация версии прайс-листа", isSensitive: true),
        new("Payroll.ViewOwn", "Payroll", "Просмотр собственной выработки"),
        new("Payroll.ViewAll", "Payroll", "Просмотр начислений всех сотрудников", isSensitive: true),
        new("Payroll.Approve", "Payroll", "Утверждение расчёта зарплаты", isSensitive: true),
        new("Quality.View", "Quality", "Просмотр очереди качества"),
        new("Quality.Manage", "Quality", "Управление внутренним контролем качества", isSensitive: true),
        new("Sms.SendTemplate", "Communication", "Отправка SMS по утверждённому шаблону"),
        new("Sms.SendFreeText", "Communication", "Отправка свободного SMS", isSensitive: true),
        new("Marketing.View", "Marketing", "Просмотр маркетинговых данных"),
        new("Marketing.Manage", "Marketing", "Управление лидами, источниками и кампаниями"),
        new("Marketing.Campaign.Execute", "Marketing", "Запуск массовой коммуникации", isSensitive: true),
        new("ExternalIntegration.Manage", "System", "Подключение и управление внешними сервисами", isSensitive: true),
        new("Staff.Manage", "System", "Управление сотрудниками", isSensitive: true),
        new("RBAC.ManageRoleProfile", "System", "Изменение базовых прав ролей", isSensitive: true),
        new("RBAC.ManageUserOverride", "System", "Индивидуальные Allow/Deny сотрудника", isSensitive: true),
        new("RBAC.Delegate", "System", "Временное делегирование полномочий", isSensitive: true),
        new("RBAC.SwitchRoleContext", "System", "Переключение Director между рабочими контекстами ролей", isSensitive: true),
        new("Audit.View", "System", "Просмотр аудита", isSensitive: true),
        new("SystemSettings.Manage", "System", "Системные настройки", isSensitive: true)
    ];

    public static readonly IReadOnlyList<RolePermission> RoleDefaults = BuildRoleDefaults();

    private static IReadOnlyList<RolePermission> BuildRoleDefaults()
    {
        var list = new List<RolePermission>();
        Add(SystemRoleCode.Doctor,
            "Patient.View", "Appointment.View", "Clinical.Record.View", "Clinical.Note.Edit", "Clinical.Note.Sign",
            "Clinical.Diagnosis.Manage", "Clinical.TreatmentPlan.Manage", "Payroll.ViewOwn");

        Add(SystemRoleCode.Administrator,
            "Patient.View", "Patient.EditDemographics", "Appointment.View", "Appointment.Manage", "ScheduleOperations.BatchBlock",
            "Payment.Accept", "Refund.Create", "CashShift.Close", "Discount.Apply", "Sms.SendTemplate", "Sms.SendFreeText");

        Add(SystemRoleCode.Marketer,
            "Patient.View", "Clinical.Record.View", "Appointment.View", "Marketing.View", "Marketing.Manage");

        Add(SystemRoleCode.ChiefMedicalOfficer,
            "Patient.View", "Appointment.View", "Clinical.Record.View", "Clinical.Note.Edit", "Clinical.Note.Sign",
            "Clinical.Diagnosis.Manage", "Clinical.TreatmentPlan.Manage", "Quality.View", "Quality.Manage", "Payroll.ViewOwn");

        foreach (var definition in Definitions)
            list.Add(new RolePermission(SystemRoleCode.Director, definition.Code));

        return list;

        void Add(string role, params string[] permissions)
        {
            foreach (var permission in permissions)
                list.Add(new RolePermission(role, permission));
        }
    }
}
