# GREIS Phase 4B — target model design

Date: 2026-09-26

Status: mapping frozen after Phase 4A + 4A2; executable migration prepared.

## Separation of concerns

- `scheduling.Appointment` = schedule fact/status.
- `clinical.Encounter` = actual clinical event; an appointment does not automatically imply an encounter.
- GREIS `visits.comments` = legacy appointment/admin provenance, **not** an editable or formal medical `ClinicalNote`.
- `services.ServiceCatalogItem` = immutable historical service definition needed to render old GREIS work.
- `clinical.PerformedService` = historical delivered-work fact bound to an Encounter and preserving source service attributes.
- GREIS cash/debt/advance semantics are not reconstructed in this phase.

## Audited source counts

Final clinical GREIS appointments after identity/staff cleanup: **18,823**.

- Fulfilled: **15,836** → eligible `Encounter` rows.
- `visits_services` total source rows: **30,847**.
- rows on final clinical appointments: **30,817**.
- rows on Fulfilled appointments: **30,813**.
- 4 rows are attached to non-Fulfilled appointments (2 Arrived, 2 NoShow) and remain provenance only.
- 10 rows belong to excluded advance-payment pseudo-visits.
- 20 rows belong to user-confirmed test/error visits.

## Encounter mapping

Create one `clinical.Encounter` for every final GREIS appointment with mapped status `Fulfilled`: **15,836** rows.

Encounter retains:
- PatientId;
- AppointmentId;
- primary StaffProfileId;
- StartedLocal / EndedLocal;
- `ExternalIdentifier(SystemCode=GREIS, EntityType=Encounter, ExternalId=visit_id)`.

Scheduled, Confirmed, Arrived and NoShow appointments do not become Encounters merely because an Appointment exists.

## Legacy appointment comments

`greis_raw.visits.comments` is filled on 6,819 final appointments, but audited examples contain callback/no-answer notes, cancellation reasons, promotions, certificate notes and other administrative content. Therefore it is **not** migrated into `ClinicalNote`.

It is preserved source-aware in `integration.LegacyAppointmentDetails`, together with legacy `treatment_id` / `diagnos_txt` provenance. `diagnos_txt` is empty in the audited GREIS dataset.

## Historical service catalog

Canonical source key is `prices_articles.price_article_id`, referenced by `visits_services.price_article_id`.

Coverage is complete:
- 296 distinct price articles are used by Fulfilled service history;
- every one resolves to `prices_articles`;
- there are no duplicate `price_article_id` keys and no missing article mappings.

Three article IDs are financial instruments, not clinical services, and are excluded from the clinical catalog:
- `1826` — `Сертификат на 500 рублей`;
- `1827` — `Сертификат на 1000 рублей`;
- `1910` — `Аванс на стоматологические услуги`.

Therefore Phase 4B creates **293** historical `ServiceCatalogItem` rows and source-aware GREIS ExternalIdentifiers for them. They are historical only and must never become an active current Dentalla price simply because they existed in GREIS.

## Performed services

The 30,813 Fulfilled service rows are further classified.

Financial service artifacts excluded from delivered work:
- 9 rows for article 1826;
- 3 rows for article 1827;
- 2 rows for article 1910;
- total: **14**.

Thus **30,799** rows become `clinical.PerformedService`.

Each row preserves:
- source `service_id` through `ExternalIdentifier(SystemCode=GREIS, EntityType=PerformedService, ExternalId=service_id)`;
- historical service item (`price_article_id` → `ServiceCatalogItem`);
- Patient / primary StaffProfile / Encounter;
- quantity;
- raw tooth text;
- raw `n_mkb` (empty in this dataset);
- service comment;
- source unit price (`cost`);
- discount percent and discount rubles;
- final historical amount (`cost_with_discount`);
- prime cost;
- raw complexity id/value;
- `manipulation_ok` only as `LegacyManipulationOk` provenance.

### Important audited anomalies

- `manipulation_ok=0` on all 30,813 Fulfilled service rows. It therefore **cannot** be used as a completion criterion.
- quantity=0 exists on 3 Fulfilled rows and is preserved as-is.
- zero final amount exists on 29 Fulfilled rows and is preserved as-is.
- raw tooth values include legacy anomalies; they remain text and are not silently normalized to FDI values.
- five negative service amounts are certificate rows and are excluded together with all certificate/advance financial service artifacts.

The resulting delivered-work amount represented by the 30,799 normalized PerformedServices is **92,524,139.00**.

## Complete service-source accounting

All 30,847 GREIS `visits_services` rows are accounted for:

- 30,799 → normalized `PerformedService`;
- 10 → parent visit excluded as advance-payment pseudo-visit;
- 20 → parent visit excluded as test/error;
- 4 → non-Fulfilled appointment, provenance only;
- 14 → financial certificate/advance artifact, provenance only.

No source service row is silently dropped.

## Treatment IDs

Only 35 final Fulfilled visits reference `treatment_id` (25 distinct IDs); all 25 resolve to current GREIS `vtreatments`. They are retained in legacy appointment provenance in this phase. Treatment-plan normalization is a separate migration step and is not fabricated inside Encounter/PerformedService.

## Finance boundary

Phase 4B does not migrate GREIS cash, debts, advances, account movements or payment allocation.

The approved GREIS finance rule remains: migrate later only the **aggregate amount actually received from each patient** for the GREIS period, after removing double counting. No old debt/advance state is recreated.
