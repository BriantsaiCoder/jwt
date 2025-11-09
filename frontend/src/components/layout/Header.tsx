import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { Button } from '../ui/Button';
import { cn } from '../../utils';

export const Header = () => {
  const { user, logout, isAdmin } = useAuth();
  const location = useLocation();

  const navItems = [
    { path: '/dashboard', label: '儀表板', adminOnly: false },
    { path: '/profile', label: '個人資料', adminOnly: false },
    { path: '/admin/users', label: '使用者管理', adminOnly: true },
    { path: '/admin/stats', label: '系統統計', adminOnly: true },
  ];

  return (
    <header className='border-b border-gray-200 bg-white'>
      <div className='mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8'>
        <div className='flex items-center space-x-8'>
          <Link to='/dashboard' className='text-xl font-bold text-primary-600'>
            JWT Auth
          </Link>
          <nav className='hidden space-x-4 md:flex'>
            {navItems
              .filter((item) => !item.adminOnly || isAdmin)
              .map((item) => (
                <Link
                  key={item.path}
                  to={item.path}
                  className={cn(
                    'rounded-md px-3 py-2 text-sm font-medium transition-colors',
                    location.pathname === item.path
                      ? 'bg-primary-100 text-primary-700'
                      : 'text-gray-700 hover:bg-gray-100',
                  )}>
                  {item.label}
                </Link>
              ))}
          </nav>
        </div>

        <div className='flex items-center space-x-4'>
          <div className='text-sm'>
            <p className='font-medium text-gray-900'>{user?.username}</p>
            <p className='text-xs text-gray-500'>{user?.roles.join(', ')}</p>
          </div>
          <Button variant='outline' size='sm' onClick={logout}>
            登出
          </Button>
        </div>
      </div>
    </header>
  );
};
