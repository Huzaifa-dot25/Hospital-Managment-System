import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import DashboardLayout from './layouts/DashboardLayout';
import Dashboard from './pages/Dashboard';
import Login from './pages/Login';
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
              <Route path="patients" element={<div className="p-4">Patients Page coming soon...</div>} />
            </Route>
            
            <Route element={<RoleGuard allowedRoles={['Admin', 'Doctor', 'Receptionist']} />}>
              <Route path="appointments" element={<div className="p-4">Appointments Page coming soon...</div>} />
            </Route>
            
            <Route element={<RoleGuard allowedRoles={['Admin', 'Accountant', 'Cashier']} />}>
              <Route path="billing" element={<div className="p-4">Billing Page coming soon...</div>} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
