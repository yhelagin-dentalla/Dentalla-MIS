# GREIS Phase 4A — clinical history and services audit

Date: 2026-09-26

## Confirmed input state

- Final normalized GREIS clinical appointments: **18,823**.
- Primary staff resolved: **18,823 / 18,823**.
- Advance pseudo-visits were already excluded.
- Test/error doctor rows were already excluded.

## `greis_raw.visits_services`

- Raw rows: **30,847**.
- Rows attached to final 18,823 clinical visits: **30,817**.
- Rows attached to advance pseudo-visits: **10**.
- Rows attached to ignored test/error visits: **20**.
- Orphans: **0**.
- Other unclassified: **0**.

The service fact is visit-scoped and contains at least:
`service_id`, `visit_id`, `price_article_id`, `cost`, `quantity`, `discount_id`, `discount_rub`, `cost_with_discount`, `prime_cost`, `supplier_id`, `n_mkb`, `comments`, `tooth`, `manipulation_ok`, `complexity_id`, `complexity_val`, `discount`, audit timestamps.

For final clinical visits:
- quantity total: **45,842**;
- quantity range: **0..32**;
- `cost_with_discount` total: **92,574,039.00**;
- range: **-1,000.00 .. 529,200.00**.

`service_id` is unique for all 30,817 final clinical service rows. `original_service_id` is empty for them.

## Visit/service coverage

- Arrived: 424 visits; 1 with service rows; 2 service rows.
- Confirmed: 32 visits; 0 with services.
- Fulfilled: 15,836 visits; 10,863 with service rows; 30,813 service rows.
- NoShow: 2,304 visits; 2 with service rows; 2 service rows.
- Scheduled: 227 visits; 0 with services.

Service rows per clinical visit: 7,957 visits have 0 rows; the rest range from 1 to 18 rows.

## `greis_raw.visits` text fields

Two likely text fields were found:
- `comments varchar(500)`: 6,819 non-empty values on final visits; max observed length 202.
- `diagnos_txt varchar(4000)`: **0** non-empty values.

**Important correction:** `visits.comments` is not safely a medical note. The Phase 4A sample includes administrative scheduling/contact text (for example callback/no-answer remarks). Therefore GREIS `visits.comments` must be treated as a legacy visit/appointment comment with source provenance, not automatically normalized into `ClinicalNote`, diagnosis, anamnesis or other medical-document fields.

No GREIS `ClinicalNote` is created from `visits.comments` by default.

## Service catalog evidence

`visits_services.price_article_id` references the legacy GREIS price/service catalog. Candidate catalog tables include:
- `prices_articles` — 355 rows; key `price_article_id`; contains `price_group_id`, `article_name`, `article_code`, durations, comments.
- `prices_groups` — 49 rows; hierarchy/category metadata.
- `prices_costs` — 347 rows; price_article + price_name + cost.
- `prices_names` — one price-name row in this dataset.
- `history_price` — 1,069 history rows.
- `services_history` — 196,419 service audit/history rows; this is provenance/audit, not the current delivered-service fact.

## Phase 4A2 gate

Before Phase 4B writes normalized Encounter/PerformedService data, run the narrow read-only 4A2 audit to settle:
- `price_article_id -> prices_articles` join coverage;
- the exact four service rows on non-Fulfilled visits;
- zero/negative/unusual quantity and amount rows;
- MKB/tooth/service-comment population;
- `treatment_id` usage;
- visit comments by appointment status.

## Target-domain conclusion

The current core normalization is insufficient for direct GREIS clinical normalization. The expected Phase 4B domain boundary is:

1. `clinical.Encounter` — one actual clinical event for a truly fulfilled appointment, separate from scheduling status.
2. source-aware historical service definition/catalog support for GREIS `price_article_id`.
3. `clinical.PerformedService` — delivered-work fact preserving historical quantity, tooth, MKB/service metadata and historical amounts exactly as recorded.
4. GREIS `visits.comments` retained only as legacy visit-comment provenance unless later evidence proves a more specific semantic meaning.

GREIS finance remains out of scope for this phase. No debt, advance balance or patient-account movement is reconstructed here.
