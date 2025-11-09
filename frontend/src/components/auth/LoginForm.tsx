import { useState } from 'react';
import { Input } from '../ui/Input';
import { Button } from '../ui/Button';
import { Alert, AlertDescription } from '../ui/Alert';

interface LoginFormProps {
  onSubmit: (username: string, password: string) => Promise<void>;
  error?: string | null;
  isLoading?: boolean;
}

export const LoginForm: React.FC<LoginFormProps> = ({ onSubmit, error, isLoading }) => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit(username, password);
  };

  return (
    <form onSubmit={handleSubmit} className='space-y-6'>
      {error && (
        <Alert variant='error'>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <Input
        label='使用者名稱'
        type='text'
        value={username}
        onChange={(e) => setUsername(e.target.value)}
        placeholder='請輸入使用者名稱'
        required
        disabled={isLoading}
      />

      <Input
        label='密碼'
        type='password'
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        placeholder='請輸入密碼'
        required
        disabled={isLoading}
      />

      <Button type='submit' className='w-full' isLoading={isLoading}>
        登入
      </Button>
    </form>
  );
};
