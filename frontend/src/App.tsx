import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Profile from './pages/Profile';
import MySkills from './pages/MySkills';
import MyExchanges from './pages/MyExchanges';
import Browse from './pages/Browse';
import ViewProfile from './pages/ViewProfile';
import ProtectedRoute from './components/ProtectedRoute';
import { AppInitializer } from './components/AppInitializer';
import { ToastProvider } from './contexts/ToastContext';
import { NotificationProvider } from './contexts/NotificationContext';

function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <NotificationProvider>
          <AppInitializer>
            <Routes>
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Register />} />
              <Route element={<ProtectedRoute />}>
                <Route path="/dashboard" element={<Dashboard />} />
                <Route path="/profile" element={<Profile />} />
                <Route path="/profile/:userId" element={<ViewProfile />} />
                <Route path="/skills" element={<MySkills />} />
                <Route path="/exchanges" element={<MyExchanges />} />
                <Route path="/browse" element={<Browse />} />
              </Route>
            </Routes>
          </AppInitializer>
        </NotificationProvider>
      </ToastProvider>
    </BrowserRouter>
  );
}

export default App;