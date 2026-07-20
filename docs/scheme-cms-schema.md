# Scheme CMS — Recommended Database Schema

Implement these tables manually. Do **not** run EF migrations or app-generated SQL for this module.

## Admin APIs (after tables exist)
- `GET/POST/PUT/DELETE /api/scheme-categories` (+ `/admin`)
- `GET/POST/PUT/PATCH/DELETE /api/schemes` (+ `/admin`, `{slug}`)

## Admin UI routes
- `/admin/scheme-management` — schemes CRUD
- `/admin/scheme-categories` — category master

Public pages `/schemes` and `/scheme/{slug}` load from these APIs only (no frontend static fallback).

Run in order:
1. `docs/scheme-cms-create-tables.sql`
2. `docs/scheme-cms-seed.sql` — seeds all 6 schemes with exact former static content

## Tables

### dbo.SchemeCategories
| Column | Type | Notes |
|--------|------|--------|
| SchemeCategoryId | int, identity, PK | |
| Slug | nvarchar(50), unique, required | lowercase kebab (e.g. `central`, `credit`) |
| Name | nvarchar(100), required | display name (e.g. Central Govt) |
| ShortDescription | nvarchar(500), null | optional |
| IconUrl | nvarchar(500), null | future |
| SortOrder | int, default 0 | display order |
| IsActive | bit, default 1 | |
| ShowInFilter | bit, default 1 | public filter tabs on `/schemes` |
| CreatedAt | datetime2, required | |
| UpdatedAt | datetime2, required | |

**Indexes:** unique on `Slug`; `(IsActive, SortOrder)` for public lists.

### dbo.Schemes
| Column | Type | Notes |
|--------|------|--------|
| SchemeId | int, identity, PK | |
| Slug | nvarchar(200), unique, required | public URL key |
| Name | nvarchar(300), required | page title / H1 |
| Crumb | nvarchar(200) | breadcrumb label |
| Tagline | nvarchar(500) | subtitle |
| ShortDescription | nvarchar(2000) | listing / home card copy |
| ContentHtml | nvarchar(max) | detail page body |
| CategorySlug | nvarchar(50), required | references SchemeCategories.Slug (logical) |
| PrimaryBadgeText | nvarchar(100) | listing card badge 1 — **always category name** (set from CategorySlug on save) |
| PrimaryBadgeClass | nvarchar(50) | e.g. `badge-green` |
| SecondaryBadgeText | nvarchar(100) | listing card badge 2 |
| SecondaryBadgeClass | nvarchar(50) | e.g. `badge-orange` |
| HomeTitle | nvarchar(200) | homepage card title |
| HomeBadgeText | nvarchar(100) | homepage card badge label |
| HomeBadgeClass | nvarchar(50) | homepage card badge class |
| HomeDescription | nvarchar(2000) | homepage card paragraph (separate from ShortDescription) |
| SeoTitle | nvarchar(500), null | |
| MetaDescription | nvarchar(1000), null | |
| IsPublished | bit | |
| IsFeatured | bit | show on home schemes grid |
| SortOrder | int | listing order (lower first); 0 = latest-created style fallback via CreatedAt |
| CreatedAt | datetime2 | |
| UpdatedAt | datetime2 | |
| CreatedByUserId | uniqueidentifier, null | |

**Indexes:** unique on `Slug`; `(IsPublished, SortOrder, CreatedAt)`; `(CategorySlug)`; `(IsFeatured)`.

### dbo.SchemeBenefits
| Column | Type | Notes |
|--------|------|--------|
| SchemeBenefitId | int, identity, PK | |
| SchemeId | int, FK → Schemes.SchemeId ON DELETE CASCADE | |
| Text | nvarchar(1000), required | detail sidebar "Key Benefits" |
| SortOrder | int | |

**Indexes:** `(SchemeId, SortOrder)`.

### dbo.SchemeCardHighlights
| Column | Type | Notes |
|--------|------|--------|
| SchemeCardHighlightId | int, identity, PK | |
| SchemeId | int, FK → Schemes.SchemeId ON DELETE CASCADE | |
| Text | nvarchar(1000), required | listing card bullets |
| SortOrder | int | |

**Indexes:** `(SchemeId, SortOrder)`.

## Relationships
- Schemes.CategorySlug → SchemeCategories.Slug (application-enforced; cascade slug rename)
- SchemeBenefits / SchemeCardHighlights → Schemes (FK + cascade delete)

## Seed suggestion (manual)
Run `docs/scheme-cms-seed.sql` after creating tables. It inserts the four filter categories and six published schemes with the exact content previously in `schemes.data.ts`.
