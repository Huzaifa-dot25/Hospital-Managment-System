import { useState, useEffect, useCallback } from 'react';
import api from '../services/api';
import {
  Pill,
  Search,
  Plus,
  X,
  ChevronLeft,
  ChevronRight,
  AlertCircle,
  Loader2,
} from 'lucide-react';

// ── Types ─────────────────────────────────────────────────────────────────────

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

interface PageMeta {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

const PAGE_SIZE = 10;

const emptyForm = () => ({
  name: '',
  genericName: '',
  category: '',
  dosageForm: '',
  strength: '',
  price: 0,
  stockQuantity: 0,
  expiryDate: '',
  manufacturer: '',
  requiresPrescription: true,
});

// ── Component ─────────────────────────────────────────────────────────────────

const Medications = () => {
  const [medications, setMedications] = useState<Medication[]>([]);
  const [pageMeta, setPageMeta]       = useState<PageMeta | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [searchTerm, setSearchTerm]   = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [loading, setLoading]         = useState(true);

  // Modal state
  const [showModal, setShowModal]   = useState(false);
  const [form, setForm]             = useState(emptyForm());
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

  // ── Fetch medications ────────────────────────────────────────────────────────
  const fetchMedications = useCallback(async () => {
    setLoading(true);
    try {
      const params: Record<string, string | number> = {
        pageNumber: currentPage,
        pageSize: PAGE_SIZE,
      };

      const res = await api.get('/Medication', { params });
      const paged = res.data?.data;
      setMedications(paged?.items ?? []);
      setPageMeta(paged ?? null);
    } catch (err) {
      console.error('Failed to fetch medications:', err);
    } finally {
      setLoading(false);
    }
  }, [currentPage]);

  useEffect(() => {
    fetchMedications();
  }, [fetchMedications]);

  // ── Modal helpers ────────────────────────────────────────────────────────────
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
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const target = e.target as HTMLInputElement;
    const value = target.type === 'checkbox' ? target.checked : target.value;
    setForm((prev) => ({ ...prev, [target.name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setSubmitting(true);
    try {
      await api.post('/Medication', {
        ...form,
        price: Number(form.price),
        stockQuantity: Number(form.stockQuantity),
        expiryDate: form.expiryDate || null,
      });
      closeModal();
      setCurrentPage(1);
      fetchMedications();
    } catch (err: any) {
      const msg =
        err.response?.data?.message ||
        err.response?.data?.title ||
        'Failed to save medication.';
      setFormError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  // ── Client-side search filter (applied to the loaded page) ──────────────────
  const filteredMedications = debouncedSearch
    ? medications.filter(
        (m) =>
          m.name.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
          m.genericName.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
          m.category.toLowerCase().includes(debouncedSearch.toLowerCase()),
      )
    : medications;

  // ── Render ───────────────────────────────────────────────────────────────────
  return (
    <div className="p-6 max-w-7xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">

      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-gray-900 dark:text-white">
            Medications
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            {pageMeta
              ? `${pageMeta.totalCount} medication${pageMeta.totalCount !== 1 ? 's' : ''}`
              : 'Loading…'}
          </p>
        </div>
        <button
          onClick={openModal}
          className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-xl shadow-lg shadow-blue-500/30 transition-all active:scale-95"
        >
          <Plus className="w-4 h-4" />
          Add Medication
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
              placeholder="Search by name, generic name, or category…"
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
                <th className="px-6 py-4 font-medium">Name</th>
                <th className="px-6 py-4 font-medium">Category</th>
                <th className="px-6 py-4 font-medium">Dosage Form</th>
                <th className="px-6 py-4 font-medium">Strength</th>
                <th className="px-6 py-4 font-medium">Price</th>
                <th className="px-6 py-4 font-medium">Stock</th>
                <th className="px-6 py-4 font-medium">Expiry Date</th>
                <th className="px-6 py-4 font-medium">Requires Rx</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
              {loading ? (
                <tr>
                  <td colSpan={8} className="px-6 py-16 text-center">
                    <Loader2 className="w-6 h-6 text-blue-500 animate-spin mx-auto" />
                  </td>
                </tr>
              ) : filteredMedications.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    <div className="flex flex-col items-center gap-3">
                      <Pill className="w-8 h-8 text-gray-300 dark:text-gray-600" />
                      <p>No medications found{debouncedSearch ? ' matching your search' : ''}.</p>
                    </div>
                  </td>
                </tr>
              ) : (
                filteredMedications.map((med, idx) => (
                  <tr
                    key={med.id}
                    className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                    style={{ animationDelay: `${idx * 40}ms` }}
                  >
                    {/* Name + generic name subtitle */}
                    <td className="px-6 py-4 text-sm">
                      <p className="font-medium text-gray-900 dark:text-white">{med.name}</p>
                      {med.genericName && (
                        <p className="text-gray-400 dark:text-gray-500 text-xs mt-0.5">{med.genericName}</p>
                      )}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                      {med.category || '—'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                      {med.dosageForm || '—'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                      {med.strength || '—'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                      ${med.price.toFixed(2)}
                    </td>
                    {/* Stock with low-stock highlight */}
                    <td
                      className={`px-6 py-4 whitespace-nowrap text-sm ${
                        med.stockQuantity <= 10
                          ? 'text-red-600 font-semibold'
                          : 'text-gray-600 dark:text-gray-300'
                      }`}
                    >
                      {med.stockQuantity}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                      {med.expiryDate
                        ? new Date(med.expiryDate).toLocaleDateString(undefined, {
                            year: 'numeric',
                            month: 'short',
                            day: 'numeric',
                          })
                        : '—'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span
                        className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${
                          med.requiresPrescription
                            ? 'bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/20 dark:text-amber-400 dark:border-amber-500/20'
                            : 'bg-gray-100 text-gray-600 border-gray-200 dark:bg-gray-700 dark:text-gray-300 dark:border-gray-600'
                        }`}
                      >
                        {med.requiresPrescription ? 'Required' : 'OTC'}
                      </span>
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

      {/* ── Add Medication Modal ───────────────────────────────────────────────── */}
      {showModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="med-modal-title"
        >
          <div className="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">

            {/* Modal header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-800">
              <h2 id="med-modal-title" className="text-lg font-semibold text-gray-900 dark:text-white">
                Add Medication
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

              {/* Name / Generic Name */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Name <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    name="name"
                    value={form.name}
                    onChange={handleFieldChange}
                    required
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    placeholder="e.g. Amoxicillin"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Generic Name
                  </label>
                  <input
                    type="text"
                    name="genericName"
                    value={form.genericName}
                    onChange={handleFieldChange}
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    placeholder="e.g. Amoxicillin trihydrate"
                  />
                </div>
              </div>

              {/* Category / Dosage Form */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Category
                  </label>
                  <input
                    type="text"
                    name="category"
                    value={form.category}
                    onChange={handleFieldChange}
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    placeholder="e.g. Antibiotic"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Dosage Form
                  </label>
                  <input
                    type="text"
                    name="dosageForm"
                    value={form.dosageForm}
                    onChange={handleFieldChange}
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    placeholder="e.g. Capsule"
                  />
                </div>
              </div>

              {/* Strength / Manufacturer */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Strength
                  </label>
                  <input
                    type="text"
                    name="strength"
                    value={form.strength}
                    onChange={handleFieldChange}
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    placeholder="e.g. 500mg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Manufacturer
                  </label>
                  <input
                    type="text"
                    name="manufacturer"
                    value={form.manufacturer}
                    onChange={handleFieldChange}
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    placeholder="e.g. Pfizer"
                  />
                </div>
              </div>

              {/* Price / Stock Quantity */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Price ($) <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="number"
                    name="price"
                    value={form.price}
                    onChange={handleFieldChange}
                    min={0}
                    step={0.01}
                    required
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Stock Quantity <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="number"
                    name="stockQuantity"
                    value={form.stockQuantity}
                    onChange={handleFieldChange}
                    min={0}
                    required
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  />
                </div>
              </div>

              {/* Expiry Date */}
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Expiry Date
                </label>
                <input
                  type="date"
                  name="expiryDate"
                  value={form.expiryDate}
                  onChange={handleFieldChange}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
              </div>

              {/* Requires Prescription */}
              <div className="flex items-center gap-3">
                <input
                  type="checkbox"
                  id="requiresPrescription"
                  name="requiresPrescription"
                  checked={form.requiresPrescription}
                  onChange={handleFieldChange}
                  className="w-4 h-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <label
                  htmlFor="requiresPrescription"
                  className="text-sm font-medium text-gray-700 dark:text-gray-300 cursor-pointer"
                >
                  Requires Prescription
                </label>
              </div>

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
                  disabled={submitting}
                  className="inline-flex items-center gap-2 px-5 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm disabled:opacity-60 disabled:cursor-not-allowed transition-colors"
                >
                  {submitting && <Loader2 className="w-4 h-4 animate-spin" />}
                  {submitting ? 'Saving…' : 'Add Medication'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Medications;
