import React from 'react';
import { cn } from '../../utils';

export interface AlertProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info';
}

export const Alert = React.forwardRef<HTMLDivElement, AlertProps>(
  ({ className, variant = 'default', ...props }, ref) => {
    const variants = {
      default: 'bg-gray-50 text-gray-900 border-gray-200',
      success: 'bg-green-50 text-green-900 border-green-200',
      warning: 'bg-yellow-50 text-yellow-900 border-yellow-200',
      error: 'bg-red-50 text-red-900 border-red-200',
      info: 'bg-blue-50 text-blue-900 border-blue-200',
    };

    return (
      <div
        ref={ref}
        role='alert'
        className={cn('relative w-full rounded-lg border p-4', variants[variant], className)}
        {...props}
      />
    );
  },
);

Alert.displayName = 'Alert';

export const AlertTitle = React.forwardRef<HTMLHeadingElement, React.HTMLAttributes<HTMLHeadingElement>>(
  ({ className, ...props }, ref) => {
    return <h5 ref={ref} className={cn('mb-1 font-medium leading-none tracking-tight', className)} {...props} />;
  },
);

AlertTitle.displayName = 'AlertTitle';

export const AlertDescription = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => {
    return <div ref={ref} className={cn('text-sm [&_p]:leading-relaxed', className)} {...props} />;
  },
);

AlertDescription.displayName = 'AlertDescription';
