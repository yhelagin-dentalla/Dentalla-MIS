# GREIS Phase 5B — treatment-course reconciliation

Date: 2026-09-26

Phase 5B completed successfully against the Dentalla migration database.

## Final normalized counts

- `clinical.TreatmentCourses`: 27 GREIS courses.
- Completed source courses: 0.
- Courses without normalized start date: 2.
- Courses without normalized end date: 2.
- `clinical.TreatmentCourseEncounters`: 35 links.
- Courses with Encounter links: 25.
- Distinct linked Encounters: 35.
- `clinical.TreatmentCoursePlannedServices`: 5 rows.
- Planned quantity total: 12.0000.
- Courses with planned services: 1.
- `integration.LegacyTreatmentCourseEvents`: 1,437 immutable source-history rows.

No additional historical ServiceCatalogItem was needed for the five planned-service rows: GREIS ServiceCatalogItem ExternalIdentifier count remained 293.

## Source-history operation distribution

- `Измение услуги`: 1,023
- `Удаление услуги`: 285
- `Добавление услуги`: 44
- `Добавление визита`: 40
- `Создание курса`: 37
- `Удаление визита`: 5
- `Удаление курса`: 2
- `Измение курса`: 1

This confirms that `history_vtreatments` is primarily an audit/history stream and must remain provenance rather than being replayed as current editable state.

## Two current courses without final Encounter links

- GREIS treatment 106 — `07.12.2013 -`, no planned services.
- GREIS treatment 132 — `Морозов И.Д. Заказ-наряд №333`, five planned services.

Both remain valid current legacy course entities and were retained.

## Canonical-link coverage of historical events

Of 1,437 historical events:

- 77 resolve to one of the 27 current TreatmentCourses;
- 85 resolve to a canonical Patient;
- 38 resolve to a canonical StaffProfile.

The remaining events are intentionally retained with their original legacy identifiers/display names instead of inventing canonical relationships.

## Semantic boundary

GREIS `vtreatments` remain modeled as historical `TreatmentCourse` / care episode objects. They are **not** promoted to the new MIS comprehensive editable TreatmentPlan.

Phase 5 contains no finance migration. The next GREIS wave is aggregate **actual cash received per canonical Patient**, with no migration of debts, advance balances, internal account balances, or legacy payment allocation state.
