import { createBrowserRouter, Navigate } from 'react-router-dom';
import { LoginPage } from '../pages/LoginPage';
import { DashboardPage } from '../pages/DashboardPage';
import { ProfilePage } from '../pages/ProfilePage';
import { AdminUsersPage } from '../pages/admin/AdminUsersPage';
import { AdminStatsPage } from '../pages/admin/AdminStatsPage';
import { ProtectedRoute } from '../components/auth/ProtectedRoute';
import { AdminRoute } from '../components/auth/AdminRoute';
import { Layout } from '../components/layout/Layout';

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: <Navigate to='/dashboard' replace />,
  },
  {
    path: '/',
    element: (
      <ProtectedRoute>
        <Layout />
      </ProtectedRoute>
    ),
    children: [
      {
        path: 'dashboard',
        element: <DashboardPage />,
      },
      {
        path: 'profile',
        element: <ProfilePage />,
      },
      {
        path: 'admin',
        element: <AdminRoute />,
        children: [
          {
            path: 'users',
            element: <AdminUsersPage />,
          },
          {
            path: 'stats',
            element: <AdminStatsPage />,
          },
        ],
      },
    ],
  },
]);
