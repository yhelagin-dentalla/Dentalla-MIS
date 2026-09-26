# GREIS Phase 6 — actual receipts aggregate

Date: 2026-09-26

## Decision

GREIS financial history is **not** reconstructed as old cash movements, advances, debts, patient-account movements, or payment allocations.

The only normalized historical fact from GREIS finance is one source-aware aggregate per canonical Patient: total actual receipts attributable to that patient for the GREIS period.

Calculation code: `GREIS_ACTUAL_RECEIPTS_V1`.

## Audited source facts

Phase 6A1/6A2 established:

- `visits_payments`: 12,652 rows, 11,606 visits, total 111,706,999.00 RUB.
- Every `visits_payments` row resolves to a GREIS visit and canonical Dentalla Patient.
- For all 11,606 detailed-payment visits, payment detail reconciles exactly to `visits.sum_paid` and `sum_paid_type0..4`; mismatch count is zero.
- 513 legacy visits have no `visits_payments` rows but do have `sum_paid_final`; their total is 2,567,091.00 RUB.
- `patients_money_transfers`: 4,254 rows; all resolve to canonical Patients.
- `pay_type_id=0` means internal patient-account usage and must not be counted as new external money.
- External account movements are transfer type 4 (`Внесение/возврат наличных`) and type 8 (`Внесение/возврат Терминал`). Their net total is 3,067,353.00 RUB.
- doctor 182 technical advance visits contain external cash/terminal payments, but no row matched transfer type 4/8 by patient + channel + amount in the same day or within ±3 days. No proven duplicate set exists.
- certificate usage, debt write-off, and patient-to-patient transfers are internal/account facts and are excluded from actual receipts.

## Formula v1

Include:

1. `greis_raw.visits_payments.paym_s` where `pay_type_id IN (1,3,4)`:
   - 1 cash;
   - 3 legal entity;
   - 4 terminal.
2. For legacy visits with no `visits_payments` rows, use `visits.sum_paid_final` only when `pay_type_id IN (1,3,4)`.
3. Add net `patients_money_transfers.ss` where `transfer_type_id IN (4,8)`.

Exclude:

- `pay_type_id=0` internal-account usage;
- certificate usage;
- debt write-off;
- patient-to-patient transfer;
- legacy debt balances;
- legacy advance balances;
- old payment allocations.

## Control total

| Component | Amount RUB |
| --- | ---: |
| Detailed external visit payments | 88,245,768.50 |
| Legacy no-detail fallback | 2,567,091.00 |
| External account deposits/refunds | 3,067,353.00 |
| **Total** | **93,880,212.50** |

## Target model

`finance.PatientHistoricalReceiptTotals` stores one aggregate row per canonical Patient / source / calculation version. It is a historical aggregate, not a current patient balance and not a cash ledger.

`migration.GreisPatientReceiptAudit` stores source component amounts and row counts for reconciliation.

Phase 6B SQL: `scripts/migration/greis_phase6b_import_actual_receipts.sql`.

## EF synchronization

The SQL import is intentionally run and reconciled first. After the real database result is confirmed, the final finance aggregate entity, DbContext mapping, EF migration, and generated model snapshot are synchronized in the Phase 6 branch before merge to `main`.
