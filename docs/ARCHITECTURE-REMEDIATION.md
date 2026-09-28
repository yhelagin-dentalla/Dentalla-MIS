# Architecture remediation — migration gates, schema ownership, tests/CI

Date: 2026-09-28

## Status

This document is the executable-development companion to Notion ADR-02. It does not redefine the domain model.

## 1. Migration gate

Effective immediately, no new legacy/raw dataset may be normalized into a canonical Dentalla entity until the target domain model for that entity has been approved in the corresponding Notion source-of-truth page.

Already imported GREIS data is preserved. Existing Encounter, PerformedService, TreatmentCourse and historical receipt rows are migration evidence and compatibility data; this remediation does not delete or reinterpret them.

Any further GREIS normalization requires an explicit approved target model first. Raw/staging ingestion may continue because it does not assert a canonical domain model.

## 2. One canonical-schema owner

EF Core migrations are the only owner of the canonical Dentalla SQL Server schema.

Rules for all new import scripts:
- no CREATE/ALTER/DROP of canonical tables, indexes, constraints or schemas;
- import scripts may validate source/raw data and perform DML into schema already created by EF migrations;
- raw/staging schema may have a separate ingestion owner, but it must not mutate canonical schema;
- clean installation must be reproducible from EF migrations before any canonical import runs.

The existing GREIS phase 4B/5B/6B scripts are grandfathered historical artifacts because they were already applied before this rule was enforced. They must not be copied as a template for new work. `Dentalla.ArchitectureTests` fails CI if a new SQL file is added to `scripts/migration` with canonical DDL unless it is in the explicit historical allow-list.

Existing applied EF migrations are not rewritten by this remediation. Rewriting applied history would create a different risk for the current database. A later consolidation/baseline decision requires a clean-database rehearsal and an explicit ADR.

## 3. Automated gates

CI now performs:
1. `dotnet restore`;
2. `dotnet build` with warnings treated as errors;
3. EF `has-pending-model-changes`;
4. architecture tests enforcing the canonical-schema ownership rule and presence of reconciliation invariants.

`tests/Dentalla.ArchitectureTests` intentionally has no third-party test framework dependency. It is a deterministic executable gate: any failed invariant exits non-zero and fails CI.

## 4. GREIS reconciliation

`scripts/verification/verify_greis_invariants.sql` converts the audited migration totals into executable assertions against a database containing the imported GREIS dataset. It verifies at least:
- Encounter = 15,836;
- PerformedService = 30,799;
- historical delivered work = 92,524,139.00 RUB;
- TreatmentCourse = 27;
- TreatmentCourseEncounter = 35;
- TreatmentCoursePlannedService = 5;
- historical receipt Patients = 2,387;
- GREIS_ACTUAL_RECEIPTS_V1 = 93,880,212.50 RUB.

These data-dependent assertions cannot run on a blank GitHub-hosted runner because the private GREIS dataset is not present there. They are nevertheless executable checks, not Markdown-only numbers. They must be run after GREIS import in staging/migration rehearsal. CI validates that the invariant script and its approved constants remain present; a future sanitized migration fixture can promote the data reconciliation itself into hosted CI.

## 5. Patient Workspace / operational development

Patient Workspace development may continue only against approved operational models. Import convenience is not a reason to expand an unapproved canonical entity. Historical-import shape and operational domain shape must be separated where their semantics differ.
