import { useState, useEffect, useCallback } from 'react';
import api from '../services/api';
import {
  Activity,
  Search,
  Plus,
  X,
  ChevronLeft,
  ChevronRight,
  CalendarDays,
  User,
  Stethoscope,
  AlertCircle,
  Loader2,
} from 'lucide-react';

// ── Types ─────────────────────────────────────────────────────────────────────

interface Appointment {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  appointmentDate: string;
  reason: string;
  status: number; // AppointmentStatus enum
  notes: string;
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

interface PageMeta {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Matches Hospital.Domain.Enums.AppointmentStatus
const STATUS_LABELS: Record<number, string> = {
  0: 'Scheduled',
  1: 'Completed',
  2: 'Cancelled',
  3: 'NoShow',
};

const STATUS_STYLES: Record<number, string> = {
  0: 'bg-blue-100 text-blue-700 dark:bg-blue-500/10 dark:text-blue-400 border-blue-200 dark:border-blue-500/20',
  1: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400 border-emerald-200 dark:border-emerald-500/20',
  2: 'bg-rose-100 text-rose-700 dark:bg-rose-500/10 dark:text-rose-400 border-rose-200 dark:border-rose-500/20',
  3: 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400 border-amber-200 dark:border-amber-500/20',
};

const PAGE_SIZE = 10;

const emptyForm = () => ({
  patientId: '',
  doctorId: '',
  appointmentDate: '',
  appointmentTime: '',
  reason: '',
});

// ── Component ─────────────────────────────────────────────────────────────────

const Appointments = () => {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [pageMeta, setPageMeta]         = useState<PageMeta | null>(null);
  const [currentPage, setCurrentPage]   = useState(1);
  const [searchTerm, setSearchTerm]     = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [loading, setLoading]           = useState(true);

  // Dropdown data for the modal
  const [patients, setPatients]   = useState<PatientOption[]>([]);
  const [doctors, setDoctors]     = useState<DoctorOption[]>([]);
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

  // ── Fetch appointments ───────────────────────────────────────────────────────
  const fetchAppointments = useCallback(async () => {
    setLoading(true);
    try {
      const params: Record<string, string | number> = {
        pageNumber: currentPage,
        pageSize: PAGE_SIZE,
      };

      const res = await api.get('/Appointment', { params });
      const paged = res.data?.data;
      setAppointments(paged?.items ?? []);
      setPageMeta(paged ?? null);
    } catch (err) {
      console.error('Failed to fetch appointments:', err);
    } finally {
      setLoading(false);
    }
  }, [currentPage]);

  useEffect(() => {
    fetchAppointments();
  }, [fetchAppointments]);

  // ── Fetch dropdown data when modal opens ─────────────────────────────────────
  const loadDropdowns = async () => {
    if (patients.length > 0 && doctors.length > 0) return; // already loaded
    setDropdownsLoading(true);
    try {
      const [patientsRes, doctorsRes] = await Promise.all([
        api.get('/Patient', { params: { pageNumber: 1, pageSize: 200 } }),
        api.get('/Doctor',  { params: { pageNumber: 1, pageSize: 200 } }),
      ]);
      setPatients(patientsRes.data?.data?.items ?? []);
      setDoctors(doctorsRes.data?.data?.items   ?? []);
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!form.patientId || !form.doctorId) {
      setFormError('Please select both a patient and a doctor.');
      return;
    }

    // Combine date + time into a UTC ISO string the backend expects
    const combinedDateTime = form.appointmentTime
      ? `${form.appointmentDate}T${form.appointmentTime}:00`
      : `${form.appointmentDate}T00:00:00`;

    setSubmitting(true);
    try {
      await api.post('/Appointment', {
        patientId: form.patientId,
        doctorId: form.doctorId,
        appointmentDate: combinedDateTime,
        reason: form.reason,
      });
      closeModal();
      setCurrentPage(1);
      fetchAppointments();
    } catch (err: any) {
      const msg =
        err.response?.data?.message ||
        err.response?.data?.title ||
        'Failed to book appointment. Please check your input.';
      setFormError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  // ── Client-side filter on loaded page ────────────────────────────────────────
  const filtered = debouncedSearch
    ? appointments.filter(
        (a) =>
          a.patientName?.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
          a.doctorName?.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
          a.reason?.toLowerCase().includes(debouncedSearch.toLowerCase()),
      )
    : appointments;

  // ── Render ───────────────────────────────────────────────────────────────────
  return (
    <div className="p-6 max-w-7xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">

      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-gray-900 dark:text-white">
            Appointments
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            {pageMeta
              ? `${pageMeta.totalCount} appointment${pageMeta.totalCount !== 1 ? 's' : ''} total`
              : 'Loading…'}
          </p>
        </div>
        <button
          onClick={openModal}
          className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-xl shadow-lg shadow-blue-500/30 transition-all active:scale-95"
        >
          <Plus className="w-4 h-4" />
          Book Appointment
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
              placeholder="Search by patient, doctor or reason…"
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
                <th className="px-6 py-4 font-medium">Date & Time</th>
                <th className="px-6 py-4 font-medium">Reason</th>
                <th className="px-6 py-4 font-medium">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
              {loading ? (
                <tr>
                  <td colSpan={5} className="px-6 py-16 text-center">
                    <Loader2 className="w-6 h-6 text-blue-500 animate-spin mx-auto" />
                  </td>
                </tr>
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    <div className="flex flex-col items-center gap-3">
                      <Activity className="w-8 h-8 text-gray-300 dark:text-gray-600" />
                      <p>No appointments found{debouncedSearch ? ' matching your search' : ''}.</p>
                    </div>
                  </td>
                </tr>
              ) : (
                filtered.map((appt, idx) => {
                  const dt = new Date(appt.appointmentDate);
                  return (
                    <tr
                      key={appt.id}
                      className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                      style={{ animationDelay: `${idx * 40}ms` }}
                    >
                      {/* Patient */}
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-2.5">
                          <div className="w-8 h-8 rounded-full bg-violet-100 dark:bg-violet-900/30 flex items-center justify-center">
                            <User className="w-4 h-4 text-violet-600 dark:text-violet-400" />
                          </div>
                          <span className="text-sm font-medium text-gray-900 dark:text-white">
                            {appt.patientName}
                          </span>
                        </div>
                      </td>

                      {/* Doctor */}
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-2.5">
                          <div className="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center">
                            <Stethoscope className="w-4 h-4 text-blue-600 dark:text-blue-400" />
                          </div>
                          <span className="text-sm text-gray-700 dark:text-gray-300">
                            {appt.doctorName}
                          </span>
                        </div>
                      </td>

                      {/* Date & Time */}
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-1.5 text-sm text-gray-700 dark:text-gray-300">
                          <CalendarDays className="w-4 h-4 text-gray-400 shrink-0" />
                          <div>
                            <p className="font-medium">
                              {dt.toLocaleDateString(undefined, {
                                year: 'numeric',
                                month: 'short',
                                day: 'numeric',
                              })}
                            </p>
                            <p className="text-xs text-gray-400">
                              {dt.toLocaleTimeString(undefined, {
                                hour: '2-digit',
                                minute: '2-digit',
                              })}
                            </p>
                          </div>
                        </div>
                      </td>

                      {/* Reason */}
                      <td className="px-6 py-4 text-sm text-gray-500 dark:text-gray-400 max-w-[200px]">
                        <p className="truncate">{appt.reason || '—'}</p>
                      </td>

                      {/* Status */}
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span
                          className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${
                            STATUS_STYLES[appt.status] ?? STATUS_STYLES[0]
                          }`}
                        >
                          {STATUS_LABELS[appt.status] ?? 'Unknown'}
                        </span>
                      </td>
                    </tr>
                  );
                })
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

      {/* ── Book Appointment Modal ─────────────────────────────────────────────── */}
      {showModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="appt-modal-title"
        >
          <div className="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">

            {/* Modal header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-800">
              <h2 id="appt-modal-title" className="text-lg font-semibold text-gray-900 dark:text-white">
                Book New Appointment
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
                  <span className="ml-2 text-sm text-gray-500">Loading patients and doctors…</span>
                </div>
              ) : (
                <>
                  {/* Patient select */}
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

                  {/* Doctor select */}
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
                          {d.specialization ? ` — ${d.specialization}` : ''}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Date & Time */}
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                        Date <span className="text-red-500">*</span>
                      </label>
                      <input
                        type="date"
                        name="appointmentDate"
                        value={form.appointmentDate}
                        onChange={handleFieldChange}
                        required
                        min={new Date().toISOString().split('T')[0]}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                        Time
                      </label>
                      <input
                        type="time"
                        name="appointmentTime"
                        value={form.appointmentTime}
                        onChange={handleFieldChange}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                      />
                    </div>
                  </div>

                  {/* Reason */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Reason for Visit
                    </label>
                    <textarea
                      name="reason"
                      value={form.reason}
                      onChange={handleFieldChange}
                      rows={3}
                      className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none resize-none"
                      placeholder="Brief description of the visit reason…"
                    />
                  </div>
                </>
              )}

              {/* Footer actions */}
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
                  {submitting ? 'Booking…' : 'Book Appointment'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Appointments;
