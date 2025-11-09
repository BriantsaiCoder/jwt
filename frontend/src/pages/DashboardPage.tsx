import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/Card';
import { useAuth } from '../hooks/useAuth';
import { useApiAuto } from '../hooks/useApi';
import { userService } from '../services/user.service';
import { formatRelativeTime } from '../utils';

export const DashboardPage = () => {
  const { user } = useAuth();
  const { data: activities, isLoading, error } = useApiAuto(() => userService.getActivities(10), []);

  return (
    <div className='space-y-6'>
      <div>
        <h1 className='text-3xl font-bold text-gray-900'>儀表板</h1>
        <p className='mt-2 text-gray-600'>歡迎回來, {user?.username}!</p>
      </div>

      <div className='grid gap-6 md:grid-cols-2 lg:grid-cols-3'>
        <Card>
          <CardHeader>
            <CardTitle className='text-lg'>使用者資訊</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className='space-y-2'>
              <div>
                <dt className='text-sm font-medium text-gray-500'>使用者名稱</dt>
                <dd className='text-base text-gray-900'>{user?.username}</dd>
              </div>
              <div>
                <dt className='text-sm font-medium text-gray-500'>角色</dt>
                <dd className='text-base text-gray-900'>{user?.roles.join(', ')}</dd>
              </div>
              <div>
                <dt className='text-sm font-medium text-gray-500'>建立時間</dt>
                <dd className='text-base text-gray-900'>{user?.createdAt && formatRelativeTime(user.createdAt)}</dd>
              </div>
            </dl>
          </CardContent>
        </Card>

        <Card className='md:col-span-2'>
          <CardHeader>
            <CardTitle className='text-lg'>最近活動</CardTitle>
            <CardDescription>您最近的系統活動記錄</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading && <p className='text-gray-500'>載入中...</p>}
            {error && <p className='text-red-600'>{error}</p>}
            {activities && activities.activities.length === 0 && <p className='text-gray-500'>尚無活動記錄</p>}
            {activities && activities.activities.length > 0 && (
              <div className='space-y-3'>
                {activities.activities.map((activity, index) => (
                  <div key={index} className='flex items-start space-x-3 border-b pb-3 last:border-b-0'>
                    <div className='flex-1'>
                      <p className='text-sm font-medium text-gray-900'>{activity.action}</p>
                      {activity.description && <p className='text-sm text-gray-500'>{activity.description}</p>}
                      <p className='mt-1 text-xs text-gray-400'>{formatRelativeTime(activity.timestamp)}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
