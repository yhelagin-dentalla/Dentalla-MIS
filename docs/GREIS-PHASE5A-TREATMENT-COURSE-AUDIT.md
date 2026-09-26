# GREIS Phase 5A — treatment-course audit

Date: 2026-09-26

## Source semantics

The GREIS object named `vtreatments` is not a safe equivalent of the new MIS comprehensive TreatmentPlan. The source audit history explicitly uses course terminology (`Создание курса`, `Добавление визита`, `Удаление визита`, `Измение курса`) and most rows behave as a grouping of visits.

Therefore the Phase 5 target concept is **TreatmentCourse / historical care episode**, not an editable modern treatment plan.

## Confirmed counts

- `greis_raw.vtreatments`: 27 current rows.
- `greis_raw.history_vtreatments`: 1,437 immutable history rows.
- `greis_raw.vtreatments_prescriptions`: 5 rows.
- 35 final normalized GREIS visits reference `treatment_id`.
- Those 35 visits reference 25 distinct treatment IDs.
- Every one of those 25 treatment IDs belongs to exactly one canonical Patient.
- No referenced course crosses patients.
- 2 current courses have no final visit link: GREIS treatment IDs 106 and 132.
- Treatment 132 contains all five prescription/planned-service rows.
- All 27 current rows have `treatment_completed=0`; preserve the source flag but do not derive additional completion semantics.
- GREIS `d1/d2=1900-01-01` is a legacy unknown-date sentinel. Normalize it to null while retaining the unchanged raw source.

## Phase 5B target

- `clinical.TreatmentCourses`: 27 current GREIS courses.
- `clinical.TreatmentCourseEncounters`: 35 links to the already-normalized Encounters.
- `clinical.TreatmentCoursePlannedServices`: 5 prescription/planned-service rows.
- `integration.LegacyTreatmentCourseEvents`: all 1,437 source history events as immutable provenance.
- source-aware ExternalIdentifiers and migration maps for all normalized/imported entities.

The two unreferenced current courses are still imported because they are current source entities. Course 132 is especially important because it carries the five prescription rows.

## Boundary

No GREIS finance is imported in Phase 5. The following wave remains aggregate actual receipts per canonical Patient only, without GREIS debt, advance balances, internal account balances, or reconstructed allocation state.
