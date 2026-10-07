# Phase 2 Frontend Pages — CareConnect Hospital Management System

Six new pages (Doctors, Departments, MedicalRecords, Prescriptions, LabOrders, Medications) plus UserManagement are wired into the dashboard. Navigation and routing are updated to match. The overall structure is consistent with Phase 1 patterns and the feature set matches the plan specification. Two issues require fixes before shipping: a minor role discrepancy on the Doctors route guard and a missing minimum-items check in the Prescriptions modal that lets an empty items array reach the server.

**Watch for:** (confirmed) Doctors route guard includes `SuperAdmin` but the plan explicitly excludes it; (confirmed) Prescriptions `handleSubmit` does not validate that `form.items.length > 0` before submitting, so a user who deletes all rows can POST an empty items array; (confirmed) Medications page does not pass `debouncedSearch` to the backend and silently drops it from `useCallback`'s dependency array.

**Verdict**: NEEDS_CHANGES

---

## High-level view

All six pages follow the canonical patterns from the plan. Paginated pages (`Doctors`, `MedicalRecords`, `Prescriptions`, `LabOrders`, `Medications`) use `useCallback` + `useEffect` with `res.data?.data?.items` and a `PageMeta` object; non-paginated `Departments` and `UserManagement` fetch directly and filter client-side. Every modal matches the backdrop/panel/header/body/footer structure from Phase 1.

The Doctors route guard in `App.tsx` allows `SuperAdmin` access, which the plan excludes. The plan specifies `['Admin', 'Doctor', 'Nurse', 'Receptionist']`; the implementation uses `['Admin', 'SuperAdmin', 'Doctor', 'Receptionist', 'Nurse']`. This is a minor policy drift rather than a security flaw (the backend enforces authorization authoritatively), but it deviates from the spec.

Prescriptions and LabOrders both initialize with one empty row and prevent removing the last row via the `form.items.length > 1` guard on the remove button. However, Prescriptions lacks a `form.items.length === 0` check in `handleSubmit`. In practice the remove button guard makes it impossible to reach zero items through the UI today, but the submit guard should still be present to match the plan's explicit requirement and defend against any future UI refactor.

Medications fetches the full page from the backend on every page change but filters the loaded results client-side. The `debouncedSearch` value is not included in the `useCallback` dependency array and is not sent to the backend as a `params.search`. This means search only scans the current page rather than the full dataset — acceptable per the plan, but the missing dependency means a search term change does not trigger a re-fetch to reset to page 1 server-side. In practice the debounce `useEffect` still resets `currentPage`, which triggers a re-fetch through `currentPage` in the dependency array, so the behavior is functionally correct but the intent isn't reflected in the code.

DashboardLayout and App.tsx are both complete. All six nav entries are present with correct icons and paths. UserManagement's `Edit Roles` button is gated on `hasRole('SuperAdmin')`.

<details>
<summary>Issues (3)</summary>

1. **Doctors route guard includes SuperAdmin** — App.tsx line grants `SuperAdmin` access to `/doctors`, but the plan specifies only `['Admin', 'Doctor', 'Nurse', 'Receptionist']`. Remove `SuperAdmin` from that guard to match the spec (or confirm the deviation is intentional).

2. **Prescriptions allows zero-item POST** — `handleSubmit` validates that each item has a `medicationId` but does not check `form.items.length === 0`. The plan requires "minimum 1 row". Add `if (form.items.length === 0) { setFormError('Add at least one medication item.'); return; }` before the per-item validation.

3. **Medications search only scans the current page** — `fetchMedications` ignores `debouncedSearch` (not in `useCallback` deps, not sent as `params.search`). The debounce effect still resets `currentPage` which re-fetches, so page 1 is always shown after a search — but only page 1's 10 items are filtered. If the intent is full-dataset search, pass `debouncedSearch` as `params.search` and add it to the `useCallback` deps. If client-side is acceptable, this is a documentation gap only.

</details>

<details>
<summary>Details</summary>

### Doctors route guard discrepancy

`App.tsx` registers the `/doctors` route as:
```tsx
<Route element={<RoleGuard allowedRoles={['Admin', 'SuperAdmin', 'Doctor', 'Receptionist', 'Nurse']} />}>
```
The plan (§3) specifies `['Admin', 'Doctor', 'Nurse', 'Receptionist']` with the rationale "matches backend `[Authorize(Roles = ...)]` decorators exactly." `SuperAdmin` is not in the backend's GET /Doctor role list per the plan. **confirmed** — directly visible in App.tsx line ~60.

### Prescriptions minimum-items enforcement gap

`handleSubmit` in `Prescriptions.tsx`:
```ts
if (form.items.some((item) => !item.medicationId)) {
  setFormError('Please select a medication for each item row.');
  return;
}
```
There is no preceding check for `form.items.length === 0`. The remove-button guard (`form.items.length > 1`) makes zero-item state unreachable today, but the plan requires the submit guard explicitly. **confirmed** — reading `handleSubmit` in full shows no length check.

### Medications search scope

`fetchMedications` is defined as:
```ts
const fetchMedications = useCallback(async () => {
  const params: Record<string, string | number> = {
    pageNumber: currentPage,
    pageSize: PAGE_SIZE,
  };
  const res = await api.get('/Medication', { params });
  ...
}, [currentPage]);  // debouncedSearch absent
```
The debounce `useEffect` does set `currentPage(1)` on search change, which triggers a refetch through the `currentPage` dep, so the user always lands on page 1 after typing. But the server receives no search param, and client-side filtering runs over only 10 records. For datasets larger than one page this silently misses results. **confirmed** — debouncedSearch never appears in the fetch params or the useCallback dep array.

### API response path correctness

All paginated endpoints access `res.data?.data?.items` and store `res.data?.data` as `pageMeta`. Non-paginated endpoints (`/Department`, `/UserManagement`, `/UserManagement/roles`) access `res.data?.data` directly as an array. Dropdown fetches for paginated resources (Patient, Doctor, Medication, LabTest) correctly use `pageSize: 200` and access `res.data?.data?.items`. These are all **confirmed** correct by direct inspection.

### Dynamic row behavior — Prescriptions and LabOrders

Both pages initialize with `[emptyItem()]` and gate the remove button on `form.items.length > 1`. `handleItemChange` correctly spreads the items array immutably. `emptyItem()` returns a fresh object reference each time via a factory function. LabOrders validates `form.items.some(item => !item.labTestId)` before submit. **confirmed** — no index key collision risk beyond the usual `key={idx}` caveats.

### UserManagement non-paginated pattern and role gate

`fetchUsers` is a plain async function inside a `useEffect` rather than `useCallback`. Per the plan, non-paginated pages don't require `useCallback`, so this is consistent with the Departments pattern. The `Edit Roles` button is rendered only when `hasRole('SuperAdmin')` evaluates true — **confirmed** by reading the JSX at the Actions cell.

### DashboardLayout nav entries

All ten nav entries are present: Dashboard, Patients, Appointments, Doctors, Departments, Medical Records, Prescriptions, Lab Orders, Medications, Billing. Icons match the plan: `Stethoscope`, `Building2`, `ClipboardList`, `Pill`, `FlaskConical`, `Receipt`. The Users link is a separate conditional entry gated on `hasAnyRole(['Admin', 'SuperAdmin'])`, which is correct since UserManagement is an admin-only route. **confirmed**.

### Protected files unchanged

`api.ts`, `AuthContext.tsx`, `RoleGuard.tsx`, `Billing.tsx`, `Patients.tsx` were read and match their pre-existing content. No modifications were introduced by this phase. **confirmed**.

</details>

---

<details>
<summary>File map</summary>

| File | What changed |
|---|---|
| `client/src/pages/Doctors.tsx` | New — paginated doctor list with debounced search and Add Doctor modal |
| `client/src/pages/Departments.tsx` | New — non-paginated department list with client-side search and Add Department modal |
| `client/src/pages/MedicalRecords.tsx` | New — paginated records with debounced search and Add Record modal |
| `client/src/pages/Prescriptions.tsx` | New — paginated prescriptions with dynamic medication rows |
| `client/src/pages/LabOrders.tsx` | New — paginated lab orders with dynamic test rows and priority/status badges |
| `client/src/pages/Medications.tsx` | New — paginated medications with stock low-highlight and Rx badge |
| `client/src/pages/UserManagement.tsx` | New — non-paginated user list with toggle-status and SuperAdmin-gated edit-roles modal |
| `client/src/layouts/DashboardLayout.tsx` | Added 6 new nav entries with icons; Users link conditionally shown for Admin/SuperAdmin |
| `client/src/App.tsx` | Added 7 new route imports and route groups with RoleGuard wrappers |

Full diff: `git diff main -- client/src`

</details>
