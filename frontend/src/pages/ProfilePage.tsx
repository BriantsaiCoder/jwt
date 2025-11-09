import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Alert, AlertDescription } from '../components/ui/Alert';
import { useAuth } from '../hooks/useAuth';
import { useApi } from '../hooks/useApi';
import { userService } from '../services/user.service';

export const ProfilePage = () => {
  const { user } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [bio, setBio] = useState('');

  const {
    execute: updateProfile,
    isLoading,
    error,
  } = useApi(userService.updateProfile, {
    onSuccess: () => {
      setIsEditing(false);
      alert('個人資料更新成功!');
    },
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await updateProfile({ email, displayName, bio });
  };

  return (
    <div className='space-y-6'>
      <div>
        <h1 className='text-3xl font-bold text-gray-900'>個人資料</h1>
        <p className='mt-2 text-gray-600'>管理您的個人資訊</p>
      </div>

      <div className='grid gap-6 lg:grid-cols-3'>
        <Card className='lg:col-span-2'>
          <CardHeader>
            <CardTitle>基本資訊</CardTitle>
            <CardDescription>更新您的個人資料</CardDescription>
          </CardHeader>
          <CardContent>
            {error && (
              <Alert variant='error' className='mb-4'>
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            {!isEditing ? (
              <div className='space-y-4'>
                <dl className='space-y-4'>
                  <div>
                    <dt className='text-sm font-medium text-gray-500'>使用者名稱</dt>
                    <dd className='mt-1 text-base text-gray-900'>{user?.username}</dd>
                  </div>
                  <div>
                    <dt className='text-sm font-medium text-gray-500'>角色</dt>
                    <dd className='mt-1 text-base text-gray-900'>{user?.roles.join(', ')}</dd>
                  </div>
                  {user?.claims && user.claims.length > 0 && (
                    <div>
                      <dt className='text-sm font-medium text-gray-500'>權限</dt>
                      <dd className='mt-1 space-y-1'>
                        {user.claims.map((claim, index) => (
                          <div key={index} className='text-sm text-gray-900'>
                            {claim.type}: {claim.value}
                          </div>
                        ))}
                      </dd>
                    </div>
                  )}
                </dl>
                <Button onClick={() => setIsEditing(true)}>編輯資料</Button>
              </div>
            ) : (
              <form onSubmit={handleSubmit} className='space-y-4'>
                <Input
                  label='Email'
                  type='email'
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder='your.email@example.com'
                />
                <Input
                  label='顯示名稱'
                  type='text'
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  placeholder='您的顯示名稱'
                />
                <div>
                  <label className='mb-2 block text-sm font-medium text-gray-700'>個人簡介</label>
                  <textarea
                    value={bio}
                    onChange={(e) => setBio(e.target.value)}
                    rows={4}
                    className='w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-600'
                    placeholder='簡單介紹一下自己...'
                  />
                </div>
                <div className='flex space-x-2'>
                  <Button type='submit' isLoading={isLoading}>
                    儲存變更
                  </Button>
                  <Button type='button' variant='outline' onClick={() => setIsEditing(false)} disabled={isLoading}>
                    取消
                  </Button>
                </div>
              </form>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>帳號安全</CardTitle>
          </CardHeader>
          <CardContent>
            <div className='space-y-4'>
              <div>
                <p className='text-sm text-gray-600'>定期變更密碼可以提高帳號安全性</p>
              </div>
              <Button variant='outline' className='w-full' disabled>
                變更密碼
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
