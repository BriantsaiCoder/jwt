import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Alert, AlertDescription } from '../../components/ui/Alert';
import { useApiAuto, useApi } from '../../hooks/useApi';
import { adminService } from '../../services/admin.service';
import { formatDateTime } from '../../utils';

export const AdminUsersPage = () => {
  const { data: usersData, isLoading, error, execute: refreshUsers } = useApiAuto(() => adminService.getAllUsers(), []);

  const { execute: revokeTokens, isLoading: isRevoking } = useApi(adminService.revokeUserTokens, {
    onSuccess: () => {
      alert('Token 已成功撤銷!');
      refreshUsers();
    },
  });

  const handleRevokeTokens = async (userId: string, username: string) => {
    if (confirm(`確定要撤銷使用者 "${username}" 的所有 Token 嗎?`)) {
      await revokeTokens(userId);
    }
  };

  return (
    <div className='space-y-6'>
      <div className='flex items-center justify-between'>
        <div>
          <h1 className='text-3xl font-bold text-gray-900'>使用者管理</h1>
          <p className='mt-2 text-gray-600'>管理系統中的所有使用者</p>
        </div>
        <Button onClick={refreshUsers} disabled={isLoading}>
          重新整理
        </Button>
      </div>

      {error && (
        <Alert variant='error'>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {isLoading && (
        <div className='text-center'>
          <p className='text-gray-500'>載入中...</p>
        </div>
      )}

      {usersData && (
        <Card>
          <CardHeader>
            <CardTitle>所有使用者</CardTitle>
            <CardDescription>共 {usersData.totalCount} 位使用者</CardDescription>
          </CardHeader>
          <CardContent>
            <div className='overflow-x-auto'>
              <table className='w-full'>
                <thead>
                  <tr className='border-b border-gray-200 text-left'>
                    <th className='pb-3 text-sm font-medium text-gray-500'>使用者名稱</th>
                    <th className='pb-3 text-sm font-medium text-gray-500'>角色</th>
                    <th className='pb-3 text-sm font-medium text-gray-500'>建立時間</th>
                    <th className='pb-3 text-sm font-medium text-gray-500'>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {usersData.users.map((user) => (
                    <tr key={user.id} className='border-b border-gray-100 last:border-b-0'>
                      <td className='py-3 text-sm text-gray-900'>{user.username}</td>
                      <td className='py-3'>
                        <span className='inline-flex rounded-full bg-primary-100 px-2 py-1 text-xs font-semibold text-primary-700'>
                          {user.roles.join(', ')}
                        </span>
                      </td>
                      <td className='py-3 text-sm text-gray-500'>{formatDateTime(user.createdAt)}</td>
                      <td className='py-3'>
                        <Button
                          variant='danger'
                          size='sm'
                          onClick={() => handleRevokeTokens(user.id, user.username)}
                          disabled={isRevoking}>
                          撤銷 Token
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};
