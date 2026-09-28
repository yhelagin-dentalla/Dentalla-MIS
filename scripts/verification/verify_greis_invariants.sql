/*
DENTALLA MIS — executable GREIS reconciliation gate.
Run after EF migrations + GREIS import against a staging/migration-rehearsal Dentalla database.
No DDL. No data mutation.
*/

set nocount on;
set xact_abort on;

if object_id(N'clinical.Encounters', N'U') is null throw 57000, 'clinical.Encounters missing.', 1;
if object_id(N'clinical.PerformedServices', N'U') is null throw 57001, 'clinical.PerformedServices missing.', 1;
if object_id(N'clinical.TreatmentCourses', N'U') is null throw 57002, 'clinical.TreatmentCourses missing.', 1;
if object_id(N'clinical.TreatmentCourseEncounters', N'U') is null throw 57003, 'clinical.TreatmentCourseEncounters missing.', 1;
if object_id(N'clinical.TreatmentCoursePlannedServices', N'U') is null throw 57004, 'clinical.TreatmentCoursePlannedServices missing.', 1;
if object_id(N'finance.PatientHistoricalReceiptTotals', N'U') is null throw 57005, 'finance.PatientHistoricalReceiptTotals missing.', 1;

if (select count_big(*) from clinical.Encounters) <> 15836
    throw 57010, 'GREIS invariant failed: Encounter count must be 15,836.', 1;

if (select count_big(*) from clinical.PerformedServices) <> 30799
    throw 57011, 'GREIS invariant failed: PerformedService count must be 30,799.', 1;

if (select coalesce(sum(convert(decimal(19,4), FinalAmount)), 0) from clinical.PerformedServices) <> convert(decimal(19,4), 92524139.0000)
    throw 57012, 'GREIS invariant failed: historical delivered work must be 92,524,139.00.', 1;

if (select count_big(*) from clinical.TreatmentCourses) <> 27
    throw 57013, 'GREIS invariant failed: TreatmentCourse count must be 27.', 1;

if (select count_big(*) from clinical.TreatmentCourseEncounters) <> 35
    throw 57014, 'GREIS invariant failed: TreatmentCourseEncounter count must be 35.', 1;

if (select count_big(*) from clinical.TreatmentCoursePlannedServices) <> 5
    throw 57015, 'GREIS invariant failed: TreatmentCoursePlannedService count must be 5.', 1;

if (select count_big(*) from finance.PatientHistoricalReceiptTotals where SourceSystem=N'GREIS' and CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1') <> 2387
    throw 57016, 'GREIS invariant failed: historical receipt Patient count must be 2,387.', 1;

if (select coalesce(sum(Amount), 0) from finance.PatientHistoricalReceiptTotals where SourceSystem=N'GREIS' and CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1') <> convert(decimal(19,4), 93880212.5000)
    throw 57017, 'GREIS invariant failed: GREIS_ACTUAL_RECEIPTS_V1 must be 93,880,212.50.', 1;

select N'GREIS reconciliation passed' as Result;
