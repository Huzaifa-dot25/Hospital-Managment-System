import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import DashboardLayout from './layouts/DashboardLayout';
import Dashboard from './pages/Dashboard';
import Login from './pages/Login';
import Billing from './pages/Billing';
import Patients from './pages/Patients';
import Appointments from './pages/Appointments';
import { AuthProvider, useAuth } from './context/AuthContext';
import RoleGuard from './components/RoleGuard';

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
};

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          
          {/* Base Layout requires basic authentication */}
          <Route 
            path="/" 
            element={
              <ProtectedRoute>
                <DashboardLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<Dashboard />} />
            
            {/* Example of Role-Based Route Guards */}
            <Route element={<RoleGuard allowedRoles={['Admin', 'Doctor', 'Nurse', 'Receptionist']} />}>
              <Route path="patients" element={<Patients />} />
            </Route>
            
            <Route element={<RoleGuard allowedRoles={['Admin', 'Doctor', 'Receptionist']} />}>
              <Route path="appointments" element={<Appointments />} />
            </Route>
            
            <Route element={<RoleGuard allowedRoles={['Admin', 'Accountant', 'Cashier']} />}>
              <Route path="billing" element={<Billing />} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
