import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

export const AdminRoute: React.FC = () => {
  const { isAdmin, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className='flex min-h-screen items-center justify-center'>
        <div className='text-center'>
          <div className='mb-4 inline-block h-12 w-12 animate-spin rounded-full border-4 border-solid border-primary-600 border-r-transparent'></div>
          <p className='text-gray-600'>載入中...</p>
        </div>
      </div>
    );
  }

  if (!isAdmin) {
    return <Navigate to='/dashboard' replace />;
  }

  return <Outlet />;
};
