import { useState, useEffect, useCallback } from 'react';

interface UseApiOptions<T> {
  initialData?: T;
  onSuccess?: (data: T) => void;
  onError?: (error: any) => void;
}

interface UseApiReturn<T> {
  data: T | null;
  isLoading: boolean;
  error: string | null;
  execute: (...args: any[]) => Promise<void>;
  reset: () => void;
}

/**
 * API 呼叫 Hook - 處理載入狀態和錯誤
 */
export const useApi = <T = any>(
  apiFunction: (...args: any[]) => Promise<T>,
  options: UseApiOptions<T> = {},
): UseApiReturn<T> => {
  const { initialData = null, onSuccess, onError } = options;

  const [data, setData] = useState<T | null>(initialData);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const execute = useCallback(
    async (...args: any[]) => {
      setIsLoading(true);
      setError(null);

      try {
        const result = await apiFunction(...args);
        setData(result);
        onSuccess?.(result);
      } catch (err: any) {
        const errorMessage = err.response?.data?.message || '發生錯誤,請稍後再試';
        setError(errorMessage);
        onError?.(err);
      } finally {
        setIsLoading(false);
      }
    },
    [apiFunction, onSuccess, onError],
  );

  const reset = useCallback(() => {
    setData(initialData);
    setError(null);
    setIsLoading(false);
  }, [initialData]);

  return { data, isLoading, error, execute, reset };
};

/**
 * 自動執行的 API Hook - 在元件載入時自動呼叫 API
 */
export const useApiAuto = <T = any>(
  apiFunction: () => Promise<T>,
  dependencies: any[] = [],
  options: UseApiOptions<T> = {},
): UseApiReturn<T> => {
  const result = useApi(apiFunction, options);

  useEffect(() => {
    result.execute();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, dependencies);

  return result;
};
