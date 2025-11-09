import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

/**
 * 認證 Hook - 提供登入、登出和使用者資訊
 */
export const useAuth = () => {
  const navigate = useNavigate();
  const { user, isLoading, error, login, logout, fetchUser, clearError, isAdmin } = useAuthStore();

  // 初始化時檢查使用者狀態
  useEffect(() => {
    fetchUser();
  }, [fetchUser]);

  const handleLogin = async (username: string, password: string) => {
    try {
      await login(username, password);
      navigate('/dashboard');
    } catch (error) {
      // 錯誤已在 store 中處理
    }
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return {
    user,
    isLoading,
    error,
    isAuthenticated: !!user,
    isAdmin: isAdmin(),
    login: handleLogin,
    logout: handleLogout,
    clearError,
  };
};
