import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/Card';
import { LoginForm } from '../components/auth/LoginForm';
import { useAuth } from '../hooks/useAuth';
import { Navigate } from 'react-router-dom';

export const LoginPage = () => {
  const { login, error, isLoading, isAuthenticated } = useAuth();

  // 如果已登入,重導向到儀表板
  if (isAuthenticated) {
    return <Navigate to='/dashboard' replace />;
  }

  return (
    <div className='flex min-h-screen items-center justify-center bg-gray-50 px-4'>
      <Card className='w-full max-w-md'>
        <CardHeader className='space-y-1 text-center'>
          <CardTitle className='text-3xl font-bold'>JWT 認證系統</CardTitle>
          <CardDescription>請輸入您的帳號密碼登入</CardDescription>
        </CardHeader>
        <CardContent>
          <LoginForm onSubmit={login} error={error} isLoading={isLoading} />
        </CardContent>
      </Card>
    </div>
  );
};
