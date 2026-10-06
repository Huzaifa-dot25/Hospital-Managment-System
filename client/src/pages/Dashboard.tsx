import { useState, useEffect } from 'react';
import api from '../services/api';
import { Users, Activity, FileText, Plus } from 'lucide-react';

export default function Dashboard() {
  const [totalPatients, setTotalPatients] = useState(0);
  const [todayAppointments, setTodayAppointments] = useState(0);
  const [pendingInvoices, setPendingInvoices] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        // All three in parallel; individual failures don't break the whole dashboard
        const [patientsRes, apptsRes, invoicesRes] = await Promise.all([
          api.get('/Patient').catch(() => ({ data: null })),
          api.get('/Appointment').catch(() => ({ data: null })),
          api.get('/Invoice').catch(() => ({ data: null })),
        ]);

        // Backend shape: ApiResponse<PagedResponse<T>>
        // → response.data        = ApiResponse wrapper  { data: PagedResponse }
        // → response.data.data   = PagedResponse         { items: T[], totalCount }
        const patients   = patientsRes.data?.data?.items   ?? [];
        const appts      = apptsRes.data?.data?.items      ?? [];
        const invoices   = invoicesRes.data?.data?.items   ?? [];

        setTotalPatients(patientsRes.data?.data?.totalCount ?? patients.length);

        const today = new Date().toISOString().split('T')[0];
        const todayCount = appts.filter((a: any) => {
          const dateStr = a.appointmentDate || a.date || a.scheduledAt || '';
          return dateStr.startsWith(today);
        }).length;
        setTodayAppointments(todayCount);

        const pendingCount = invoices.filter((i: any) => i.status === 'Pending').length;
        setPendingInvoices(pendingCount);
      } catch (error) {
        console.error('Error fetching dashboard data:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Dashboard Overview</h2>
          <p className="mt-1 text-sm text-gray-500">Welcome back to CareConnect admin portal.</p>
        </div>
        <button className="inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors">
          <Plus className="h-5 w-5 mr-2 -ml-1" />
          New Appointment
        </button>
      </div>

      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {/* Stat Card 1 */}
        <div className="bg-white overflow-hidden shadow-sm rounded-xl border border-gray-100 hover:shadow-md transition-shadow">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <div className="bg-blue-100 rounded-lg p-3">
                  <Users className="h-6 w-6 text-blue-600" />
                </div>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Total Patients</dt>
                  <dd className="text-2xl font-semibold text-gray-900">{loading ? '...' : totalPatients}</dd>
                </dl>
              </div>
            </div>
          </div>
          <div className="bg-gray-50 px-5 py-3">
            <div className="text-sm">
              <a href="#" className="font-medium text-blue-600 hover:text-blue-500">View all</a>
            </div>
          </div>
        </div>

        {/* Stat Card 2 */}
        <div className="bg-white overflow-hidden shadow-sm rounded-xl border border-gray-100 hover:shadow-md transition-shadow">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <div className="bg-green-100 rounded-lg p-3">
                  <Activity className="h-6 w-6 text-green-600" />
                </div>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Today's Appointments</dt>
                  <dd className="text-2xl font-semibold text-gray-900">{loading ? '...' : todayAppointments}</dd>
                </dl>
              </div>
            </div>
          </div>
          <div className="bg-gray-50 px-5 py-3">
            <div className="text-sm">
              <a href="#" className="font-medium text-blue-600 hover:text-blue-500">View schedule</a>
            </div>
          </div>
        </div>

        {/* Stat Card 3 */}
        <div className="bg-white overflow-hidden shadow-sm rounded-xl border border-gray-100 hover:shadow-md transition-shadow">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <div className="bg-purple-100 rounded-lg p-3">
                  <FileText className="h-6 w-6 text-purple-600" />
                </div>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Pending Invoices</dt>
                  <dd className="text-2xl font-semibold text-gray-900">{loading ? '...' : pendingInvoices}</dd>
                </dl>
              </div>
            </div>
          </div>
          <div className="bg-gray-50 px-5 py-3">
            <div className="text-sm">
              <a href="#" className="font-medium text-blue-600 hover:text-blue-500">Process billing</a>
            </div>
          </div>
        </div>
      </div>
      
      <div className="bg-white shadow-sm rounded-xl border border-gray-100 overflow-hidden mt-8">
        <div className="px-6 py-5 border-b border-gray-100">
          <h3 className="text-lg font-medium leading-6 text-gray-900">Recent Activity</h3>
        </div>
        <div className="p-6 flex justify-center items-center h-48 text-gray-400">
          Activity chart placeholder
        </div>
      </div>
    </div>
  );
}
