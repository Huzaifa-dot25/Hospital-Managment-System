import { useState, useEffect, useCallback } from 'react';
import api from '../services/api';
import {
  FileText,
  DollarSign,
  Search,
  MoreVertical,
  CheckCircle,
  Clock,
  XCircle,
  Plus,
  X,
  ChevronLeft,
  ChevronRight,
  AlertCircle,
  Loader2,
  CreditCard,
  Banknote,
  Eye,
} from 'lucide-react';

// ── Types ─────────────────────────────────────────────────────────────────────

interface InvoiceItem {
  id: string;
  itemType: string;
  description: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

interface Payment {
  id: string;
  paymentDate: string;
  amount: number;
  method: string;
  status: string;
  transactionReference: string;
  notes: string;
}

interface InvoiceDetail {
  id: string;
  invoiceNumber: string;
  patientId: string;
  patientName: string;
  issueDate: string;
  dueDate: string;
  subTotal: number;
  taxPercentage: number;
  taxAmount: number;
  discountAmount: number;
  totalAmount: number;
  paidAmount: number;
  balanceDue: number;
  status: string;
  notes: string;
  items: InvoiceItem[];
  payments: Payment[];
}

interface Invoice {
  id: string;
  invoiceNumber: string;
  patientName: string;
  amount: number;
  date: string;
  status: string;
}

interface PatientOption {
  id: string;
  firstName: string;
  lastName: string;
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

const BILLING_ITEM_TYPES = [
  { value: 0, label: 'Consultation' },
  { value: 1, label: 'Medication' },
  { value: 2, label: 'Lab Test' },
  { value: 3, label: 'Procedure' },
  { value: 4, label: 'Room Charge' },
  { value: 5, label: 'Other' },
];

const PAYMENT_METHODS = [
  { value: 0, label: 'Cash' },
  { value: 1, label: 'Credit Card' },
  { value: 2, label: 'Debit Card' },
  { value: 3, label: 'Insurance' },
  { value: 4, label: 'Bank Transfer' },
];

const STATUS_FILTERS = ['All', 'Pending', 'Paid', 'Cancelled'];
const PAGE_SIZE = 10;

// ── Empty form helpers ─────────────────────────────────────────────────────────

const emptyLineItem = () => ({
  itemType: 0,
  description: '',
  unitPrice: 0,
  quantity: 1,
});

const emptyInvoiceForm = () => ({
  patientId: '',
  dueDate: '',
  taxPercentage: 0,
  discountAmount: 0,
  notes: '',
  items: [emptyLineItem()],
});

const emptyPaymentForm = () => ({
  amount: 0,
  method: 0,
  transactionReference: '',
  notes: '',
});

// ── Component ─────────────────────────────────────────────────────────────────

const Billing = () => {
  const [invoices, setInvoices]         = useState<Invoice[]>([]);
  const [pageMeta, setPageMeta]         = useState<PageMeta | null>(null);
  const [currentPage, setCurrentPage]   = useState(1);
  const [searchTerm, setSearchTerm]     = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [loading, setLoading]           = useState(true);

  // Create Invoice modal
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm]           = useState(emptyInvoiceForm());
  const [creating, setCreating]               = useState(false);
  const [createError, setCreateError]         = useState<string | null>(null);
  const [patients, setPatients]               = useState<PatientOption[]>([]);
  const [patientsLoading, setPatientsLoading] = useState(false);

  // Invoice detail slide-over
  const [selectedInvoice, setSelectedInvoice]       = useState<InvoiceDetail | null>(null);
  const [detailLoading, setDetailLoading]             = useState(false);
  const [paymentForm, setPaymentForm]                 = useState(emptyPaymentForm());
  const [paying, setPaying]                           = useState(false);
  const [paymentError, setPaymentError]               = useState<string | null>(null);
  const [cancelling, setCancelling]                   = useState(false);

  // ── Debounce search ──────────────────────────────────────────────────────────
  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(searchTerm);
      setCurrentPage(1);
    }, 400);
    return () => clearTimeout(t);
  }, [searchTerm]);

  // ── Fetch invoices ───────────────────────────────────────────────────────────
  const fetchInvoices = useCallback(async () => {
    setLoading(true);
    try {
      const params: Record<string, string | number> = {
        pageNumber: currentPage,
        pageSize: PAGE_SIZE,
      };
      if (debouncedSearch) params.search = debouncedSearch;
      if (statusFilter !== 'All') params.status = statusFilter;

      const res = await api.get('/Invoice', { params });
      const paged = res.data?.data;
      const items: any[] = paged?.items ?? [];

      setInvoices(
        items.map((inv: any) => ({
          id: inv.id,
          invoiceNumber: inv.invoiceNumber,
          patientName: inv.patientName,
          amount: inv.totalAmount ?? 0,
          date: inv.issueDate ? new Date(inv.issueDate).toISOString().split('T')[0] : '',
          status: inv.status ?? 'Pending',
        }))
      );
      setPageMeta(paged ?? null);
    } catch (err) {
      console.error('Failed to fetch invoices:', err);
    } finally {
      setLoading(false);
    }
  }, [currentPage, debouncedSearch, statusFilter]);

  useEffect(() => {
    fetchInvoices();
  }, [fetchInvoices]);

  // ── Derived stats (from all pages — re-fetch without paging for stats) ───────
  const totalRevenue = invoices.filter(i => i.status === 'Paid').reduce((s, i) => s + i.amount, 0);
  const pendingAmount = invoices.filter(i => i.status === 'Pending').reduce((s, i) => s + i.amount, 0);

  // ── Load patients for Create Invoice modal ───────────────────────────────────
  const loadPatients = async () => {
    if (patients.length > 0) return;
    setPatientsLoading(true);
    try {
      const res = await api.get('/Patient', { params: { pageNumber: 1, pageSize: 200 } });
      setPatients(res.data?.data?.items ?? []);
    } catch (e) {
      console.error('Failed to load patients:', e);
    } finally {
      setPatientsLoading(false);
    }
  };

  // ── Create Invoice ───────────────────────────────────────────────────────────
  const openCreateModal = async () => {
    setCreateForm(emptyInvoiceForm());
    setCreateError(null);
    setShowCreateModal(true);
    await loadPatients();
  };

  const closeCreateModal = () => {
    setShowCreateModal(false);
    setCreateError(null);
  };

  const handleCreateField = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setCreateForm(prev => ({ ...prev, [name]: value }));
  };

  const addLineItem = () =>
    setCreateForm(prev => ({ ...prev, items: [...prev.items, emptyLineItem()] }));

  const removeLineItem = (idx: number) =>
    setCreateForm(prev => ({ ...prev, items: prev.items.filter((_, i) => i !== idx) }));

  const handleLineItemChange = (idx: number, e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setCreateForm(prev => {
      const items = [...prev.items];
      items[idx] = { ...items[idx], [name]: value };
      return { ...prev, items };
    });
  };

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreateError(null);
    if (!createForm.patientId) { setCreateError('Please select a patient.'); return; }
    if (createForm.items.length === 0) { setCreateError('Add at least one line item.'); return; }
    if (createForm.items.some(it => !it.description)) { setCreateError('Each line item needs a description.'); return; }

    setCreating(true);
    try {
      await api.post('/Invoice', {
        patientId: createForm.patientId,
        dueDate: createForm.dueDate || null,
        taxPercentage: Number(createForm.taxPercentage),
        discountAmount: Number(createForm.discountAmount),
        notes: createForm.notes,
        items: createForm.items.map(it => ({
          itemType: Number(it.itemType),
          description: it.description,
          unitPrice: Number(it.unitPrice),
          quantity: Number(it.quantity),
        })),
      });
      closeCreateModal();
      setCurrentPage(1);
      fetchInvoices();
    } catch (err: any) {
      setCreateError(err.response?.data?.message || err.response?.data?.title || 'Failed to create invoice.');
    } finally {
      setCreating(false);
    }
  };

  // ── Invoice detail ───────────────────────────────────────────────────────────
  const openDetail = async (id: string) => {
    setSelectedInvoice(null);
    setDetailLoading(true);
    setPaymentForm(emptyPaymentForm());
    setPaymentError(null);
    try {
      const res = await api.get(`/Invoice/${id}`);
      setSelectedInvoice(res.data?.data ?? null);
    } catch (err) {
      console.error('Failed to load invoice detail:', err);
    } finally {
      setDetailLoading(false);
    }
  };

  const closeDetail = () => setSelectedInvoice(null);

  // ── Record Payment ───────────────────────────────────────────────────────────
  const handlePaymentField = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setPaymentForm(prev => ({ ...prev, [name]: value }));
  };

  const handleRecordPayment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedInvoice) return;
    setPaymentError(null);
    const amount = Number(paymentForm.amount);
    if (!amount || amount <= 0) { setPaymentError('Enter a valid payment amount.'); return; }

    setPaying(true);
    try {
      await api.post('/Payment', {
        invoiceId: selectedInvoice.id,
        amount,
        method: Number(paymentForm.method),
        transactionReference: paymentForm.transactionReference,
        notes: paymentForm.notes,
      });
      // Refresh detail panel
      await openDetail(selectedInvoice.id);
      fetchInvoices();
      setPaymentForm(emptyPaymentForm());
    } catch (err: any) {
      setPaymentError(err.response?.data?.message || err.response?.data?.title || 'Payment failed.');
    } finally {
      setPaying(false);
    }
  };

  // ── Cancel Invoice ───────────────────────────────────────────────────────────
  const handleCancelInvoice = async (id: string) => {
    if (!window.confirm('Cancel this invoice? This cannot be undone.')) return;
    setCancelling(true);
    try {
      await api.post(`/Invoice/${id}/cancel`);
      if (selectedInvoice?.id === id) await openDetail(id);
      fetchInvoices();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to cancel invoice.');
    } finally {
      setCancelling(false);
    }
  };

  // ── Status badge ─────────────────────────────────────────────────────────────
  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Paid':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-500/20">
            <CheckCircle className="w-3.5 h-3.5" /> Paid
          </span>
        );
      case 'Pending':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400 border border-amber-200 dark:border-amber-500/20">
            <Clock className="w-3.5 h-3.5" /> Pending
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-rose-100 text-rose-700 dark:bg-rose-500/10 dark:text-rose-400 border border-rose-200 dark:border-rose-500/20">
            <XCircle className="w-3.5 h-3.5" /> Cancelled
          </span>
        );
      default:
        return <span className="text-xs text-gray-500">{status}</span>;
    }
  };

  // ── Render ───────────────────────────────────────────────────────────────────
  return (
    <div className="p-6 max-w-7xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">

      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-gray-900 dark:text-white">Billing & Payments</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Manage patient invoices, track payments, and review financial stats.</p>
        </div>
        <button
          onClick={openCreateModal}
          className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-xl shadow-lg shadow-blue-500/30 transition-all active:scale-95"
        >
          <Plus className="w-4 h-4" /> Create Invoice
        </button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {[
          { label: 'Total Revenue', value: `$${totalRevenue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`, icon: DollarSign, color: 'blue' },
          { label: 'Pending Amount', value: `$${pendingAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`, icon: Clock, color: 'amber' },
          { label: 'Invoices (this page)', value: String(invoices.length), icon: FileText, color: 'emerald' },
        ].map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="bg-white dark:bg-gray-800 rounded-2xl p-6 border border-gray-100 dark:border-gray-700 shadow-sm hover:shadow-md transition-shadow relative overflow-hidden group">
            <div className={`absolute -right-6 -top-6 w-24 h-24 bg-${color}-500/10 rounded-full group-hover:scale-150 transition-transform duration-500`} />
            <div className="flex items-center gap-4">
              <div className={`w-12 h-12 rounded-xl bg-${color}-50 dark:bg-${color}-900/20 flex items-center justify-center text-${color}-600 dark:text-${color}-400`}>
                <Icon className="w-6 h-6" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-500 dark:text-gray-400">{label}</p>
                <h3 className="text-2xl font-bold text-gray-900 dark:text-white">{value}</h3>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Table card */}
      <div className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 shadow-sm overflow-hidden">

        {/* Toolbar */}
        <div className="p-4 border-b border-gray-100 dark:border-gray-700 flex flex-col sm:flex-row gap-3 justify-between bg-gray-50/50 dark:bg-gray-800/50">
          <div className="relative max-w-md w-full">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input
              type="text"
              placeholder="Search invoices or patients…"
              value={searchTerm}
              onChange={e => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition-shadow text-gray-900 dark:text-white placeholder-gray-400"
            />
          </div>
          {/* Status filter pills */}
          <div className="flex items-center gap-2 flex-wrap">
            {STATUS_FILTERS.map(s => (
              <button
                key={s}
                onClick={() => { setStatusFilter(s); setCurrentPage(1); }}
                className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                  statusFilter === s
                    ? 'bg-blue-600 text-white'
                    : 'bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800'
                }`}
              >
                {s}
              </button>
            ))}
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-gray-50/50 dark:bg-gray-800/50 text-xs uppercase tracking-wider text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-700">
                <th className="px-6 py-4 font-medium">Invoice</th>
                <th className="px-6 py-4 font-medium">Patient</th>
                <th className="px-6 py-4 font-medium">Date</th>
                <th className="px-6 py-4 font-medium">Amount</th>
                <th className="px-6 py-4 font-medium">Status</th>
                <th className="px-6 py-4 font-medium text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
              {loading ? (
                <tr><td colSpan={6} className="px-6 py-16 text-center"><Loader2 className="w-6 h-6 text-blue-500 animate-spin mx-auto" /></td></tr>
              ) : invoices.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    <div className="flex flex-col items-center gap-3">
                      <Search className="w-8 h-8 text-gray-300 dark:text-gray-600" />
                      <p>No invoices found.</p>
                    </div>
                  </td>
                </tr>
              ) : (
                invoices.map((invoice, idx) => (
                  <tr key={invoice.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors group" style={{ animationDelay: `${idx * 40}ms` }}>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded bg-gray-100 dark:bg-gray-700 flex items-center justify-center text-gray-500 dark:text-gray-400">
                          <FileText className="w-4 h-4" />
                        </div>
                        <span className="font-medium text-gray-900 dark:text-white">{invoice.invoiceNumber}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">{invoice.patientName}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{invoice.date}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-white">${invoice.amount.toFixed(2)}</td>
                    <td className="px-6 py-4 whitespace-nowrap">{getStatusBadge(invoice.status)}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-right">
                      <div className="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <button onClick={() => openDetail(invoice.id)} className="p-1.5 text-gray-400 hover:text-blue-600 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors" title="View details">
                          <Eye className="w-4 h-4" />
                        </button>
                        {invoice.status === 'Pending' && (
                          <button onClick={() => handleCancelInvoice(invoice.id)} disabled={cancelling} className="p-1.5 text-gray-400 hover:text-red-600 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors" title="Cancel invoice">
                            <XCircle className="w-4 h-4" />
                          </button>
                        )}
                        <button className="p-1.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
                          <MoreVertical className="w-4 h-4" />
                        </button>
                      </div>
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
            <span>Page {pageMeta.pageNumber} of {pageMeta.totalPages} &nbsp;·&nbsp; {pageMeta.totalCount} total</span>
            <div className="flex items-center gap-2">
              <button onClick={() => setCurrentPage(p => p - 1)} disabled={!pageMeta.hasPreviousPage} className="p-1.5 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors" aria-label="Previous page"><ChevronLeft className="w-4 h-4" /></button>
              <button onClick={() => setCurrentPage(p => p + 1)} disabled={!pageMeta.hasNextPage} className="p-1.5 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors" aria-label="Next page"><ChevronRight className="w-4 h-4" /></button>
            </div>
          </div>
        )}
      </div>

      {/* ── Create Invoice Modal ───────────────────────────────────────────────── */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4" role="dialog" aria-modal="true" aria-labelledby="create-inv-title">
          <div className="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-800">
              <h2 id="create-inv-title" className="text-lg font-semibold text-gray-900 dark:text-white">Create Invoice</h2>
              <button onClick={closeCreateModal} className="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors" aria-label="Close"><X className="w-5 h-5" /></button>
            </div>
            <form onSubmit={handleCreateSubmit} className="px-6 py-5 space-y-5">
              {createError && (
                <div className="flex items-start gap-2 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 px-4 py-3 text-sm text-red-700 dark:text-red-400">
                  <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" /><span>{createError}</span>
                </div>
              )}

              {/* Patient */}
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Patient <span className="text-red-500">*</span></label>
                {patientsLoading ? (
                  <div className="flex items-center gap-2 text-sm text-gray-500"><Loader2 className="w-4 h-4 animate-spin" /> Loading patients…</div>
                ) : (
                  <select name="patientId" value={createForm.patientId} onChange={handleCreateField} required className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none">
                    <option value="">— Select patient —</option>
                    {patients.map(p => <option key={p.id} value={p.id}>{p.firstName} {p.lastName}</option>)}
                  </select>
                )}
              </div>

              {/* Due date / Tax / Discount */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Due Date</label>
                  <input type="date" name="dueDate" value={createForm.dueDate} onChange={handleCreateField} min={new Date().toISOString().split('T')[0]} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Tax %</label>
                  <input type="number" name="taxPercentage" value={createForm.taxPercentage} onChange={handleCreateField} min={0} max={100} step={0.01} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Discount ($)</label>
                  <input type="number" name="discountAmount" value={createForm.discountAmount} onChange={handleCreateField} min={0} step={0.01} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none" />
                </div>
              </div>

              {/* Notes */}
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notes</label>
                <textarea name="notes" value={createForm.notes} onChange={handleCreateField} rows={2} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none resize-none" placeholder="Optional notes…" />
              </div>

              {/* Line items */}
              <div>
                <p className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Line Items <span className="text-red-500">*</span></p>
                <div className="space-y-3">
                  {createForm.items.map((item, idx) => (
                    <div key={idx} className="border border-gray-200 dark:border-gray-700 rounded-lg p-3 space-y-3">
                      <div className="grid grid-cols-[1fr_1fr_auto] gap-3 items-start">
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Type</label>
                          <select name="itemType" value={item.itemType} onChange={e => handleLineItemChange(idx, e)} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none">
                            {BILLING_ITEM_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                          </select>
                        </div>
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Description <span className="text-red-500">*</span></label>
                          <input name="description" value={item.description} onChange={e => handleLineItemChange(idx, e)} required className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none" placeholder="e.g. General consultation" />
                        </div>
                        {createForm.items.length > 1 && (
                          <button type="button" onClick={() => removeLineItem(idx)} className="p-1.5 text-gray-400 hover:text-red-500 rounded transition-colors mt-5" aria-label="Remove"><X className="w-4 h-4" /></button>
                        )}
                      </div>
                      <div className="grid grid-cols-2 gap-3">
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Unit Price ($)</label>
                          <input type="number" name="unitPrice" value={item.unitPrice} onChange={e => handleLineItemChange(idx, e)} min={0} step={0.01} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none" />
                        </div>
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Quantity</label>
                          <input type="number" name="quantity" value={item.quantity} onChange={e => handleLineItemChange(idx, e)} min={1} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none" />
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
                <button type="button" onClick={addLineItem} className="mt-2 inline-flex items-center gap-1.5 text-sm text-blue-600 hover:text-blue-700 font-medium">
                  <Plus className="w-4 h-4" /> Add Line Item
                </button>
              </div>

              {/* Footer */}
              <div className="flex justify-end gap-3 pt-2">
                <button type="button" onClick={closeCreateModal} className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">Cancel</button>
                <button type="submit" disabled={creating || patientsLoading} className="inline-flex items-center gap-2 px-5 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm disabled:opacity-60 disabled:cursor-not-allowed transition-colors">
                  {creating && <Loader2 className="w-4 h-4 animate-spin" />}
                  {creating ? 'Creating…' : 'Create Invoice'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── Invoice Detail Slide-over ──────────────────────────────────────────── */}
      {(detailLoading || selectedInvoice) && (
        <div className="fixed inset-0 z-50 flex" role="dialog" aria-modal="true" aria-labelledby="detail-title">
          {/* Backdrop */}
          <div className="flex-1 bg-black/40 backdrop-blur-sm" onClick={closeDetail} />
          {/* Panel */}
          <div className="w-full max-w-lg bg-white dark:bg-gray-900 shadow-2xl overflow-y-auto flex flex-col">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-800 sticky top-0 bg-white dark:bg-gray-900 z-10">
              <h2 id="detail-title" className="text-lg font-semibold text-gray-900 dark:text-white">
                {selectedInvoice ? selectedInvoice.invoiceNumber : 'Loading…'}
              </h2>
              <button onClick={closeDetail} className="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors" aria-label="Close"><X className="w-5 h-5" /></button>
            </div>

            {detailLoading ? (
              <div className="flex-1 flex items-center justify-center"><Loader2 className="w-6 h-6 text-blue-500 animate-spin" /></div>
            ) : selectedInvoice && (
              <div className="px-6 py-5 space-y-6 flex-1">

                {/* Summary */}
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between"><span className="text-gray-500">Patient</span><span className="font-medium text-gray-900 dark:text-white">{selectedInvoice.patientName}</span></div>
                  <div className="flex justify-between"><span className="text-gray-500">Issue Date</span><span className="text-gray-900 dark:text-white">{new Date(selectedInvoice.issueDate).toLocaleDateString()}</span></div>
                  <div className="flex justify-between"><span className="text-gray-500">Due Date</span><span className="text-gray-900 dark:text-white">{new Date(selectedInvoice.dueDate).toLocaleDateString()}</span></div>
                  <div className="flex justify-between"><span className="text-gray-500">Status</span>{getStatusBadge(selectedInvoice.status)}</div>
                  <div className="flex justify-between pt-1 border-t border-gray-100 dark:border-gray-800"><span className="text-gray-500">Subtotal</span><span>${selectedInvoice.subTotal.toFixed(2)}</span></div>
                  {selectedInvoice.discountAmount > 0 && <div className="flex justify-between text-emerald-600"><span>Discount</span><span>-${selectedInvoice.discountAmount.toFixed(2)}</span></div>}
                  {selectedInvoice.taxAmount > 0 && <div className="flex justify-between"><span className="text-gray-500">Tax ({selectedInvoice.taxPercentage}%)</span><span>${selectedInvoice.taxAmount.toFixed(2)}</span></div>}
                  <div className="flex justify-between font-semibold text-base border-t border-gray-100 dark:border-gray-800 pt-1"><span className="text-gray-900 dark:text-white">Total</span><span className="text-gray-900 dark:text-white">${selectedInvoice.totalAmount.toFixed(2)}</span></div>
                  <div className="flex justify-between text-emerald-600"><span>Paid</span><span>${selectedInvoice.paidAmount.toFixed(2)}</span></div>
                  <div className="flex justify-between font-semibold text-blue-600"><span>Balance Due</span><span>${selectedInvoice.balanceDue.toFixed(2)}</span></div>
                </div>

                {/* Line items */}
                {selectedInvoice.items.length > 0 && (
                  <div>
                    <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Line Items</h3>
                    <div className="rounded-lg border border-gray-100 dark:border-gray-700 overflow-hidden">
                      <table className="w-full text-xs">
                        <thead className="bg-gray-50 dark:bg-gray-800 text-gray-500 uppercase tracking-wider">
                          <tr>
                            <th className="px-3 py-2 text-left font-medium">Description</th>
                            <th className="px-3 py-2 text-right font-medium">Qty</th>
                            <th className="px-3 py-2 text-right font-medium">Unit</th>
                            <th className="px-3 py-2 text-right font-medium">Total</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
                          {selectedInvoice.items.map(item => (
                            <tr key={item.id} className="bg-white dark:bg-gray-900">
                              <td className="px-3 py-2 text-gray-800 dark:text-gray-200">{item.description}</td>
                              <td className="px-3 py-2 text-right text-gray-600 dark:text-gray-400">{item.quantity}</td>
                              <td className="px-3 py-2 text-right text-gray-600 dark:text-gray-400">${item.unitPrice.toFixed(2)}</td>
                              <td className="px-3 py-2 text-right font-medium text-gray-900 dark:text-white">${item.totalPrice.toFixed(2)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                {/* Payment history */}
                {selectedInvoice.payments.length > 0 && (
                  <div>
                    <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Payment History</h3>
                    <div className="space-y-2">
                      {selectedInvoice.payments.map(p => (
                        <div key={p.id} className="flex items-center justify-between text-sm p-3 rounded-lg bg-gray-50 dark:bg-gray-800 border border-gray-100 dark:border-gray-700">
                          <div className="flex items-center gap-2">
                            <CreditCard className="w-4 h-4 text-gray-400" />
                            <div>
                              <p className="font-medium text-gray-900 dark:text-white">{p.method}</p>
                              <p className="text-xs text-gray-400">{new Date(p.paymentDate).toLocaleDateString()}{p.transactionReference && ` · ${p.transactionReference}`}</p>
                            </div>
                          </div>
                          <span className="font-semibold text-emerald-600">${p.amount.toFixed(2)}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Record Payment form (only for Pending invoices) */}
                {selectedInvoice.status === 'Pending' && selectedInvoice.balanceDue > 0 && (
                  <div className="border border-blue-100 dark:border-blue-800 rounded-xl p-4 bg-blue-50/50 dark:bg-blue-900/10">
                    <h3 className="text-sm font-semibold text-blue-800 dark:text-blue-300 mb-3 flex items-center gap-2">
                      <Banknote className="w-4 h-4" /> Record Payment
                    </h3>
                    <form onSubmit={handleRecordPayment} className="space-y-3">
                      {paymentError && (
                        <div className="flex items-start gap-2 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 px-3 py-2 text-xs text-red-700 dark:text-red-400">
                          <AlertCircle className="w-3.5 h-3.5 mt-0.5 shrink-0" /><span>{paymentError}</span>
                        </div>
                      )}
                      <div className="grid grid-cols-2 gap-3">
                        <div>
                          <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Amount ($) <span className="text-red-500">*</span></label>
                          <input type="number" name="amount" value={paymentForm.amount || ''} onChange={handlePaymentField} min={0.01} max={selectedInvoice.balanceDue} step={0.01} required className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none" placeholder={selectedInvoice.balanceDue.toFixed(2)} />
                        </div>
                        <div>
                          <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Method</label>
                          <select name="method" value={paymentForm.method} onChange={handlePaymentField} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none">
                            {PAYMENT_METHODS.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
                          </select>
                        </div>
                      </div>
                      <div>
                        <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Transaction Reference</label>
                        <input name="transactionReference" value={paymentForm.transactionReference} onChange={handlePaymentField} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none" placeholder="Optional ref / receipt no." />
                      </div>
                      <button type="submit" disabled={paying} className="w-full inline-flex items-center justify-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors disabled:opacity-60 disabled:cursor-not-allowed">
                        {paying ? <><Loader2 className="w-4 h-4 animate-spin" /> Processing…</> : <><Banknote className="w-4 h-4" /> Process Payment</>}
                      </button>
                    </form>
                  </div>
                )}

                {/* Cancel action */}
                {selectedInvoice.status === 'Pending' && (
                  <button onClick={() => handleCancelInvoice(selectedInvoice.id)} disabled={cancelling} className="w-full px-4 py-2 text-sm font-medium text-red-600 border border-red-200 dark:border-red-800 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/10 transition-colors disabled:opacity-60">
                    {cancelling ? 'Cancelling…' : 'Cancel Invoice'}
                  </button>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default Billing;
