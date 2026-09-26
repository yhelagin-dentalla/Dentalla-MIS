# GREIS Phase 4B — target model design

Date: 2026-09-26

This document records the target-domain contract before normalization code is added.

## Separation of concerns

- `scheduling.Appointment` = schedule fact/status.
- `clinical.Encounter` = actual clinical event; an appointment does not automatically imply an encounter.
- `clinical.LegacyClinicalNoteSnapshot` = immutable source text/provenance. It is not an editable current clinical note.
- `services.ServiceCatalogItem` = canonical service definition/name/code/group reference.
- `clinical.PerformedService` = historical delivered-work fact bound to an Encounter and preserving source financial/service attributes.

## GREIS mapping rules

### Encounter

Create an Encounter only for GREIS visits mapped as `Fulfilled`. Do not fabricate encounters for Scheduled, Confirmed, Arrived or NoShow merely because an Appointment exists.

Encounter stores:
- PatientId
- AppointmentId
- Primary StaffProfileId
- StartedLocal / EndedLocal
- legacy source provenance via `ExternalIdentifier(SystemCode=GREIS, EntityType=Encounter, ExternalId=visit_id)`

### Legacy clinical text

GREIS `visits.comments` is preserved verbatim when non-empty. It is not normalized into diagnosis, anamnesis or other semantic fields. `visits.diagnos_txt` is empty in the audited dataset and therefore generates no clinical records.

### Service catalog

Canonical source for GREIS service identity is `prices_articles.price_article_id` referenced by `visits_services.price_article_id`.

Imported historical catalog items retain:
- source article id
- article name
- article code
- group id/name when resolvable
- active/archive state is historical metadata, not a current price decision

Do not infer current Dentalla price, salary rate or payroll rules from GREIS prices.

### Performed services

For each eligible `visits_services` row attached to a Fulfilled GREIS visit, preserve:
- source `service_id`
- service catalog item (`price_article_id`)
- quantity
- tooth text
- `n_mkb`
- comments
- gross/source cost
- discount percent and discount rubles where present
- final `cost_with_discount`
- prime cost if present
- manipulation flag
- complexity id/value if present

The historical amount must not be recomputed using the modern price catalog.

### Rows on non-Fulfilled appointments

Audit found four service rows outside Fulfilled appointments: two on Arrived and two on NoShow. These are anomalous historical facts. Do not silently convert them into delivered work. Preserve them in raw/provenance and classify them for review before any normalization as PerformedService.

## Finance boundary

This phase does not migrate GREIS cash, debts, advances or account movements. Historical delivered-service amounts are clinical/delivered-work facts and remain separate from cash receipt facts.
