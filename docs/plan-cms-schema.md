# Plan CMS

Super Admin can edit display content and pricing for **basic**, **standard**, **pro**, and **premium** membership plans.

Other plans (e.g. `scheme-report-onetime`) remain hardcoded in the frontend.

## Tables

- **`dbo.Plans`** (extended) — pricing + marketing fields; `Code` is the stable checkout key.
- **`dbo.PlanFeatures`** — ordered benefit bullets per plan (text, optional description, optional offering link).

## API

| Endpoint | Auth | Purpose |
|----------|------|---------|
| `GET /api/plans` | Public | Active CMS-managed plans for marketing pages |
| `GET /api/plans/{code}` | Public | Single plan + features |
| `GET /api/plans/admin` | Super Admin | All CMS-managed plans |
| `GET /api/plans/admin/{planId}` | Super Admin | Plan detail |
| `PUT /api/plans/admin/{planId}` | Super Admin | Update plan + features |
| `PATCH /api/plans/admin/{planId}/active` | Super Admin | Toggle active |

Run SQL manually — do not use EF migrations for this module.
