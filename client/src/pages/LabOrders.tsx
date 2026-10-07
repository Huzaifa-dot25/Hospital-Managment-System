import { useState, useEffect, useCallback } from 'react';
import api from '../services/api';
import {
  FlaskConical,
  Search,
  Plus,
  X,
  ChevronLeft,
  ChevronRight,
  AlertCircle,
  Loader2,
} from 'lucide-react';

// ── Types ─────────────────────────────────────────────────────────────────────

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

interface PatientOption {
  id: string;
  firstName: string;
  lastName: string;
}

interface DoctorOption {
  id: string;
  firstName: string;
  lastName: string;
  specialization?: string;
}

interface LabTestOption {
  id: string;
  name: string;
  code: string;
  price: number;
  referenceRange: string;
  unit: string;
}

interface PageMeta {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ── Enums ─────────────────────────────────────────────────────────────────────

const LAB_STATUS_LABELS: Record<string, string> = {
  Ordered: 'Ordered',
  SampleCollected: 'Sample Collected',
  InProcess: 'In Process',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};

const LAB_STATUS_STYLES: Record<string, string> = {
  Ordered:         'bg-blue-100 text-blue-700 border-blue-200 dark:bg-blue-900/20 dark:text-blue-400 dark:border-blue-500/20',
  SampleCollected: 'bg-violet-100 text-violet-700 border-violet-200 dark:bg-violet-900/20 dark:text-violet-400 dark:border-violet-500/20',
  InProcess:       'bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/20 dark:text-amber-400 dark:border-amber-500/20',
  Completed:       'bg-emerald-100 text-emerald-700 border-emerald-200 dark:bg-emerald-900/20 dark:text-emerald-400 dark:border-emerald-500/20',
  Cancelled:       'bg-rose-100 text-rose-700 border-rose-200 dark:bg-rose-900/20 dark:text-rose-400 dark:border-rose-500/20',
};

const PRIORITY_LABELS: Record<string, string> = {
  '0': 'Routine',
  '1': 'Urgent',
  '2': 'STAT',
};

const PRIORITY_STYLES: Record<string, string> = {
  '0': 'bg-gray-100 text-gray-700 border-gray-200 dark:bg-gray-700 dark:text-gray-300 dark:border-gray-600',
  '1': 'bg-orange-100 text-orange-700 border-orange-200 dark:bg-orange-900/20 dark:text-orange-400 dark:border-orange-500/20',
  '2': 'bg-red-100 text-red-700 border-red-200 dark:bg-red-900/20 dark:text-red-400 dark:border-red-500/20',
};

const PAGE_SIZE = 10;

interface LabOrderItemRow {
  labTestId: string;
}

const emptyItem = (): LabOrderItemRow => ({ labTestId: '' });

const emptyForm = () => ({
  patientId: '',
  doctorId: '',
  medicalRecordId: '',
  priority: '0',
  clinicalNotes: '',
  items: [emptyItem()],
});

// ── Component ─────────────────────────────────────────────────────────────────

const LabOrders = () => {
  const [labOrders, setLabOrders]     = useState<LabOrder[]>([]);
  const [pageMeta, setPageMeta]       = useState<PageMeta | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [searchTerm, setSearchTerm]   = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [loading, setLoading]         = useState(true);

  // Dropdown data
  const [patients, setPatients]                 = useState<PatientOption[]>([]);
  const [doctors, setDoctors]                   = useState<DoctorOption[]>([]);
  const [labTests, setLabTests]                 = useState<LabTestOption[]>([]);
  const [dropdownsLoading, setDropdownsLoading] = useState(false);

  // Modal state
  const [showModal, setShowModal]   = useState(false);
  const [form, setForm]             = useState(emptyForm);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError]   = useState<string | null>(null);

  // ── Debounce search ──────────────────────────────────────────────────────────
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchTerm);
      setCurrentPage(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // ── Fetch lab orders ─────────────────────────────────────────────────────────
  const fetchLabOrders = useCallback(async () => {
    setLoading(true);
    try {
      const params: Record<string, string | number> = {
        pageNumber: currentPage,
        pageSize: PAGE_SIZE,
      };
      if (debouncedSearch) params.search = debouncedSearch;

      const res = await api.get('/LabOrder', { params });
      const paged = res.data?.data;
      setLabOrders(paged?.items ?? []);
      setPageMeta(paged ?? null);
    } catch (err) {
      console.error('Failed to fetch lab orders:', err);
    } finally {
      setLoading(false);
    }
  }, [currentPage, debouncedSearch]);

  useEffect(() => {
    fetchLabOrders();
  }, [fetchLabOrders]);

  // ── Load dropdowns ───────────────────────────────────────────────────────────
  const loadDropdowns = async () => {
    if (patients.length > 0 && doctors.length > 0 && labTests.length > 0) return;
    setDropdownsLoading(true);
    try {
      const [patientsRes, doctorsRes, labTestsRes] = await Promise.all([
        api.get('/Patient',  { params: { pageNumber: 1, pageSize: 200 } }),
        api.get('/Doctor',   { params: { pageNumber: 1, pageSize: 200 } }),
        api.get('/LabTest',  { params: { pageNumber: 1, pageSize: 200 } }),
      ]);
      setPatients(patientsRes.data?.data?.items ?? []);
      setDoctors(doctorsRes.data?.data?.items ?? []);
      setLabTests(labTestsRes.data?.data?.items ?? []);
    } catch (err) {
      console.error('Failed to load dropdown data:', err);
    } finally {
      setDropdownsLoading(false);
    }
  };

  // ── Modal helpers ────────────────────────────────────────────────────────────
  const openModal = async () => {
    setForm(emptyForm());
    setFormError(null);
    setShowModal(true);
    await loadDropdowns();
  };

  const closeModal = () => {
    setShowModal(false);
    setFormError(null);
  };

  const handleFieldChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  // ── Dynamic row handlers ─────────────────────────────────────────────────────
  const addRow = () =>
    setForm((prev) => ({ ...prev, items: [...prev.items, emptyItem()] }));

  const removeRow = (idx: number) =>
    setForm((prev) => ({ ...prev, items: prev.items.filter((_, i) => i !== idx) }));

  const handleItemChange = (
    idx: number,
    e: React.ChangeEvent<HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setForm((prev) => {
      const items = [...prev.items];
      items[idx] = { ...items[idx], [name]: value };
      return { ...prev, items };
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!form.patientId || !form.doctorId) {
      setFormError('Please select both a patient and a doctor.');
      return;
    }
    if (form.items.some((item) => !item.labTestId)) {
      setFormError('Please select a lab test for each row.');
      return;
    }

    setSubmitting(true);
    try {
      await api.post('/LabOrder', {
        patientId: form.patientId,
        doctorId: form.doctorId,
        medicalRecordId: form.medicalRecordId || null,
        priority: Number(form.priority),
        clinicalNotes: form.clinicalNotes,
        items: form.items.map((item) => ({ labTestId: item.labTestId })),
      });
      closeModal();
      setCurrentPage(1);
      fetchLabOrders();
    } catch (err: any) {
      const msg =
        err.response?.data?.message ||
        err.response?.data?.title ||
        'Failed to create lab order. Please check your input.';
      setFormError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  // ── Render ───────────────────────────────────────────────────────────────────
  return (
    <div className="p-6 max-w-7xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">

      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-gray-900 dark:text-white">
            Lab Orders
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            {pageMeta
              ? `${pageMeta.totalCount} order${pageMeta.totalCount !== 1 ? 's' : ''}`
              : 'Loading…'}
          </p>
        </div>
        <button
          onClick={openModal}
          className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-xl shadow-lg shadow-blue-500/30 transition-all active:scale-95"
        >
          <Plus className="w-4 h-4" />
          New Lab Order
        </button>
      </div>

      {/* Table card */}
      <div className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 shadow-sm overflow-hidden">

        {/* Toolbar */}
        <div className="p-4 border-b border-gray-100 dark:border-gray-700 flex gap-4 bg-gray-50/50 dark:bg-gray-800/50">
          <div className="relative max-w-md w-full">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input
              type="text"
              placeholder="Search by patient or doctor…"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition-shadow text-gray-900 dark:text-white placeholder-gray-400"
            />
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-gray-50/50 dark:bg-gray-800/50 text-xs uppercase tracking-wider text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-700">
                <th className="px-6 py-4 font-medium">Patient</th>
                <th className="px-6 py-4 font-medium">Doctor</th>
                <th className="px-6 py-4 font-medium">Order Date</th>
                <th className="px-6 py-4 font-medium">Priority</th>
                <th className="px-6 py-4 font-medium">Status</th>
                <th className="px-6 py-4 font-medium">Total Amount</th>
                <th className="px-6 py-4 font-medium">Tests</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
              {loading ? (
                <tr>
                  <td colSpan={7} className="px-6 py-16 text-center">
                    <Loader2 className="w-6 h-6 text-blue-500 animate-spin mx-auto" />
                  </td>
                </tr>
              ) : labOrders.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    <div className="flex flex-col items-center gap-3">
                      <FlaskConical className="w-8 h-8 text-gray-300 dark:text-gray-600" />
                      <p>No lab orders found{debouncedSearch ? ' matching your search' : ''}.</p>
                    </div>
                  </td>
                </tr>
              ) : (
                labOrders.map((lo, idx) => (
                  <tr
                    key={lo.id}
                    className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                    style={{ animationDelay: `${idx * 40}ms` }}
                  >
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-white">
                      {lo.patientName}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                      {lo.doctorName}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                      {new Date(lo.orderDate).toLocaleDateString(undefined, {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric',
                      })}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span
                        className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${
                          PRIORITY_STYLES[String(lo.priority)] ?? PRIORITY_STYLES['0']
                        }`}
                      >
                        {PRIORITY_LABELS[String(lo.priority)] ?? lo.priority}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span
                        className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${
                          LAB_STATUS_STYLES[lo.status] ?? 'bg-gray-100 text-gray-700 border-gray-200'
                        }`}
                      >
                        {LAB_STATUS_LABELS[lo.status] ?? lo.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                      ${lo.totalAmount.toFixed(2)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                      {lo.items?.length ?? 0} test{(lo.items?.length ?? 0) !== 1 ? 's' : ''}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {pageMeta && pageMeta.totalPages > 1 && (
          <div className="px-6 py-4 border-t border-gray-100 dark:border-gray-700 flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 bg-gray-50/50 dark:bg-gray-800/50">
            <span>
              Page {pageMeta.pageNumber} of {pageMeta.totalPages} &nbsp;·&nbsp; {pageMeta.totalCount} total
            </span>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setCurrentPage((p) => p - 1)}
                disabled={!pageMeta.hasPreviousPage}
                className="p-1.5 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                aria-label="Previous page"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button
                onClick={() => setCurrentPage((p) => p + 1)}
                disabled={!pageMeta.hasNextPage}
                className="p-1.5 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                aria-label="Next page"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* ── New Lab Order Modal ────────────────────────────────────────────────── */}
      {showModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="lab-modal-title"
        >
          <div className="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">

            {/* Modal header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-800">
              <h2 id="lab-modal-title" className="text-lg font-semibold text-gray-900 dark:text-white">
                New Lab Order
              </h2>
              <button
                onClick={closeModal}
                className="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
                aria-label="Close modal"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Modal body */}
            <form onSubmit={handleSubmit} className="px-6 py-5 space-y-5">
              {formError && (
                <div className="flex items-start gap-2 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 px-4 py-3 text-sm text-red-700 dark:text-red-400">
                  <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" />
                  <span>{formError}</span>
                </div>
              )}

              {dropdownsLoading ? (
                <div className="flex items-center justify-center py-8">
                  <Loader2 className="w-6 h-6 text-blue-500 animate-spin" />
                  <span className="ml-2 text-sm text-gray-500">Loading data…</span>
                </div>
              ) : (
                <>
                  {/* Patient / Doctor */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                        Patient <span className="text-red-500">*</span>
                      </label>
                      <select
                        name="patientId"
                        value={form.patientId}
                        onChange={handleFieldChange}
                        required
                        className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                      >
                        <option value="">— Select patient —</option>
                        {patients.map((p) => (
                          <option key={p.id} value={p.id}>
                            {p.firstName} {p.lastName}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                        Doctor <span className="text-red-500">*</span>
                      </label>
                      <select
                        name="doctorId"
                        value={form.doctorId}
                        onChange={handleFieldChange}
                        required
                        className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                      >
                        <option value="">— Select doctor —</option>
                        {doctors.map((d) => (
                          <option key={d.id} value={d.id}>
                            Dr. {d.firstName} {d.lastName}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  {/* Priority */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Priority <span className="text-red-500">*</span>
                    </label>
                    <select
                      name="priority"
                      value={form.priority}
                      onChange={handleFieldChange}
                      className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    >
                      <option value="0">Routine</option>
                      <option value="1">Urgent</option>
                      <option value="2">STAT</option>
                    </select>
                  </div>

                  {/* Clinical Notes */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Clinical Notes
                    </label>
                    <textarea
                      name="clinicalNotes"
                      value={form.clinicalNotes}
                      onChange={handleFieldChange}
                      rows={2}
                      className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none resize-none"
                      placeholder="Clinical notes for the lab…"
                    />
                  </div>

                  {/* Lab Test rows */}
                  <div>
                    <p className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Lab Tests <span className="text-red-500">*</span>
                    </p>
                    <div className="space-y-2">
                      {form.items.map((item, idx) => (
                        <div
                          key={idx}
                          className="grid grid-cols-[1fr_auto] gap-2 items-start border border-gray-200 dark:border-gray-700 rounded-lg p-3"
                        >
                          <div>
                            <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                              Lab Test <span className="text-red-500">*</span>
                            </label>
                            <select
                              name="labTestId"
                              value={item.labTestId}
                              onChange={(e) => handleItemChange(idx, e)}
                              required
                              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                            >
                              <option value="">— Select test —</option>
                              {labTests.map((t) => (
                                <option key={t.id} value={t.id}>
                                  {t.name} ({t.code}) — ${t.price.toFixed(2)}
                                </option>
                              ))}
                            </select>
                          </div>
                          {form.items.length > 1 && (
                            <button
                              type="button"
                              onClick={() => removeRow(idx)}
                              className="p-1.5 text-gray-400 hover:text-red-500 rounded transition-colors mt-5"
                              aria-label="Remove test"
                            >
                              <X className="w-4 h-4" />
                            </button>
                          )}
                        </div>
                      ))}
                    </div>
                    <button
                      type="button"
                      onClick={addRow}
                      className="mt-2 inline-flex items-center gap-1.5 text-sm text-blue-600 hover:text-blue-700 font-medium"
                    >
                      <Plus className="w-4 h-4" /> Add Test
                    </button>
                  </div>
                </>
              )}

              {/* Footer */}
              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={closeModal}
                  className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting || dropdownsLoading}
                  className="inline-flex items-center gap-2 px-5 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm disabled:opacity-60 disabled:cursor-not-allowed transition-colors"
                >
                  {submitting && <Loader2 className="w-4 h-4 animate-spin" />}
                  {submitting ? 'Saving…' : 'Create Lab Order'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default LabOrders;
