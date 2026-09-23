import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import DashboardLayout from './layouts/DashboardLayout';
import Dashboard from './pages/Dashboard';
import Login from './pages/Login';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        
        {/* Protected Routes would go here, wrapped in an AuthGuard. For now we just use the Layout */}
        <Route path="/" element={<DashboardLayout />}>
          <Route index element={<Dashboard />} />
          {/* Add more routes here as we build them out */}
          <Route path="patients" element={<div className="p-4">Patients Page coming soon...</div>} />
          <Route path="appointments" element={<div className="p-4">Appointments Page coming soon...</div>} />
          <Route path="billing" element={<div className="p-4">Billing Page coming soon...</div>} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
