import React from 'react';
import { Outlet, Link, useNavigate } from 'react-router-dom';
import { LayoutDashboard, Users, Activity, FileText, Settings, LogOut, HeartPulse } from 'lucide-react';

export default function DashboardLayout() {
  const navigate = useNavigate();

  const handleLogout = () => {
    // Basic logout logic to navigate back to login
    navigate('/login');
  };

  return (
    <div className="flex h-screen bg-gray-50 overflow-hidden">
      {/* Sidebar */}
      <aside className="w-64 bg-white border-r border-gray-200 flex flex-col transition-all duration-300">
        <div className="h-16 flex items-center px-6 border-b border-gray-200">
          <HeartPulse className="h-8 w-8 text-blue-600 mr-2" />
          <span className="text-xl font-bold text-gray-800">CareConnect</span>
        </div>
        
        <nav className="flex-1 overflow-y-auto py-4">
          <ul className="space-y-1 px-3">
            <li>
              <Link to="/" className="flex items-center px-3 py-2.5 bg-blue-50 text-blue-700 rounded-lg group transition-colors">
                <LayoutDashboard className="h-5 w-5 mr-3" />
                <span className="font-medium">Dashboard</span>
              </Link>
            </li>
            <li>
              <Link to="/patients" className="flex items-center px-3 py-2.5 text-gray-700 hover:bg-gray-100 rounded-lg group transition-colors">
                <Users className="h-5 w-5 mr-3 text-gray-400 group-hover:text-gray-600" />
                <span className="font-medium">Patients</span>
              </Link>
            </li>
            <li>
              <Link to="/appointments" className="flex items-center px-3 py-2.5 text-gray-700 hover:bg-gray-100 rounded-lg group transition-colors">
                <Activity className="h-5 w-5 mr-3 text-gray-400 group-hover:text-gray-600" />
                <span className="font-medium">Appointments</span>
              </Link>
            </li>
            <li>
              <Link to="/billing" className="flex items-center px-3 py-2.5 text-gray-700 hover:bg-gray-100 rounded-lg group transition-colors">
                <FileText className="h-5 w-5 mr-3 text-gray-400 group-hover:text-gray-600" />
                <span className="font-medium">Billing</span>
              </Link>
            </li>
          </ul>
        </nav>
        
        <div className="p-4 border-t border-gray-200">
          <button 
            onClick={handleLogout}
            className="flex items-center w-full px-3 py-2.5 text-red-600 hover:bg-red-50 rounded-lg transition-colors"
          >
            <LogOut className="h-5 w-5 mr-3" />
            <span className="font-medium">Logout</span>
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Header */}
        <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6">
          <h1 className="text-xl font-semibold text-gray-800">Admin Portal</h1>
          <div className="flex items-center space-x-4">
            <div className="flex items-center space-x-2">
              <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 font-bold">
                A
              </div>
              <span className="text-sm font-medium text-gray-700 hidden sm:block">Admin User</span>
            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-y-auto p-6 bg-gray-50">
          <div className="max-w-7xl mx-auto">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
