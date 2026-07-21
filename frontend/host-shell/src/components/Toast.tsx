import { useCallback, useEffect, useRef, useState, createContext, useContext, type ReactNode } from 'react';
import { Portal } from './Portal';

type ToastType = 'success' | 'error' | 'info';

interface ToastItem {
  id: string;
  message: string;
  type: ToastType;
}

interface ToastContextValue {
  showToast: (message: string, type?: ToastType) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within ToastProvider');
  return ctx;
}

interface ToastProviderProps {
  children: ReactNode;
}

export function ToastProvider({ children }: ToastProviderProps) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const timerRefs = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
    const timer = timerRefs.current.get(id);
    if (timer) {
      clearTimeout(timer);
      timerRefs.current.delete(id);
    }
  }, []);

  const showToast = useCallback((message: string, type: ToastType = 'success') => {
    const id = `toast-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    setToasts((prev) => [...prev, { id, message, type }]);

    const timer = setTimeout(() => removeToast(id), 5000);
    timerRefs.current.set(id, timer);
  }, [removeToast]);

  // Cleanup timers on unmount
  useEffect(() => {
    return () => {
      timerRefs.current.forEach((timer) => clearTimeout(timer));
    };
  }, []);

  const value = { showToast };

  return (
    <ToastContext value={value}>
      {children}
      <Portal containerId="toast-root">
        <div
          className="fixed bottom-6 right-6 z-[1100] flex flex-col gap-3 pointer-events-none"
          aria-live="polite"
          aria-label="Notifications"
        >
          {toasts.map((toast) => (
            <ToastMessage key={toast.id} toast={toast} onDismiss={removeToast} />
          ))}
        </div>
      </Portal>
    </ToastContext>
  );
}

function ToastMessage({ toast, onDismiss }: { toast: ToastItem; onDismiss: (id: string) => void }) {
  const typeStyles: Record<ToastType, string> = {
    success: 'border-l-4 border-mint bg-mint/10 text-mint',
    error: 'border-l-4 border-rose bg-rose/10 text-rose',
    info: 'border-l-4 border-violet bg-violet/10 text-violet-light',
  };

  const icons: Record<ToastType, string> = {
    success: '✓',
    error: '✕',
    info: 'ℹ',
  };

  return (
    <div
      role="status"
      className={`pointer-events-auto glass-card px-4 py-3 flex items-center gap-3 min-w-[280px] max-w-[400px] animate-[slideUp_200ms_ease-out] ${typeStyles[toast.type]}`}
    >
      <span className="text-lg font-bold" aria-hidden="true">{icons[toast.type]}</span>
      <p className="flex-1 text-sm text-text-primary">{toast.message}</p>
      <button
        onClick={() => onDismiss(toast.id)}
        className="text-text-muted hover:text-text-primary transition-colors p-1"
        aria-label="Dismiss notification"
      >
        ✕
      </button>
    </div>
  );
}
