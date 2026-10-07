# Phase 2 Implementation Plan — CareConnect Frontend

## Context

**Project stack:** React 19 + TypeScript + Vite + Tailwind CSS v4 + Axios + React Router v7 + Lucide-React  
**Build command:** `npm run build` (runs `tsc -b && vite build`)  
**Dev command:** `npm run dev`  
**Lint:** `npm run lint` (oxlint)  
**All files live under:** `c:\Users\user\Desktop\Hospital Managment System\client\src\`

Phase 1 delivered: `Dashboard`, `Patients`, `Appointments`, `Billing` pages.  
Phase 2 delivers: `Doctors`, `Departments`, `MedicalRecords`, `Prescriptions`, `LabOrders`, `Medications` pages.

---

## 1. Canonical Patterns (extracted from existing code)

### 1.1 Paginated API Call Pattern

Every paginated page follows this exact sequence — replicate it verbatim:

```
const PAGE_SIZE = 10;

const [items, setItems]           = useState<T[]>([]);
const [pageMeta, setPageMeta]     = useState<PageMeta | null>(null);
const [currentPage, setCurrentPage] = useState(1);
const [loading, setLoading]       = useState(true);

const fetchItems = useCallback(async () => {
  setLoading(true);
  try {
    const params: Record<string, string | number> = {
      pageNumber: currentPage,
      pageSize: PAGE_SIZE,
    };
    if (debouncedSearch) params.search = debouncedSearch;

    const res = await api.get('/Endpoint', { params });
    const paged = res.data?.data;          // ApiResponse<PagedResponse<T>>
    setItems(paged?.items ?? []);          // paged.items — lowercase 'i'
    setPageMeta(paged ?? null);
  } catch (err) {
    console.error('Failed to fetch:', err);
  } finally {
    setLoading(false);
  }
}, [currentPage, debouncedSearch]);

useEffect(() => { fetchItems(); }, [fetchItems]);
```

`PageMeta` interface (copy-paste for every paginated page):
```ts
interface PageMeta {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
```

Pagination footer — only renders when `pageMeta.totalPages > 1`:
```tsx
{pageMeta && pageMeta.totalPages > 1 && (
  <div className="px-6 py-4 border-t border-gray-100 dark:border-gray-700 flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 bg-gray-50/50 dark:bg-gray-800/50">
    <span>Page {pageMeta.pageNumber} of {pageMeta.totalPages} &nbsp;·&nbsp; {pageMeta.totalCount} total</span>
    <div className="flex items-center gap-2">
      <button onClick={() => setCurrentPage(p => p - 1)} disabled={!pageMeta.hasPreviousPage}
        className="p-1.5 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors" aria-label="Previous page">
        <ChevronLeft className="w-4 h-4" />
      </button>
      <button onClick={() => setCurrentPage(p => p + 1)} disabled={!pageMeta.hasNextPage}
        className="p-1.5 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors" aria-label="Next page">
        <ChevronRight className="w-4 h-4" />
      </button>
    </div>
  </div>
)}
```

### 1.2 Non-Paginated API Call Pattern

Used for dropdown data (patients, doctors, departments, medications, lab tests) inside modals. Response shape is `ApiResponse<IEnumerable<T>>` — items live at `res.data?.data` (a plain array, not a paged object).

```ts
const [options, setOptions] = useState<OptionType[]>([]);
// In a loader function:
const res = await api.get('/Department');
setOptions(res.data?.data ?? []);   // res.data.data is the array directly
```

For the Appointments modal the existing code already uses the paged endpoint with `pageSize: 200` to get all patients/doctors in one call:
```ts
api.get('/Patient', { params: { pageNumber: 1, pageSize: 200 } })
```
Use the same approach for any dropdown that hits a paginated endpoint (Doctor, Medication, LabTest). Use the non-paged pattern for Department (its `GET /Department` returns a plain array, not a paged response).

### 1.3 Modal Open/Close Pattern

Exact pattern from `Patients.tsx` and `Appointments.tsx`:

```ts
const [showModal, setShowModal]   = useState(false);
const [form, setForm]             = useState(emptyForm);
const [submitting, setSubmitting] = useState(false);
const [formError, setFormError]   = useState<string | null>(null);

const openModal = () => {
  setForm(emptyForm());
  setFormError(null);
  setShowModal(true);
};

const closeModal = () => {
  setShowModal(false);
  setFormError(null);
};

const handleFieldChange = (
  e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
) => {
  const { name, value } = e.target;
  setForm(prev => ({ ...prev, [name]: value }));
};

const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  setFormError(null);
  setSubmitting(true);
  try {
    await api.post('/Endpoint', form);
    closeModal();
    setCurrentPage(1);
    fetchItems();
  } catch (err: any) {
    const msg = err.response?.data?.message || err.response?.data?.title || 'Failed to save.';
    setFormError(msg);
  } finally {
    setSubmitting(false);
  }
};
```

Modal backdrop + container:
```tsx
{showModal && (
  <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4"
    role="dialog" aria-modal="true" aria-labelledby="modal-title">
    <div className="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
      {/* header with <X> close button */}
      {/* form body */}
    </div>
  </div>
)}
```

For modals that need dropdown data loaded on open, use the `Appointments.tsx` pattern:
- `async openModal()` calls `loadDropdowns()` before `setShowModal(true)`.
- `loadDropdowns()` guards with early return if already populated.
- Shows `<Loader2>` spinner inside the modal while `dropdownsLoading` is true.

### 1.4 Search Debounce Pattern

Exact pattern from `Patients.tsx` and `Appointments.tsx`:

```ts
const [searchTerm, setSearchTerm]         = useState('');
const [debouncedSearch, setDebouncedSearch] = useState('');

useEffect(() => {
  const timer = setTimeout(() => {
    setDebouncedSearch(searchTerm);
    setCurrentPage(1);   // always reset to page 1 on new search
  }, 400);
  return () => clearTimeout(timer);
}, [searchTerm]);
```

The `debouncedSearch` value is passed as `params.search` in `fetchItems`. Client-side filtering of the loaded page (as in `Appointments.tsx`) is acceptable when the backend does not support a `search` query param; use server-side `params.search` when the endpoint supports it (as in `Patients.tsx`).

### 1.5 Dynamic List Item (Add/Remove Rows) Pattern

No existing page uses dynamic row lists. The `Prescriptions`, `LabOrders`, and `Billing` create flows all require them (prescription items, lab order items, invoice items). Design consistent with existing style:

```ts
// Each row is a typed object in an array inside form state
interface PrescriptionItemRow {
  medicationId: string;
  dosage: string;
  frequency: string;
  durationInDays: number;
  quantity: number;
  instructions: string;
}

const emptyItem = (): PrescriptionItemRow => ({
  medicationId: '', dosage: '', frequency: '', durationInDays: 1, quantity: 1, instructions: '',
});

// In component:
const [form, setForm] = useState({ ...otherFields, items: [emptyItem()] });

const addRow = () =>
  setForm(prev => ({ ...prev, items: [...prev.items, emptyItem()] }));

const removeRow = (idx: number) =>
  setForm(prev => ({ ...prev, items: prev.items.filter((_, i) => i !== idx) }));

const handleItemChange = (
  idx: number,
  e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
) => {
  const { name, value } = e.target;
  setForm(prev => {
    const items = [...prev.items];
    items[idx] = { ...items[idx], [name]: value };
    return { ...prev, items };
  });
};
```

Render pattern for each row (inside the modal form):
```tsx
{form.items.map((item, idx) => (
  <div key={idx} className="grid grid-cols-[1fr_auto] gap-2 items-start border border-gray-200 dark:border-gray-700 rounded-lg p-3">
    {/* row fields */}
    {form.items.length > 1 && (
      <button type="button" onClick={() => removeRow(idx)}
        className="p-1.5 text-gray-400 hover:text-red-500 rounded transition-colors mt-1"
        aria-label="Remove item">
        <X className="w-4 h-4" />
      </button>
    )}
  </div>
))}
<button type="button" onClick={addRow}
  className="inline-flex items-center gap-1.5 text-sm text-blue-600 hover:text-blue-700 font-medium">
  <Plus className="w-4 h-4" /> Add Item
</button>
```

---

## 2. Exact Sidebar Nav Entries to Add — `DashboardLayout.tsx`

### New imports to add
```ts
import { Stethoscope, Building2, ClipboardList, Pill, FlaskConical, Receipt } from 'lucide-react';
```
_(Receipt is for Billing which already exists — only import the new icons.)_

### Updated `navItems` array
Replace the existing `navItems` array with:
```ts
const navItems = [
  { to: '/',              label: 'Dashboard',       icon: LayoutDashboard },
  { to: '/patients',      label: 'Patients',        icon: Users           },
  { to: '/appointments',  label: 'Appointments',    icon: Activity        },
  { to: '/doctors',       label: 'Doctors',         icon: Stethoscope     },
  { to: '/departments',   label: 'Departments',     icon: Building2       },
  { to: '/medical-records', label: 'Medical Records', icon: ClipboardList },
  { to: '/prescriptions', label: 'Prescriptions',   icon: Pill            },
  { to: '/lab-orders',    label: 'Lab Orders',      icon: FlaskConical    },
  { to: '/medications',   label: 'Medications',     icon: Receipt         },
  { to: '/billing',       label: 'Billing',         icon: FileText        },
];
```
Note: `Stethoscope` from lucide-react is already imported in `Appointments.tsx`; it is not yet imported in `DashboardLayout.tsx`. `Building2`, `ClipboardList`, `Pill`, `FlaskConical`, `Receipt` are all new imports for this file.

---

## 3. Exact Routes to Add — `App.tsx`

### New page imports
```ts
import Doctors        from './pages/Doctors';
import Departments    from './pages/Departments';
import MedicalRecords from './pages/MedicalRecords';
import Prescriptions  from './pages/Prescriptions';
import LabOrders      from './pages/LabOrders';
import Medications    from './pages/Medications';
```

### New `<Route>` entries (add inside the `/` layout route, after existing routes)
```tsx
<Route element={<RoleGuard allowedRoles={['Admin', 'Doctor', 'Nurse', 'Receptionist']} />}>
  <Route path="doctors" element={<Doctors />} />
</Route>

<Route element={<RoleGuard allowedRoles={['Admin', 'Doctor', 'Nurse', 'Receptionist', 'SuperAdmin']} />}>
  <Route path="departments" element={<Departments />} />
</Route>

<Route element={<RoleGuard allowedRoles={['Admin', 'Doctor', 'Nurse', 'SuperAdmin']} />}>
  <Route path="medical-records" element={<MedicalRecords />} />
</Route>

<Route element={<RoleGuard allowedRoles={['Admin', 'Doctor', 'Nurse', 'Pharmacist', 'SuperAdmin']} />}>
  <Route path="prescriptions" element={<Prescriptions />} />
</Route>

<Route element={<RoleGuard allowedRoles={['Admin', 'Doctor', 'Nurse', 'LabTechnician', 'SuperAdmin']} />}>
  <Route path="lab-orders" element={<LabOrders />} />
</Route>

<Route element={<RoleGuard allowedRoles={['Admin', 'Doctor', 'Nurse', 'Pharmacist', 'Receptionist', 'LabTechnician', 'Radiologist', 'Cashier', 'Accountant', 'SuperAdmin']} />}>
  <Route path="medications" element={<Medications />} />
</Route>
```

Role selection rationale: matches backend `[Authorize(Roles = ...)]` decorators exactly as found in the controllers. Frontend guards are for UX only; the backend enforces authorization authoritatively.

---

## 4. Per-Page Implementation Notes

### 4.1 Doctors (`src/pages/Doctors.tsx`)

**API endpoint:** `GET /Doctor` — paginated, `ApiResponse<PagedResponse<DoctorDto>>`  
**Create:** `POST /Doctor` — `CreateDoctorDto`  
**Allowed roles (backend):** GET: all staff + patient; POST: AdminAndAbove only

**TypeScript interface:**
```ts
interface Doctor {
  id: string;
  firstName: string;
  lastName: string;
  specialization: string;
  licenseNumber: string;
  yearsOfExperience: number;
  contactNumber: string;
  bio: string;
  consultationFee: number;
  profilePictureUrl: string;
  departmentId: string;
  departmentName: string;
}
```

**Table columns:** Name (avatar initials), Specialization, Department, Experience, Fee, Contact  
**Search:** client-side filter on loaded page by `firstName+lastName` and `specialization` (Appointments.tsx pattern — backend search param optional)  
**Dropdown needed in create modal:** Departments (non-paginated endpoint `GET /Department` → `res.data?.data` is the array directly)

**`emptyForm`:**
```ts
{
  firstName: '', lastName: '', specialization: '', licenseNumber: '',
  yearsOfExperience: 0, contactNumber: '', bio: '', consultationFee: 0,
  profilePictureUrl: '', departmentId: '',
}
```

**Modal fields:** First Name, Last Name, Specialization, License Number, Years of Experience (number input), Contact Number, Bio (textarea, optional), Consultation Fee (number input), Department (select from dropdown), Profile Picture URL (text input, optional)

**Fee display:** `$consultationFee.toFixed(2)` — use emerald badge  
**Avatar:** initials circle in blue-100 background: `{d.firstName[0]}{d.lastName[0]}`

---

### 4.2 Departments (`src/pages/Departments.tsx`)

**API endpoint:** `GET /Department` — **non-paginated**, `ApiResponse<IEnumerable<DepartmentDto>>` — data is at `res.data?.data` (plain array)  
**Create:** `POST /Department` — `CreateDepartmentDto { name, description }`  
**Backend allowed roles:** GET: AnyStaff + Patient; POST: AdminAndAbove

Because there is no pagination, this page does NOT use `PageMeta`, `currentPage`, `useCallback`, or pagination footer. Use a simple `useEffect` with `useState` (Dashboard.tsx pattern):

```ts
const [departments, setDepartments] = useState<Department[]>([]);
const [loading, setLoading] = useState(true);

useEffect(() => {
  const fetch = async () => {
    setLoading(true);
    try {
      const res = await api.get('/Department');
      setDepartments(res.data?.data ?? []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };
  fetch();
}, []);
```

After a successful create, call the fetch function again (refetch without pagination reset).

**TypeScript interface:**
```ts
interface Department {
  id: string;
  name: string;
  description: string;
  createdDate: string;
}
```

**Table columns:** Name, Description, Created Date  
**Search:** client-side filter (no debounce needed for small list)  
```ts
const filtered = searchTerm
  ? departments.filter(d =>
      d.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      d.description.toLowerCase().includes(searchTerm.toLowerCase()))
  : departments;
```

**`emptyForm`:** `{ name: '', description: '' }`  
**Modal fields:** Name (required), Description (textarea)

---

### 4.3 Medical Records (`src/pages/MedicalRecords.tsx`)

**API endpoint:** `GET /MedicalRecord` — paginated  
**Create:** `POST /MedicalRecord` — `CreateMedicalRecordDto`  
**Backend allowed roles:** GET: SuperAdmin, Admin, Doctor, Nurse; POST: SuperAdmin, Admin, Doctor

**TypeScript interface:**
```ts
interface MedicalRecord {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  appointmentId: string | null;
  recordDate: string;
  diagnosis: string;
  symptoms: string;
  treatment: string;
  prescription: string;
  notes: string;
}
```

**Table columns:** Patient (avatar initials), Doctor, Record Date, Diagnosis (truncated), Symptoms (truncated)  
**Search:** client-side filter by patientName, doctorName, diagnosis  
**Dropdowns needed in create modal:** Patients (`pageSize: 200`), Doctors (`pageSize: 200`)  
Use `Appointments.tsx` `loadDropdowns()` pattern with `dropdownsLoading` state.

**`emptyForm`:**
```ts
{
  patientId: '', doctorId: '', appointmentId: '',
  diagnosis: '', symptoms: '', treatment: '',
  prescription: '', notes: '',
}
```

**Modal fields:** Patient (select), Doctor (select), Appointment ID (optional text — user may leave blank; send `null` when empty), Diagnosis (required textarea), Symptoms (textarea), Treatment (textarea), Prescription notes (textarea, different from the Prescription entity — this is a free-text field), Notes (textarea)

**Transformation before POST:** Convert empty `appointmentId` string to `null`:
```ts
await api.post('/MedicalRecord', {
  ...form,
  appointmentId: form.appointmentId || null,
});
```

---

### 4.4 Prescriptions (`src/pages/Prescriptions.tsx`)

**API endpoint:** `GET /Prescription` — paginated  
**Create:** `POST /Prescription` — `CreatePrescriptionDto`  
**Backend allowed roles:** GET: SuperAdmin, Admin, Doctor, Nurse, Pharmacist; POST: SuperAdmin, Admin, Doctor

**Enums:**
```ts
const PRESCRIPTION_STATUS_LABELS: Record<number, string> = {
  0: 'Pending', 1: 'Dispensed', 2: 'Cancelled',
};
const PRESCRIPTION_STATUS_STYLES: Record<number, string> = {
  0: 'bg-amber-100 text-amber-700 border-amber-200',
  1: 'bg-emerald-100 text-emerald-700 border-emerald-200',
  2: 'bg-rose-100 text-rose-700 border-rose-200',
};
```

**TypeScript interface:**
```ts
interface Prescription {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  medicalRecordId: string | null;
  prescriptionDate: string;
  status: number;  // PrescriptionStatus enum
  notes: string;
  dispensedDate: string | null;
  items: PrescriptionItem[];
}
interface PrescriptionItem {
  id: string;
  medicationId: string;
  medicationName: string;
  dosage: string;
  frequency: string;
  durationInDays: number;
  quantity: number;
  instructions: string;
}
```

**Table columns:** Patient, Doctor, Date, Status (badge), Item Count (items.length)  
**Search:** client-side filter by patientName, doctorName  
**Dropdowns needed:** Patients (`pageSize: 200`), Doctors (`pageSize: 200`), Medications (`pageSize: 200`)

**`emptyForm`:**
```ts
{
  patientId: '', doctorId: '', medicalRecordId: '',
  notes: '',
  items: [{ medicationId: '', dosage: '', frequency: '', durationInDays: 1, quantity: 1, instructions: '' }],
}
```

**Modal fields:**
- Patient (select, required)
- Doctor (select, required)
- Medical Record ID (optional text — convert empty to null)
- Notes (textarea, optional)
- **Dynamic rows** (use §1.5 pattern): each row has Medication (select from medications dropdown), Dosage (text), Frequency (text), Duration in Days (number), Quantity (number), Instructions (text)
- Minimum 1 row, "Add Item" button below rows

**Transformation before POST:**
```ts
await api.post('/Prescription', {
  patientId: form.patientId,
  doctorId: form.doctorId,
  medicalRecordId: form.medicalRecordId || null,
  notes: form.notes,
  items: form.items.map(item => ({
    medicationId: item.medicationId,
    dosage: item.dosage,
    frequency: item.frequency,
    durationInDays: Number(item.durationInDays),
    quantity: Number(item.quantity),
    instructions: item.instructions,
  })),
});
```

---

### 4.5 Lab Orders (`src/pages/LabOrders.tsx`)

**API endpoint:** `GET /LabOrder` — paginated  
**Create:** `POST /LabOrder` — `CreateLabOrderDto`  
**Backend allowed roles:** GET: SuperAdmin, Admin, Doctor, Nurse, LabTechnician; POST: SuperAdmin, Admin, Doctor

**Enums:**
```ts
const LAB_STATUS_LABELS: Record<string, string> = {
  'Ordered': 'Ordered',
  'SampleCollected': 'Sample Collected',
  'InProcess': 'In Process',
  'Completed': 'Completed',
  'Cancelled': 'Cancelled',
};
const LAB_STATUS_STYLES: Record<string, string> = {
  'Ordered':         'bg-blue-100 text-blue-700 border-blue-200',
  'SampleCollected': 'bg-violet-100 text-violet-700 border-violet-200',
  'InProcess':       'bg-amber-100 text-amber-700 border-amber-200',
  'Completed':       'bg-emerald-100 text-emerald-700 border-emerald-200',
  'Cancelled':       'bg-rose-100 text-rose-700 border-rose-200',
};

const PRIORITY_LABELS: Record<string, string> = {
  '0': 'Routine', '1': 'Urgent', '2': 'STAT',
};
const PRIORITY_STYLES: Record<string, string> = {
  '0': 'bg-gray-100 text-gray-700',
  '1': 'bg-orange-100 text-orange-700',
  '2': 'bg-red-100 text-red-700',
};
```

Note: `LabOrderDto.Status` and `.Priority` come back as **strings** from the backend (e.g. `"Ordered"`, `"Urgent"`), not integers. Key the lookup maps on strings.

**TypeScript interface:**
```ts
interface LabOrder {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  medicalRecordId: string | null;
  orderDate: string;
  status: string;
  priority: string;
  clinicalNotes: string;
  sampleCollectionDate: string | null;
  completedDate: string | null;
  totalAmount: number;
  items: LabOrderItem[];
}
interface LabOrderItem {
  id: string;
  labTestId: string;
  labTestName: string;
  labTestCode: string;
  price: number;
  resultValue: string | null;
  unit: string | null;
  isAbnormal: boolean;
  remarks: string | null;
}
```

**Table columns:** Patient, Doctor, Order Date, Priority (badge), Status (badge), Total Amount, Test Count  
**Search:** client-side filter by patientName, doctorName  
**Dropdowns needed:** Patients (`pageSize: 200`), Doctors (`pageSize: 200`), LabTests (`pageSize: 200`)

**`emptyForm`:**
```ts
{
  patientId: '', doctorId: '', medicalRecordId: '',
  priority: '0',   // LabOrderPriority.Routine
  clinicalNotes: '',
  items: [{ labTestId: '' }],
}
```

**Modal fields:**
- Patient (select, required)
- Doctor (select, required)
- Priority (select: Routine/Urgent/STAT)
- Clinical Notes (textarea, optional)
- **Dynamic rows** (§1.5 pattern): each row has Lab Test (select from lab tests dropdown). Minimal row — only `labTestId` is needed for creation.

**Transformation before POST:**
```ts
await api.post('/LabOrder', {
  patientId: form.patientId,
  doctorId: form.doctorId,
  medicalRecordId: form.medicalRecordId || null,
  priority: Number(form.priority),
  clinicalNotes: form.clinicalNotes,
  items: form.items.map(item => ({ labTestId: item.labTestId })),
});
```

---

### 4.6 Medications (`src/pages/Medications.tsx`)

**API endpoint:** `GET /Medication` — paginated  
**Create:** `POST /Medication` — `CreateMedicationDto`  
**Backend allowed roles:** GET: AnyStaff; POST: SuperAdmin, Admin, Pharmacist

**TypeScript interface:**
```ts
interface Medication {
  id: string;
  name: string;
  genericName: string;
  category: string;
  dosageForm: string;
  strength: string;
  price: number;
  stockQuantity: number;
  expiryDate: string | null;
  manufacturer: string;
  requiresPrescription: boolean;
}
```

**Table columns:** Name (with generic name subtitle), Category, Dosage Form, Strength, Price, Stock (with low-stock highlight if ≤ 10), Expiry Date, Requires Rx (badge)  
**Stock low-stock highlight:** if `stockQuantity <= 10` render stock cell with `text-red-600 font-semibold`  
**Search:** client-side filter by name, genericName, category (no debounce loop with backend needed — filter on loaded page)  
**No dropdowns needed** in create modal — all fields are plain inputs

**`emptyForm`:**
```ts
{
  name: '', genericName: '', category: '', dosageForm: '', strength: '',
  price: 0, stockQuantity: 0, expiryDate: '', manufacturer: '',
  requiresPrescription: true,
}
```

**Modal fields:** Name (required), Generic Name, Category, Dosage Form, Strength, Price (number), Stock Quantity (number), Expiry Date (date input, optional), Manufacturer, Requires Prescription (checkbox)

**Checkbox handling** — note `handleFieldChange` must handle checkboxes:
```ts
const handleFieldChange = (
  e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
) => {
  const target = e.target as HTMLInputElement;
  const value = target.type === 'checkbox' ? target.checked : target.value;
  setForm(prev => ({ ...prev, [target.name]: value }));
};
```

**Transformation before POST:**
```ts
await api.post('/Medication', {
  ...form,
  price: Number(form.price),
  stockQuantity: Number(form.stockQuantity),
  expiryDate: form.expiryDate || null,
});
```

---

## 5. Ordered Implementation Checklist

- [ ] 1. Add new lucide icons to `DashboardLayout.tsx` imports (`Stethoscope`, `Building2`, `ClipboardList`, `Pill`, `FlaskConical`, `Receipt`) and update `navItems` array with all 6 new entries in the position shown in §2.
      Files: `src/layouts/DashboardLayout.tsx`
      Verify: `npm run build` — no TypeScript or import errors.

- [ ] 2. Create `src/pages/Departments.tsx` using the non-paginated fetch pattern (§1.2), client-side search, and a simple two-field create modal.
      Files: `src/pages/Departments.tsx`
      Verify: `npm run build` — no type errors.

- [ ] 3. Create `src/pages/Doctors.tsx` using the paginated fetch pattern (§1.1), client-side debounced search, and a create modal that loads departments dropdown on open.
      Files: `src/pages/Doctors.tsx`
      Verify: `npm run build` — no type errors.

- [ ] 4. Create `src/pages/MedicalRecords.tsx` using the paginated fetch pattern, client-side debounced search, and a create modal with patient/doctor dropdowns and the `appointmentId` null-conversion.
      Files: `src/pages/MedicalRecords.tsx`
      Verify: `npm run build` — no type errors.

- [ ] 5. Create `src/pages/Medications.tsx` using the paginated fetch pattern, client-side search, checkbox handling in `handleFieldChange`, and a create modal with no dropdowns.
      Files: `src/pages/Medications.tsx`
      Verify: `npm run build` — no type errors.

- [ ] 6. Create `src/pages/Prescriptions.tsx` using the paginated fetch pattern, patient/doctor/medication dropdowns, and the dynamic row pattern (§1.5) for prescription items.
      Files: `src/pages/Prescriptions.tsx`
      Verify: `npm run build` — no type errors.

- [ ] 7. Create `src/pages/LabOrders.tsx` using the paginated fetch pattern, patient/doctor/labtest dropdowns, and the dynamic row pattern (§1.5) for lab order items (each row: single `labTestId` select).
      Files: `src/pages/LabOrders.tsx`
      Verify: `npm run build` — no type errors.

- [ ] 8. Register all 6 new pages in `App.tsx` — add imports and route entries exactly as specified in §3. Add them after the existing billing route, inside the `ProtectedRoute > DashboardLayout` tree.
      Files: `src/App.tsx`
      Verify: `npm run build` — full build succeeds with no errors. Then `npm run dev` and navigate to each new route to confirm it renders without runtime errors.

---

## 6. Shared CSS Class Reference

All new pages must use these exact Tailwind classes for consistency with existing pages:

| Element | Classes |
|---|---|
| Page wrapper | `p-6 max-w-7xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500` |
| Page title | `text-3xl font-bold tracking-tight text-gray-900 dark:text-white` |
| Page subtitle | `text-sm text-gray-500 dark:text-gray-400 mt-1` |
| Primary button | `inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-xl shadow-lg shadow-blue-500/30 transition-all active:scale-95` |
| Table card | `bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 shadow-sm overflow-hidden` |
| Table toolbar | `p-4 border-b border-gray-100 dark:border-gray-700 flex gap-4 bg-gray-50/50 dark:bg-gray-800/50` |
| Search input | `w-full pl-10 pr-4 py-2 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition-shadow text-gray-900 dark:text-white placeholder-gray-400` |
| Table `<thead>` | `bg-gray-50/50 dark:bg-gray-800/50 text-xs uppercase tracking-wider text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-700` |
| Table `<th>` | `px-6 py-4 font-medium` |
| Table `<tbody>` | `divide-y divide-gray-100 dark:divide-gray-700` |
| Table row | `hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors` |
| Table cell | `px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300` |
| Status badge | `inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border` |
| Loading spinner row | `<td colSpan={N} className="px-6 py-16 text-center"><Loader2 className="w-6 h-6 text-blue-500 animate-spin mx-auto" /></td>` |
| Empty state row | `px-6 py-12 text-center text-gray-500 dark:text-gray-400` with page icon + message |
| Modal overlay | `fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4` |
| Modal container | `bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto` |
| Modal header | `flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-800` |
| Modal title | `text-lg font-semibold text-gray-900 dark:text-white` |
| Modal close button | `p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors` |
| Modal body form | `px-6 py-5 space-y-5` |
| Form label | `block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1` |
| Form input/select | `w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none` |
| Error banner | `flex items-start gap-2 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 px-4 py-3 text-sm text-red-700 dark:text-red-400` |
| Cancel button | `px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors` |
| Submit button | `inline-flex items-center gap-2 px-5 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm disabled:opacity-60 disabled:cursor-not-allowed transition-colors` |

---

## 7. Summary of New Files

| File | Route | RoleGuard | API Endpoints |
|---|---|---|---|
| `src/pages/Doctors.tsx` | `/doctors` | Admin, Doctor, Nurse, Receptionist | `GET /Doctor`, `POST /Doctor`, `GET /Department` (dropdown) |
| `src/pages/Departments.tsx` | `/departments` | Admin, Doctor, Nurse, Receptionist, SuperAdmin | `GET /Department`, `POST /Department` |
| `src/pages/MedicalRecords.tsx` | `/medical-records` | Admin, Doctor, Nurse, SuperAdmin | `GET /MedicalRecord`, `POST /MedicalRecord`, `GET /Patient` + `GET /Doctor` (dropdowns) |
| `src/pages/Prescriptions.tsx` | `/prescriptions` | Admin, Doctor, Nurse, Pharmacist, SuperAdmin | `GET /Prescription`, `POST /Prescription`, `GET /Patient` + `GET /Doctor` + `GET /Medication` (dropdowns) |
| `src/pages/LabOrders.tsx` | `/lab-orders` | Admin, Doctor, Nurse, LabTechnician, SuperAdmin | `GET /LabOrder`, `POST /LabOrder`, `GET /Patient` + `GET /Doctor` + `GET /LabTest` (dropdowns) |
| `src/pages/Medications.tsx` | `/medications` | AnyStaff (all roles) | `GET /Medication`, `POST /Medication` |

Files to modify:
- `src/layouts/DashboardLayout.tsx` — add 6 nav entries
- `src/App.tsx` — add 6 imports + 6 route groups
