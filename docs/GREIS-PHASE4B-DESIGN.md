# GREIS Phase 4B — target model design

Date: 2026-09-26

Status: draft; final mapping is gated by Phase 4A2.

## Separation of concerns

- `scheduling.Appointment` = schedule fact/status.
- `clinical.Encounter` = actual clinical event; an appointment does not automatically imply an encounter.
- GREIS `visits.comments` = legacy visit/appointment comment provenance, **not** an editable or formal medical `ClinicalNote` by default.
- source-aware historical service definition = the display/identity metadata needed to reproduce old GREIS services.
- `clinical.PerformedService` = historical delivered-work fact bound to an Encounter and preserving source service attributes.

## GREIS mapping rules

### Encounter

Create an Encounter only for GREIS visits mapped as `Fulfilled`, unless Phase 4A2 produces evidence that a specific anomalous row requires a different explicit disposition. Do not fabricate encounters for Scheduled, Confirmed, Arrived or NoShow merely because an Appointment exists.

Encounter stores:
- PatientId
- AppointmentId
- Primary StaffProfileId
- StartedLocal / EndedLocal
- source provenance via `ExternalIdentifier(SystemCode=GREIS, EntityType=Encounter, ExternalId=visit_id)`

### Legacy visit comments

`greis_raw.visits.comments` is preserved verbatim only as legacy visit/appointment comment provenance where useful for historical UI/audit. It is not normalized into diagnosis, anamnesis, clinical note or other medical-document semantics without additional evidence.

`greis_raw.visits.diagnos_txt` is empty in the audited dataset and therefore creates no normalized clinical data.

### Historical service definitions

Canonical GREIS source key is `prices_articles.price_article_id`, referenced by `visits_services.price_article_id`.

The historical service definition must retain enough immutable source metadata to render the old service correctly:
- source article id
- article name
- article code
- source group id/name when resolvable

It must not become a current active Dentalla price merely because it existed in GREIS. Current Dentalla pricing/payroll rules remain independent.

### Performed services

For each eligible `visits_services` row attached to an approved fulfilled GREIS encounter, preserve:
- source `service_id`
- source service/article identity (`price_article_id`)
- quantity
- tooth text
- `n_mkb`
- source service comment
- gross/source cost
- discount percent and discount rubles where present
- final `cost_with_discount`
- prime cost if present
- manipulation flag
- complexity id/value if present

Historical source values are preserved exactly; they are not recalculated from modern Dentalla price lists.

### Non-Fulfilled service rows

Phase 4A found four `visits_services` rows outside Fulfilled appointments: two on Arrived and two on NoShow. Phase 4A2 must show all four rows before they are assigned any normalized delivered-work meaning. Until then they remain raw/provenance only.

### Historical anomalies

Zero quantity, negative amount, unusual discount and `manipulation_ok=0` values are not automatically repaired. Phase 4A2 classifies them; Phase 4B either preserves them as source facts with an explicit disposition or excludes them from delivered-work calculations with an auditable reason.

## Finance boundary

This phase does not migrate GREIS cash, debts, advances or account movements. Historical delivered-service amounts are delivered-work/service facts and remain separate from cash receipt facts.

GREIS financial migration later imports only the aggregate amount actually received from each patient, according to the already approved migration rule.
