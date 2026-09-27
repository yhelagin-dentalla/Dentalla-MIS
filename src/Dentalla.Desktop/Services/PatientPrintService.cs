using System.Diagnostics;
using System.Net;
using System.Text;
using Dentalla.Contracts.Patients;

namespace Dentalla.Desktop.Services;

public static class PatientPrintService
{
    public static void OpenPrintPreview(PatientWorkspaceDto patient)
    {
        var root = Path.Combine(Path.GetTempPath(), "Dentalla", "print");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, $"patient-{patient.Id:N}.html");
        File.WriteAllText(path, BuildHtml(patient), Encoding.UTF8);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private static string BuildHtml(PatientWorkspaceDto patient)
    {
        static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var html = new StringBuilder();
        html.Append("<!doctype html><html lang='ru'><head><meta charset='utf-8'>")
            .Append("<title>Dentalla — карточка пациента</title>")
            .Append("<style>body{font-family:Arial,sans-serif;margin:28px;color:#222}h1{font-size:22px;margin:0 0 6px}h2{font-size:16px;margin:24px 0 8px;border-bottom:1px solid #ddd;padding-bottom:5px}.meta{color:#666;margin-bottom:18px}table{width:100%;border-collapse:collapse;font-size:12px}th,td{padding:6px 8px;border-bottom:1px solid #ddd;text-align:left;vertical-align:top}th{background:#f4f3ef}.amount{text-align:right;white-space:nowrap}@media print{body{margin:10mm}.no-print{display:none}}</style>")
            .Append("</head><body>")
            .Append("<button class='no-print' onclick='window.print()'>Печать</button>")
            .Append($"<h1>{H(patient.FullName)}</h1>")
            .Append($"<div class='meta'>Карта № {H(patient.CardNumber)} · Дата рождения: {H(patient.BirthDate?.ToString("dd.MM.yyyy"))}</div>");

        html.Append("<h2>Приёмы</h2><table><thead><tr><th>Дата</th><th>Время</th><th>Врач</th><th>Статус</th></tr></thead><tbody>");
        foreach (var visit in patient.Visits.OrderByDescending(x => x.StartLocal))
        {
            html.Append("<tr>")
                .Append($"<td>{visit.StartLocal:dd.MM.yyyy}</td>")
                .Append($"<td>{visit.StartLocal:HH:mm}–{visit.EndLocal:HH:mm}</td>")
                .Append($"<td>{H(visit.DoctorName)}</td>")
                .Append($"<td>{H(StatusText(visit.StatusCode))}</td>")
                .Append("</tr>");
        }
        html.Append("</tbody></table>");

        html.Append("<h2>Оказанные услуги</h2><table><thead><tr><th>Услуга</th><th>Количество</th><th>Зуб</th><th>Врач</th><th class='amount'>Сумма</th></tr></thead><tbody>");
        foreach (var service in patient.Services)
        {
            var name = string.IsNullOrWhiteSpace(service.ServiceCode)
                ? service.ServiceName
                : $"{service.ServiceCode} — {service.ServiceName}";
            html.Append("<tr>")
                .Append($"<td>{H(name)}</td>")
                .Append($"<td>{service.Quantity:N2}</td>")
                .Append($"<td>{H(service.Tooth)}</td>")
                .Append($"<td>{H(service.DoctorName)}</td>")
                .Append($"<td class='amount'>{service.FinalAmount:N2} ₽</td>")
                .Append("</tr>");
        }
        html.Append("</tbody></table>");

        html.Append("<h2>Планы / курсы лечения</h2>");
        if (patient.TreatmentCourses.Count == 0)
            html.Append("<div>Нет данных</div>");
        else
            foreach (var course in patient.TreatmentCourses)
                html.Append($"<div><strong>{H(course.Name)}</strong> · {H(course.OwnerStaffName)} · связанных посещений: {course.EncounterCount}</div>");

        html.Append("<h2>Исторические поступления</h2>")
            .Append($"<div>GREIS historical aggregate: <strong>{patient.HistoricalReceiptTotal:N2} ₽</strong></div>")
            .Append("<p style='color:#777;font-size:10px'>Печатная форма создана из нормализованной БД Dentalla. Исторические поступления не являются текущим балансом пациента.</p>")
            .Append("</body></html>");

        return html.ToString();
    }

    private static string StatusText(string code) => code switch
    {
        "Cancelled" => "Отменён",
        "Confirmed" => "Подтверждён",
        "Arrived" => "Пришёл",
        "Fulfilled" => "Завершён",
        "NoShow" => "Неявка",
        _ => "Запланирован"
    };
}
