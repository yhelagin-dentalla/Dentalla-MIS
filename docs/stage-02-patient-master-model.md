# Stage 02 — canonical patient master model

Status: approved design baseline for implementation.

## Problem

The current `Patient` entity is a transitional legacy-normalized projection (`Id`, `CardNumber`, `FullName`, `BirthDate`). It must not become the canonical person master by accumulating unrelated fields.

Stage 02 separates identity, clinic-patient role, contacts, documents, representation, payer and contractual relationships before any further patient-data normalization from IDENT/GREIS.

## Aggregate boundaries

### Person
Physical person master. Canonical name components and birth/demographic identity attributes live here. `FullName` is a projection, not the canonical stored identity field.

### Patient
A person's role as a patient of a clinic/organization. Owns clinic-scoped card number, patient status and registration lifecycle. `PersonId` links to `Person`.

### PersonContact
Multi-valued phone/e-mail/address contact point with type, primary flag, provenance and validity. Identity resolution/import must not overwrite a different valid contact.

### PersonDocument
Identity/personal document metadata belongs to the person, not the patient role.

### PatientRepresentative
Auditable Patient-to-Person relationship describing legal/other representative authority and validity period.

### PatientPayer
Auditable payer relationship. A payer is not a scalar Patient property and is not assumed to be the patient Person.

### PatientRelationship
Family/other relationship without merging either master record.

### PatientContract
Contractual relationship is modeled separately from Patient identity and may evolve independently.

### ExternalIdentifier
Remains the source-aware bridge to IDENT/GREIS. Source identifiers are provenance, never Dentalla master keys.

## Duplicate and merge policy

Duplicate detection creates candidates and evidence; it does not silently merge records. Merge is an explicit audited operation that selects canonical Person/Patient records while retaining external identifiers and provenance. The design must preserve enough provenance/history to support investigation and future split/remediation of an incorrect merge.

## Invariants

1. Person, Patient, payer and representative are separate concepts and persistence models.
2. Canonical person name is component-based; display FullName is derived.
3. Contacts are multi-valued and are not destructively overwritten by migration matching.
4. Card number belongs to Patient and is unique only within its clinic/organization scope.
5. IDENT/GREIS sources remain read-only and imports remain idempotent.
6. Identity/document/representative/contract changes and merge decisions are auditable.
7. Stage 03 may not normalize a patient-data class until the corresponding Stage 02 target entity is approved.

## Compatibility migration strategy

The first implementation is additive. Existing clinical/financial/scheduling foreign keys to `Patient.Id` remain stable while `Person` and the Stage 02 satellite entities are introduced. Existing Patient rows are linked to newly created Person masters through a deterministic, reconciliation-tested migration. No destructive replacement of Patient IDs is allowed.

Only after reconciliation passes may API/Desktop projections switch from the transitional `FullName`/`BirthDate` columns to canonical Person data. Transitional columns are removed in a later migration, not in the first Stage 02 migration.

## Implementation order

1. Add Person and Patient→Person linkage while retaining existing Patient IDs and compatibility fields.
2. Add contacts, documents, representatives, payers, relationships and contracts.
3. Add patient/person search and duplicate-candidate generation.
4. Add audited merge workflow.
5. Switch Patient Workspace/API to canonical projections.
6. Open the Stage 03 gate for patient identity/contact migration.

## Required verification gates

- Existing Patient count is unchanged after the additive migration.
- Every existing Patient has exactly one Person after backfill.
- No existing clinical, scheduling or finance FK becomes orphaned.
- Existing GREIS reconciliation invariants continue to pass.
- Card-number uniqueness is enforced in clinic scope, not globally on Person.
- Re-running the migration/import reconciliation does not create duplicate Person masters.
