import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import api from '../services/api';
import { Users, Activity, FileText, Plus, CalendarDays, Receipt, Clock, CheckCircle, XCircle, Loader2, ArrowRight } from 'lucide-react';

interface RecentAppointment {
  id: string; patientName: string; doctorName: string; appointmentDate: string; status: number;
}

interface RecentInvoice {
  id: string; invoiceNumber: string; patientName: string; totalAmount: number; status: string;
}

const APPT_STATUS_LABELS: Record<number, string> = { 0: 'Scheduled', 1: 'Completed', 2: 'Cancelled', 3: 'NoShow' };
const APPT_STATUS_STYLES: Record<number, string> = {
  0: 'bg-blue-100 text-blue-700', 1: 'bg-emerald-100 text-emerald-700',
  2: 'bg-rose-100 text-rose-700', 3: 'bg-amber-100 text-amber-700',
};

export default function Dashboard() {
  const [totalPatients, setTotalPatients] = useState(0);
  const [todayAppointments, setTodayAppointments] = useState(0);
  const [pendingInvoices, setPendingInvoices] = useState(0);
  const [statsLoading, setStatsLoading] = useState(true);
  const [recentAppointments, setRecentAppointments] = useState<RecentAppointment[]>([]);
  const [recentInvoices, setRecentInvoices] = useState<RecentInvoice[]>([]);
  const [activityLoading, setActivityLoading] = useState(true);

  const fetchStats = useCallback(async () => {
    setStatsLoading(true);
    try {
      const [patientsRes, apptsRes, invoicesRes] = await Promise.all([
        api.get('/Patient', { params: { pageNumber: 1, pageSize: 1 } }).catch(() => ({ data: null })),
        api.get('/Appointment', { params: { pageNumber: 1, pageSize: 50 } }).catch(() => ({ data: null })),
        api.get('/Invoice', { params: { pageNumber: 1, pageSize: 1, status: 'Pending' } }).catch(() => ({ data: null })),
      ]);
      setTotalPatients(patientsRes.data?.data?.totalCount ?? 0);
      const appts: any[] = apptsRes.data?.data?.items ?? [];
      const today = new Date().toISOString().split('T')[0];
      setTodayAppointments(appts.filter((a: any) => (a.appointmentDate ?? '').startsWith(today)).length);
      setPendingInvoices(invoicesRes.data?.data?.totalCount ?? 0);
    } catch (err) { console.error('Error fetching stats:', err); }  
    finally { setStatsLoading(false); }
  }, []);

  const fetchActivity = useCallback(async () => {
    setActivityLoading(true);
    try {
      const [apptsRes, invoicesRes] = await Promise.all([
        api.get('/Appointment', { params: { pageNumber: 1, pageSize: 5, sortBy: 'appointmentDate', isDescending: true } }).catch(() => ({ data: null })),
        api.get('/Invoice',     { params: { pageNumber: 1, pageSize: 5, sortBy: 'issueDate',        isDescending: true } }).catch(() => ({ data: null })),
      ]);
      setRecentAppointments((apptsRes.data?.data?.items ?? []).map((a: any) => ({
        id: a.id, patientName: a.patientName, doctorName: a.doctorName,
        appointmentDate: a.appointmentDate, status: a.status,
      })));
      setRecentInvoices((invoicesRes.data?.data?.items ?? []).map((i: any) => ({
        id: i.id, invoiceNumber: i.invoiceNumber, patientName: i.patientName,
        totalAmount: i.totalAmount ?? 0, status: i.status ?? 'Pending',
      })));
    } catch (err) { console.error('Error fetching activity:', err); }
    finally { setActivityLoading(false); }
  }, []);

  useEffect(() => { fetchStats(); fetchActivity(); }, [fetchStats, fetchActivity]);

  const invoiceStatusBadge = (status: string) => {
    switch (status) {
      case 'Paid':      return (<span className='inline-flex items-center gap-1 text-xs font-medium text-emerald-700 bg-emerald-100 px-2 py-0.5 rounded-full'><CheckCircle className='w-3 h-3' /> Paid</span>);
      case 'Pending':   return (<span className='inline-flex items-center gap-1 text-xs font-medium text-amber-700 bg-amber-100 px-2 py-0.5 rounded-full'><Clock className='w-3 h-3' /> Pending</span>);
      case 'Cancelled': return (<span className='inline-flex items-center gap-1 text-xs font-medium text-rose-700 bg-rose-100 px-2 py-0.5 rounded-full'><XCircle className='w-3 h-3' /> Cancelled</span>);
      default:          return (<span className='text-xs text-gray-400'>{status}</span>);
    }
  };

  return (
    <div className='space-y-6'>
      <div className='flex items-center justify-between'>
        <div>
          <h2 className='text-2xl font-bold text-gray-900'>Dashboard Overview</h2>
          <p className='mt-1 text-sm text-gray-500'>Welcome back to CareConnect admin portal.</p>
        </div>
        <Link to='/appointments' className='inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors'>
          <Plus className='h-5 w-5 mr-2 -ml-1' /> New Appointment
        </Link>
      </div>

      <div className='grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3'>
        <div className='bg-white overflow-hidden shadow-sm rounded-xl border border-gray-100 hover:shadow-md transition-shadow'>
          <div className='p-5'><div className='flex items-center'>
            <div className='flex-shrink-0'><div className='bg-blue-100 rounded-lg p-3'><Users className='h-6 w-6 text-blue-600' /></div></div>
            <div className='ml-5 w-0 flex-1'><dl><dt className='text-sm font-medium text-gray-500 truncate'>Total Patients</dt><dd className='text-2xl font-semibold text-gray-900'>{statsLoading ? <Loader2 className='h-5 w-5 animate-spin text-gray-400 inline' /> : totalPatients}</dd></dl></div>
          </div></div>
          <div className='bg-gray-50 px-5 py-3'><Link to='/patients' className='text-sm font-medium text-blue-600 hover:text-blue-500 flex items-center gap-1'>View all <ArrowRight className='w-3.5 h-3.5' /></Link></div>
        </div>

        <div className='bg-white overflow-hidden shadow-sm rounded-xl border border-gray-100 hover:shadow-md transition-shadow'>
          <div className='p-5'><div className='flex items-center'>
            <div className='flex-shrink-0'><div className='bg-green-100 rounded-lg p-3'><Activity className='h-6 w-6 text-green-600' /></div></div>
            <div className='ml-5 w-0 flex-1'><dl><dt className='text-sm font-medium text-gray-500 truncate'>Today's Appointments</dt><dd className='text-2xl font-semibold text-gray-900'>{statsLoading ? <Loader2 className='h-5 w-5 animate-spin text-gray-400 inline' /> : todayAppointments}</dd></dl></div>
          </div></div>
          <div className='bg-gray-50 px-5 py-3'><Link to='/appointments' className='text-sm font-medium text-blue-600 hover:text-blue-500 flex items-center gap-1'>View schedule <ArrowRight className='w-3.5 h-3.5' /></Link></div>
        </div>

        <div className='bg-white overflow-hidden shadow-sm rounded-xl border border-gray-100 hover:shadow-md transition-shadow'>
          <div className='p-5'><div className='flex items-center'>
            <div className='flex-shrink-0'><div className='bg-purple-100 rounded-lg p-3'><FileText className='h-6 w-6 text-purple-600' /></div></div>
            <div className='ml-5 w-0 flex-1'><dl><dt className='text-sm font-medium text-gray-500 truncate'>Pending Invoices</dt><dd className='text-2xl font-semibold text-gray-900'>{statsLoading ? <Loader2 className='h-5 w-5 animate-spin text-gray-400 inline' /> : pendingInvoices}</dd></dl></div>
          </div></div>
          <div className='bg-gray-50 px-5 py-3'><Link to='/billing' className='text-sm font-medium text-blue-600 hover:text-blue-500 flex items-center gap-1'>Process billing <ArrowRight className='w-3.5 h-3.5' /></Link></div>
        </div>
      </div>

      <div className='grid grid-cols-1 lg:grid-cols-2 gap-6'>
        <div className='bg-white shadow-sm rounded-xl border border-gray-100 overflow-hidden'>
          <div className='px-6 py-4 border-b border-gray-100 flex items-center justify-between'>
            <h3 className='text-base font-semibold text-gray-900 flex items-center gap-2'><CalendarDays className='w-4 h-4 text-gray-400' /> Recent Appointments</h3>
            <Link to='/appointments' className='text-xs text-blue-600 hover:text-blue-500 font-medium'>View all</Link>
          </div>
          {activityLoading ? (<div className='flex items-center justify-center h-32'><Loader2 className='w-5 h-5 text-blue-500 animate-spin' /></div>)
          : recentAppointments.length === 0 ? (<div className='flex items-center justify-center h-32 text-sm text-gray-400'>No appointments yet</div>)
          : (<ul className='divide-y divide-gray-50'>{recentAppointments.map(appt => (<li key={appt.id} className='px-6 py-3 flex items-center justify-between hover:bg-gray-50 transition-colors'>
            <div className='min-w-0 flex-1'>
              <p className='text-sm font-medium text-gray-900 truncate'>{appt.patientName}</p>
              <p className='text-xs text-gray-400 truncate'>Dr. {appt.doctorName} {' · '} {new Date(appt.appointmentDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}</p>
            </div>
            <span className={APPT_STATUS_STYLES[appt.status] + ' ml-3 shrink-0 text-xs font-medium px-2 py-0.5 rounded-full'}>{APPT_STATUS_LABELS[appt.status] ?? 'Unknown'}</span>
          </li>))}</ul>)}
        </div>

        <div className='bg-white shadow-sm rounded-xl border border-gray-100 overflow-hidden'>
          <div className='px-6 py-4 border-b border-gray-100 flex items-center justify-between'>
            <h3 className='text-base font-semibold text-gray-900 flex items-center gap-2'><Receipt className='w-4 h-4 text-gray-400' /> Recent Invoices</h3>
            <Link to='/billing' className='text-xs text-blue-600 hover:text-blue-500 font-medium'>View all</Link>
          </div>
          {activityLoading ? (<div className='flex items-center justify-center h-32'><Loader2 className='w-5 h-5 text-blue-500 animate-spin' /></div>)
          : recentInvoices.length === 0 ? (<div className='flex items-center justify-center h-32 text-sm text-gray-400'>No invoices yet</div>)
          : (<ul className='divide-y divide-gray-50'>{recentInvoices.map(inv => (<li key={inv.id} className='px-6 py-3 flex items-center justify-between hover:bg-gray-50 transition-colors'>
            <div className='min-w-0 flex-1'>
              <p className='text-sm font-medium text-gray-900 truncate'>{inv.invoiceNumber}</p>
              <p className='text-xs text-gray-400 truncate'>{inv.patientName}</p>
            </div>
            <div className='ml-3 shrink-0 flex items-center gap-2'>
              <span className='text-sm font-semibold text-gray-900'>{'\$'}{inv.totalAmount.toFixed(2)}</span>
              {invoiceStatusBadge(inv.status)}
            </div>
          </li>))}</ul>)}
        </div>
      </div>
    </div>
  );
}