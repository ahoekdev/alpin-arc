# Tours and Tour Variants API Plan

## Goal

Add tours as containers for tour variants, and add ordered tour variant stages that reference existing canonical stages.

Example:

- Tour: `Haute Route`
- Tour variant: `Normal`
- Tour variant: `Alpine`
- Tour variant stages: ordered references to existing `Stage` rows

## Entities

### Tour

Fields:

- `Id`
- `Name`
- `CreatedAt`

Rules:

- `Name` is required.
- `Name` is globally unique.
- Uniqueness is case-insensitive, so `Haute Route` and `haute route` conflict.
- A tour can exist without variants.

### TourVariant

Fields:

- `Id`
- `TourId`
- `Tour`
- `Name`
- `CreatedAt`

Rules:

- `TourId` is required.
- `Name` is required.
- Variant names are unique within a tour.
- Variant name uniqueness is case-insensitive, so `Normal` and `normal` conflict within the same tour.
- The same variant name can be reused under different tours.
- A tour variant can exist without stages.
- Updates can change both `TourId` and `Name`.

### TourVariantStage

Fields:

- `Id`
- `TourVariantId`
- `TourVariant`
- `StageId`
- `Stage`
- `Order`
- `CreatedAt`

Rules:

- `TourVariantId` is required.
- `StageId` is required.
- `Order` is required and one-based, so it must be `>= 1`.
- `(TourVariantId, Order)` is unique.
- The same `Stage` can appear more than once in the same `TourVariant`, as long as each occurrence has a different `Order`.
- Tour variant stages always use live `Stage` data. They do not snapshot duration, distance, lodges, or other stage fields.
- No custom per-tour-stage override fields are included in the first implementation.
- Updates can change `TourVariantId`, `StageId`, and `Order`.
- Moving a row to an occupied order returns `409 Conflict`; the API will not auto-swap or resequence rows.

## Delete Behavior

- Deleting a `Tour` cascades to its `TourVariant` rows.
- Deleting a `TourVariant` cascades to its `TourVariantStage` rows.
- Deleting a `Stage` is restricted while any `TourVariantStage` references it.

## DTOs

### TourRequestDto

- `Name`

### TourResponseDto

- `Id`
- `Name`
- `CreatedAt`

### TourSummaryDto

- `Id`
- `Name`

### TourVariantRequestDto

- `TourId`
- `Name`

### TourVariantResponseDto

- `Id`
- `Tour`
- `Name`
- `CreatedAt`

`Tour` should use `TourSummaryDto`.

### TourVariantSummaryDto

- `Id`
- `TourId`
- `Name`

### TourVariantStageRequestDto

- `TourVariantId`
- `StageId`
- `Order`

### TourVariantStageResponseDto

- `Id`
- `TourVariant`
- `Stage`
- `Order`
- `CreatedAt`

`TourVariant` should use `TourVariantSummaryDto`.

`Stage` should include full live stage details equivalent to the current `StageResponseDto`, including:

- `Id`
- `StartLodge`
- `EndLodge`
- `DurationMinutes`
- `DistanceMeters`
- `CreatedAt`

## Endpoints

### Tours

`GET /api/tours`

- Returns all tours.
- Ordered by `Name`.

`GET /api/tours/{id}`

- Returns one tour.
- Returns `404 NotFound` when the tour does not exist.

`POST /api/tours`

- Creates a tour.
- Returns `409 Conflict` when the case-insensitive name already exists.

`PUT /api/tours/{id}`

- Updates a tour name.
- Returns `404 NotFound` when the tour does not exist.
- Returns `409 Conflict` when the case-insensitive name already exists.

`DELETE /api/tours/{id}`

- Deletes a tour.
- Cascades to variants and variant stages.
- Returns `404 NotFound` when the tour does not exist.

`GET /api/tours/{id}/variants`

- Returns variants for a tour.
- Ordered by `Name`.
- Returns `404 NotFound` when the tour does not exist.
- Returns `200 OK` with `[]` when the tour exists but has no variants.

### Tour Variants

`GET /api/tour-variants/{id}`

- Returns one tour variant.
- Returns `404 NotFound` when the variant does not exist.

`POST /api/tour-variants`

- Creates a tour variant.
- Request body includes `TourId` and `Name`.
- Returns `400 BadRequest` when the referenced tour does not exist.
- Returns `409 Conflict` when the case-insensitive variant name already exists within the tour.

`PUT /api/tour-variants/{id}`

- Updates `TourId` and `Name`.
- Returns `404 NotFound` when the variant does not exist.
- Returns `400 BadRequest` when the referenced tour does not exist.
- Returns `409 Conflict` when the case-insensitive variant name already exists within the target tour.

`DELETE /api/tour-variants/{id}`

- Deletes a tour variant.
- Cascades to tour variant stages.
- Returns `404 NotFound` when the variant does not exist.

`GET /api/tour-variants/{id}/stages`

- Returns ordered tour variant stages for one variant.
- Ordered by `Order`.
- Includes full live stage details.
- Returns `404 NotFound` when the variant does not exist.
- Returns `200 OK` with `[]` when the variant exists but has no stages.

No global `GET /api/tour-variants` endpoint is planned for the first implementation because variants are fetched in the context of a tour or as a single entity.

### Tour Variant Stages

`POST /api/tour-variant-stages`

- Creates an ordered stage entry.
- Request body includes `TourVariantId`, `StageId`, and `Order`.
- Returns `400 BadRequest` when the referenced tour variant or stage does not exist.
- Returns `409 Conflict` when `(TourVariantId, Order)` already exists.

`PUT /api/tour-variant-stages/{id}`

- Updates `TourVariantId`, `StageId`, and `Order`.
- Returns `404 NotFound` when the tour variant stage row does not exist.
- Returns `400 BadRequest` when the referenced tour variant or stage does not exist.
- Returns `409 Conflict` when `(TourVariantId, Order)` already exists for another row.

`DELETE /api/tour-variant-stages/{id}`

- Deletes one ordered stage entry.
- Returns `404 NotFound` when the row does not exist.

No global `GET /api/tour-variant-stages` endpoint is planned for the first implementation.

No direct `GET /api/tour-variant-stages/{id}` endpoint is planned for the first implementation because it has no current use case.

## Implementation Scope

Add API-only changes under `apps/api`:

- Models: `Tour`, `TourVariant`, `TourVariantStage`
- `DbSet` properties and EF Core configuration in `ApplicationDbContext`
- DTO files for tours, tour variants, and tour variant stages
- Controllers for the agreed endpoints
- EF Core migration for the new tables, constraints, indexes, and delete behavior

## Out of Scope For First Implementation

- Web app changes
- Nested mutation endpoints
- Global tour variant list endpoint
- Global tour variant stage list endpoint
- Direct tour variant stage detail endpoint
- Automatic order assignment
- Automatic resequencing, swapping, or batch reorder endpoint
- Stored derived totals for tour variants
- Snapshotting stage data onto tour variant stages
- Per-tour-stage override fields such as custom name, notes, day label, ascent, descent, difficulty, or overnight lodge
