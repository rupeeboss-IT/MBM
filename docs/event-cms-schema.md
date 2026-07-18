# Event CMS — Recommended Database Schema

Implement these tables manually. Do **not** run EF migrations or app-generated SQL for this module.

## Admin APIs (after tables exist)
- `GET/POST/PUT/DELETE /api/event-categories` (+ `/admin`)
- `GET/POST/PUT/DELETE /api/event-cities` (+ `/admin`)
- `GET/POST/PUT/PATCH/DELETE /api/events` (+ `/admin`, `upload-image`, `{slug}`)

## Admin UI routes
- `/admin/event-management` — events CRUD
- `/admin/event-categories` — category master
- `/admin/event-cities` — city master (future-ready)

Public pages `/events` and `/event/{slug}` load from these APIs (with static fallback until data is published).

## Tables

### dbo.EventCategories
| Column | Type | Notes |
|--------|------|--------|
| EventCategoryId | int, identity, PK | |
| Slug | nvarchar(50), unique, required | lowercase kebab |
| Name | nvarchar(100), required | display name |
| ShortDescription | nvarchar(500), null | optional |
| IconUrl | nvarchar(500), null | future icon support |
| SortOrder | int, default 0 | display order |
| IsActive | bit, default 1 | |
| ShowInFilter | bit, default 1 | public filter tabs |
| CreatedAt | datetime2, required | |
| UpdatedAt | datetime2, required | |

**Indexes:** unique on `Slug`; `(IsActive, SortOrder)` for public lists.

### dbo.EventCities
| Column | Type | Notes |
|--------|------|--------|
| EventCityId | int, identity, PK | |
| Slug | nvarchar(50), unique, required | |
| Name | nvarchar(100), required | |
| BadgeClass | nvarchar(50), null | e.g. `badge-green` |
| SortOrder | int | |
| IsActive | bit | |
| CreatedAt | datetime2 | |
| UpdatedAt | datetime2 | |

**Indexes:** unique on `Slug`; `(IsActive, SortOrder)`.

### dbo.Events
| Column | Type | Notes |
|--------|------|--------|
| EventId | int, identity, PK | |
| Slug | nvarchar(200), unique, required | public URL key |
| Name | nvarchar(300), required | page title / H1 |
| Crumb | nvarchar(200) | breadcrumb label |
| Tagline | nvarchar(500) | subtitle |
| ShortDescription | nvarchar(2000) | listing card copy |
| AboutHtml | nvarchar(max) | optional About section |
| HighlightsHtml | nvarchar(max) | optional rich highlights (if no bullet rows) |
| AssociationHtml | nvarchar(max) | optional rich "In Association With" (if no partner rows) |
| CategorySlug | nvarchar(50), required | references EventCategories.Slug (logical) |
| CitySlug | nvarchar(50), null | references EventCities.Slug (logical) |
| FeaturedImageUrl | nvarchar(500), null | |
| BannerImageUrl | nvarchar(500), null | future |
| ThumbnailUrl | nvarchar(500), null | future |
| DateDisplayText | nvarchar(300) | flexible date label |
| TimeDisplayText | nvarchar(300) | flexible time label |
| StartDate | datetime2, null | filter: upcoming / past / ongoing |
| EndDate | datetime2, null | |
| DateISO | nvarchar(30), null | schema.org |
| AttendanceMode | nvarchar(20) | Online / Offline / Hybrid / empty |
| LocationDisplayText | nvarchar(500) | card location line |
| VenueName | nvarchar(300) | |
| VenueAddress | nvarchar(500) | |
| Landmark | nvarchar(200) | |
| CityName | nvarchar(100) | denormalized city label |
| State | nvarchar(100) | |
| Country | nvarchar(100) | default India |
| MapsUrl | nvarchar(1000), null | future |
| Latitude | decimal(9,6), null | future |
| Longitude | decimal(9,6), null | future |
| PriceDisplay | nvarchar(100) | e.g. fee text |
| RegNote | nvarchar(1000) | register sidebar note |
| SeoTitle | nvarchar(500), null | |
| MetaDescription | nvarchar(1000), null | |
| IsPublished | bit | |
| IsFeatured | bit | future featured filter |
| SortOrder | int | |
| CreatedAt | datetime2 | |
| UpdatedAt | datetime2 | |
| CreatedByUserId | uniqueidentifier, null | |

**Indexes:** unique on `Slug`; `(IsPublished, SortOrder, StartDate)`; `(CategorySlug)`; `(CitySlug)`; `(IsFeatured)`.

**Future columns (add later without redesign):** RegistrationEnabled, RegistrationUrl, Capacity, WaitingListEnabled, RegistrationClosesAt, TicketPrice, IsPaid, LiveStreamUrl, MeetingUrl, ViewCount, SeriesId, etc.

### dbo.EventHighlights
| Column | Type | Notes |
|--------|------|--------|
| EventHighlightId | int, identity, PK | |
| EventId | int, FK → Events.EventId ON DELETE CASCADE | |
| Text | nvarchar(1000), required | |
| SortOrder | int | |

**Indexes:** `(EventId, SortOrder)`.

### dbo.EventPartners
| Column | Type | Notes |
|--------|------|--------|
| EventPartnerId | int, identity, PK | |
| EventId | int, FK → Events.EventId ON DELETE CASCADE | |
| Name | nvarchar(200), required | |
| LogoUrl | nvarchar(500), null | |
| WebsiteUrl | nvarchar(500), null | |
| SortOrder | int | |

**Indexes:** `(EventId, SortOrder)`.

## Relationships
- Events.CategorySlug → EventCategories.Slug (application-enforced; cascade slug rename like blogs)
- Events.CitySlug → EventCities.Slug (application-enforced)
- EventHighlights / EventPartners → Events (FK + cascade delete)

## Seed suggestion (manual)
Create active categories (e.g. BLL, Others) and cities (e.g. Mumbai) via Admin UI after tables exist. Do not hardcode seeds in application code for production.
