# Paynest Client Architecture

This document defines the coding conventions and boundaries for the MAUI client module.

## Layering

Follow this flow:

`Pages -> ViewModels -> Services -> Repositories -> API clients`

- `Pages`
  - UI composition and visual behavior only.
  - No business rules.
  - Forward user actions to ViewModel.
- `ViewModels`
  - Screen state and interaction orchestration.
  - Async loading, cancellation-aware updates, UI flags (`IsLoading`, `IsRefreshing`, etc.).
  - No direct HTTP calls.
- `Services`
  - Use-case oriented app logic.
  - Aggregates repository results for screen needs.
- `Repositories`
  - Data source orchestration (remote + fallback + cache).
  - No UI concerns.
- `API clients`
  - HTTP transport details only (timeouts, retries, serialization, auth headers).

## Async and Cancellation

- Prefer async APIs end-to-end for data flows.
- Avoid `.GetAwaiter().GetResult()` in production paths.
- Pages creating screen load requests must use `CancellationTokenSource` and cancel in `OnDisappearing`.
- Treat `OperationCanceledException` as expected control flow, not UI error.

## Data Access Rules

- Use repository interfaces from Services/ViewModels; never call API clients directly from UI.
- Keep fallback behavior explicit and logged.
- Cache strategy:
  - Repository-level in-memory cache with short TTL for list endpoints.
  - Invalidate related cache entries after mutating operations (e.g., pay installment).

## Error Handling

- Use typed/translated network messages for user-facing auth errors.
- Log fallback and transient failures with enough context (`endpoint`, `groupId`, `installmentId`).
- Do not expose raw exception details to end users.

## UI Conventions

- Reuse shared components:
  - `ErrorCardView`
  - `NetworkBannerView`
  - `EmptyStateView`
  - `StatusBadgeView`
- Reuse shared styles:
  - `AppPrimaryButton`
  - `AppAmountText`
  - `AppCard`
- Loading actions must prevent duplicate taps and show clear feedback (`Procesando...`, spinner).

## Dependency Injection

- Register dependencies in `MauiProgram` with clear ownership:
  - API client implementations
  - repositories
  - services
  - pages/viewmodels
- Keep constructor injection explicit; avoid service locator outside existing legacy boundaries.

## Naming

- Use consistent suffixes:
  - `...Page`, `...ViewModel`, `...Service`, `...Repository`, `...ApiClient`.
- Keep DTOs in API layer, domain/UI models in `Features/Client/Models`.

## Change Checklist

Before merging:

1. Build Android target successfully.
2. Verify no new sync-over-async calls.
3. Verify cancellation is wired for new screen loads.
4. Verify shared components/styles were reused when applicable.
5. Verify logs exist for fallback/transient failure paths.

## Git Workflow

- Branch naming:
  - Feature: `feature/<short-description>`
  - Fix: `fix/<short-description>`
  - Refactor: `refactor/<short-description>`
- Keep branch scope focused (one concern per PR when possible).
- Rebase or sync frequently with `main` to reduce integration conflicts.

## Pull Request Checklist

Before opening PR:

1. The feature works in Android emulator.
2. Build passes for target framework used in this project flow.
3. No debug leftovers (`TODO` without context, dead code, temporary logs).
4. UI strings and feedback messages are clear and user-facing.
5. Architectural boundaries were respected (no direct API calls from pages).
6. Any fallback/caching behavior changed is explained in PR description.

## Definition of Done

A task is done when:

1. Functional behavior is complete and manually validated.
2. Build is green.
3. Architecture and naming conventions were respected.
4. Loading, error, and empty states were considered.
5. Changes are documented (architecture notes or PR context) when relevant.
