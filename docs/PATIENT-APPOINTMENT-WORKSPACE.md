# Patient Workspace + Appointment Workspace

Status: implementation design + incremental runtime rollout, 2026-09-27.

## Product rule

Dentalla has one canonical Patient Workspace for every role. Role-specific copies of a patient card are prohibited. The server computes EffectivePermission / clinical privilege; the UI only projects permitted data/actions.

IDENT screenshots are used only to inventory required workflows. Dentalla does not copy IDENT visual composition.

## Patient Workspace structure

Sticky PatientContextBar:
- full name;
- card number;
- birth date / age when available;
- primary contact when ContactPoint is normalized;
- nearest appointment;
- critical medical alerts;
- representative for minors;
- contextual actions: Schedule, Start/Open encounter, Edit, Print, More.

Primary sections:
1. Overview
2. Treatment
3. Medical chart
4. Research / images
5. Documents
6. Appointments
7. Finance
8. Contacts / family
9. Communications

### Overview
Dense summary, not dashboard decoration. Patient identity, contacts, next appointment, latest encounter, active treatment plan, alerts, document/task state, permitted finance summary and acquisition/referral projection.

### Treatment
Appointment/Encounter timeline, selected-encounter PerformedService, TreatmentCourse/TreatmentPlan, stage progress and links to the active clinical workflow.

### Medical chart
ClinicalNote, diagnosis / clinical problem, findings, signatures, immutable versions and audit trail. Legacy clinical text is immutable provenance.

### Research / images
Photos, X-ray, DICOM/CBCT, radiology passport and descriptions. Metadata/thumbnails first; original media on demand.

### Documents
Contracts, informed consents, refusals, generated forms and attachments. Centralized document templates and print/report pipeline.

### Appointments
Future appointments first, then history. Create/reschedule/confirm/cancel by Appointment.Manage. No physical delete of real appointments.

### Finance
Delivered work, invoices, payments, advance, advance application, receivable, refund and void are distinct facts. Historical GREIS receipt aggregates are not current balance.

### Contacts / family
Person + Patient + ContactPoint + Address + PersonRelationship + RepresentativeLink + PatientPayerLink. Kinship, legal representation and payer are separate relations.

### Communications
Calls, SMS, messages, staff comments, recalls/tasks and resulting appointment links as a single timeline.

## Appointment Workspace

Available from Patient Workspace and role workspaces.

Three-column layout:
- left: date, doctor/specialty, room/chair, duration filters;
- center: ranked free-slot availability returned by Scheduling API;
- right: appointment editor.

Appointment editor fields:
- Patient;
- date/time;
- duration;
- doctor;
- room/chair;
- visit type;
- administrative comment / note for doctor;
- status;
- reminder/contact options when Communications is available.

Before save, server validates permission, Patient, time conflict, doctor availability, room/chair occupancy and professional constraints. Desktop never writes SQL directly.

## Lifecycle and destructive actions

User UI does not physically delete Patient, Appointment, Encounter, ClinicalNote or finance facts. Use:
- Appointment -> Cancel;
- Patient -> Archive/Merge;
- clinical signed records -> correction/version flow;
- finance -> Void/Refund/reversal as applicable.

Every destructive-equivalent action requires actor, reason and AuditEvent.

## Role projection

- Doctor: clinical read/write within privilege; own workflow; permitted scheduling and finance projections.
- Administrator: patient/admin/contact/scheduling actions; clinical read-only unless explicitly granted; no clinical signing by role name alone.
- ChiefMedicalOfficer: clinical review, quality actions and clinical write/sign only when professional privilege allows.
- Director: management/finance/reporting and patient actions only through EffectivePermission.
- Marketer: Patient Workspace read according to granted permission; marketing actions; no clinical write, finance transactions or admin actions by role name alone.

## Runtime status 2026-09-27

Implemented on `feature/patient-workspace-readonly`:
- patient search and canonical Patient read model;
- Appointment history;
- GREIS Encounter / PerformedService projection;
- TreatmentCourse projection;
- GREIS historical receipt aggregate;
- visit status filters;
- separate PatientWorkspaceWindow;
- double-click navigation from Administrator, Doctor and Patient list;
- tabbed Patient Workspace shell;
- AppointmentWorkspaceWindow shell opened from Patient card.

Known gaps before full operational mode:
- Person/Patient normalization with ContactPoint/Address/relations;
- IDENT Encounter + PerformedService migration;
- operational Encounter lifecycle;
- ClinicalNote + signatures/versioning;
- Appointment command API + availability/conflict engine;
- Patient documents;
- research/media store;
- CommunicationEvent;
- current finance ledger;
- centralized print/report service;
- production authenticated Desktop session + server-authoritative authorization for every command.

No runtime fallback to `ident_raw`/`greis_raw` is permitted.
