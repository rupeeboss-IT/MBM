# Team CMS — Recommended Database Schema

Implement these tables manually. Do **not** run EF migrations or app-generated SQL for this module.

## Admin APIs (after tables exist)
- `GET/POST/PUT/PATCH/DELETE /api/team-members` (+ `/admin`, `{id}`, `/admin/upload-photo`)

## Admin UI routes
- `/admin/team-management` — leadership team CRUD on `/about`

Public `/about` loads active team members from the API only (no frontend static fallback).

Run in order:
1. `docs/team-cms-create-tables.sql`
2. `docs/team-cms-seed.sql` — seeds all 11 leadership team members from former static `about.html`

## Table

### dbo.TeamMembers
| Column | Type | Notes |
|--------|------|--------|
| TeamMemberId | int, identity, PK | |
| Name | nvarchar(200), required | card heading |
| DesignationHtml | nvarchar(2000) | role / bio (may include `<br />`) |
| PhotoUrl | nvarchar(500), required | e.g. `/team/NBShetty.jpg` or `/uploads/team/…` |
| SortOrder | int, default 0 | display order on About page |
| IsActive | bit, default 1 | inactive members hidden on public site |
| CreatedAt | datetime2, required | |
| UpdatedAt | datetime2, required | |
| CreatedByUserId | uniqueidentifier, null | |

**Indexes:** `(IsActive, SortOrder)` for public lists.
