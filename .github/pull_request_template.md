# Summary

Describe what changed and why.

## Type of change

- [ ] Feature
- [ ] Fix
- [ ] Refactor
- [ ] Docs

## Scope

- [ ] Auth
- [ ] Client module
- [ ] UI/UX
- [ ] API integration
- [ ] Architecture/infra

## What was implemented

List the main changes in bullet points.

## Architecture checks

- [ ] Layer boundaries respected (`Pages -> ViewModels -> Services -> Repositories -> API clients`)
- [ ] No direct API calls from Pages
- [ ] No sync-over-async in production paths
- [ ] Cancellation considered for screen-load async calls
- [ ] Fallback behavior (if changed) is explicit and logged

## UI/UX checks

- [ ] Loading state handled
- [ ] Error state handled
- [ ] Empty state handled
- [ ] Feedback messages are clear for users
- [ ] Shared components/styles reused when possible

## Validation

- [ ] `dotnet build Paynest.csproj -f net10.0-android` passed
- [ ] Tested manually in Android emulator
- [ ] No temporary/debug leftovers

## Risk and rollback

Potential risks:

- 

Rollback plan:

- 

## Notes for reviewers

Anything reviewers should pay special attention to.
