import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Alert, AlertDescription } from '../../components/ui/Alert';
import { useApiAuto, useApi } from '../../hooks/useApi';
import { adminService } from '../../services/admin.service';

export const AdminStatsPage = () => {
  const { data: stats, isLoading, error, execute: refreshStats } = useApiAuto(() => adminService.getStats(), []);

  const { execute: clearCache, isLoading: isClearing } = useApi(adminService.clearCache, {
    onSuccess: () => {
      alert('快取已成功清除!');
      refreshStats();
    },
  });

  const handleClearCache = async () => {
    if (confirm('確定要清除所有快取嗎?')) {
      await clearCache();
    }
  };

  return (
    <div className='space-y-6'>
      <div className='flex items-center justify-between'>
        <div>
          <h1 className='text-3xl font-bold text-gray-900'>系統統計</h1>
          <p className='mt-2 text-gray-600'>檢視系統使用情況和效能資訊</p>
        </div>
        <div className='flex space-x-2'>
          <Button onClick={refreshStats} disabled={isLoading} variant='outline'>
            重新整理
          </Button>
          <Button onClick={handleClearCache} disabled={isClearing} variant='danger'>
            清除快取
          </Button>
        </div>
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

      {stats && (
        <>
          <div className='grid gap-6 md:grid-cols-2 lg:grid-cols-4'>
            <Card>
              <CardHeader>
                <CardTitle className='text-lg'>總使用者數</CardTitle>
              </CardHeader>
              <CardContent>
                <p className='text-3xl font-bold text-primary-600'>{stats.totalUsers}</p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className='text-lg'>活躍 Token</CardTitle>
              </CardHeader>
              <CardContent>
                <p className='text-3xl font-bold text-green-600'>{stats.activeTokens}</p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className='text-lg'>快取項目</CardTitle>
              </CardHeader>
              <CardContent>
                <p className='text-3xl font-bold text-blue-600'>{stats.cacheSize}</p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className='text-lg'>記憶體使用</CardTitle>
              </CardHeader>
              <CardContent>
                <p className='text-3xl font-bold text-orange-600'>{stats.memoryUsage} MB</p>
              </CardContent>
            </Card>
          </div>

          {stats.additionalInfo && Object.keys(stats.additionalInfo).length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>詳細資訊</CardTitle>
                <CardDescription>系統的額外統計資訊</CardDescription>
              </CardHeader>
              <CardContent>
                <dl className='grid gap-4 sm:grid-cols-2'>
                  {Object.entries(stats.additionalInfo).map(([key, value]) => (
                    <div key={key}>
                      <dt className='text-sm font-medium text-gray-500'>{key}</dt>
                      <dd className='mt-1 text-base text-gray-900'>{String(value)}</dd>
                    </div>
                  ))}
                </dl>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
};
