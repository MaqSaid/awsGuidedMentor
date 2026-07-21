import { useCallback, useEffect, useRef, useState } from 'react';
import { Portal } from './Portal';
import { apiFetch } from '../lib/api';

interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: number;
}

const MAX_MESSAGES_PER_MINUTE = 20;
const RATE_LIMIT_WINDOW_MS = 60_000;

/**
 * AiHelpChat — Floating AI help assistant chat bubble.
 * Appears in bottom-right corner on all authenticated pages.
 * Rate limits to 20 messages/minute displayed in UI.
 * Requirements: 14.1-14.7, 25.4
 */
export function AiHelpChat() {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [messageTimestamps, setMessageTimestamps] = useState<number[]>([]);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);

  // Scroll to bottom when messages update
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Focus input when panel opens
  useEffect(() => {
    if (isOpen) {
      setTimeout(() => inputRef.current?.focus(), 100);
    }
  }, [isOpen]);

  // Handle Escape key to close panel
  useEffect(() => {
    if (!isOpen) return;

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        setIsOpen(false);
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen]);

  // Rate limit check
  const isRateLimited = useCallback((): boolean => {
    const now = Date.now();
    const recentMessages = messageTimestamps.filter((ts) => now - ts < RATE_LIMIT_WINDOW_MS);
    return recentMessages.length >= MAX_MESSAGES_PER_MINUTE;
  }, [messageTimestamps]);

  async function handleSend() {
    const trimmed = input.trim();
    if (!trimmed || isLoading) return;

    if (isRateLimited()) {
      setError('You\'ve reached the message limit. Please wait a moment before sending more.');
      return;
    }

    setError(null);
    const userMessage: ChatMessage = {
      id: `msg-${Date.now()}`,
      role: 'user',
      content: trimmed,
      timestamp: Date.now(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setMessageTimestamps((prev) => [...prev, Date.now()]);
    setInput('');
    setIsLoading(true);

    try {
      const response = await apiFetch('/v1/assistant/chat', {
        method: 'POST',
        body: JSON.stringify({
          message: trimmed,
          conversationHistory: messages.map((m) => ({ role: m.role, content: m.content })),
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to get response');
      }

      const data = (await response.json()) as { reply: string };
      const assistantMessage: ChatMessage = {
        id: `msg-${Date.now()}-assistant`,
        role: 'assistant',
        content: data.reply,
        timestamp: Date.now(),
      };

      setMessages((prev) => [...prev, assistantMessage]);
    } catch {
      setError('Something went wrong. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }

  function handleRetry() {
    setError(null);
    // Resend the last user message
    const lastUserMsg = [...messages].reverse().find((m) => m.role === 'user');
    if (lastUserMsg) {
      setInput(lastUserMsg.content);
      setMessages((prev) => prev.filter((m) => m.id !== lastUserMsg.id));
    }
  }

  return (
    <Portal containerId="ai-help-root">
      {/* Floating button */}
      {!isOpen && (
        <button
          onClick={() => setIsOpen(true)}
          className="fixed bottom-6 right-6 z-[900] w-14 h-14 rounded-full bg-violet text-white shadow-lg hover:scale-105 transition-transform flex items-center justify-center glow-violet"
          aria-label="Open AI Help Assistant"
          data-tour="help"
        >
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path
              d="M12 2C6.48 2 2 6.03 2 11c0 2.76 1.36 5.22 3.5 6.84V22l3.84-2.1c.84.16 1.72.1 2.66.1 5.52 0 10-4.03 10-9S17.52 2 12 2Z"
              fill="currentColor"
            />
            <circle cx="8" cy="11" r="1.5" fill="#0a0a1a" />
            <circle cx="12" cy="11" r="1.5" fill="#0a0a1a" />
            <circle cx="16" cy="11" r="1.5" fill="#0a0a1a" />
          </svg>
        </button>
      )}

      {/* Chat panel */}
      {isOpen && (
        <div
          ref={panelRef}
          role="dialog"
          aria-modal="false"
          aria-label="AI Help Assistant"
          className="fixed bottom-6 right-6 z-[900] w-[380px] max-w-[calc(100vw-3rem)] h-[500px] max-h-[calc(100vh-6rem)] flex flex-col glass-card overflow-hidden glow-violet"
        >
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 border-b border-border">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-full bg-violet/20 flex items-center justify-center">
                <span className="text-violet-light text-sm" aria-hidden="true">🤖</span>
              </div>
              <div>
                <h2 className="text-sm font-semibold text-text-primary" style={{ fontFamily: 'Outfit, sans-serif' }}>
                  AI Help
                </h2>
                <span className="text-xs text-text-muted">Ask anything about GuidedMentor</span>
              </div>
            </div>
            <button
              onClick={() => setIsOpen(false)}
              className="p-2 rounded-lg hover:bg-white/5 transition-colors text-text-muted hover:text-text-primary"
              aria-label="Close AI Help"
            >
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                <path d="M12 4L4 12M4 4l8 8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
              </svg>
            </button>
          </div>

          {/* Messages area */}
          <div className="flex-1 overflow-y-auto px-4 py-3 space-y-3" aria-live="polite">
            {messages.length === 0 && (
              <div className="flex flex-col items-center justify-center h-full text-center py-8">
                <span className="text-3xl mb-3" aria-hidden="true">💬</span>
                <p className="text-sm text-text-secondary">
                  Hi! I can help with navigation, features, or how to use GuidedMentor.
                </p>
              </div>
            )}
            {messages.map((msg) => (
              <div
                key={msg.id}
                className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
              >
                <div
                  className={`max-w-[80%] rounded-xl px-3 py-2 text-sm ${
                    msg.role === 'user'
                      ? 'bg-violet text-white rounded-br-sm'
                      : 'bg-white/5 text-text-primary border border-border rounded-bl-sm'
                  }`}
                >
                  {msg.content}
                </div>
              </div>
            ))}
            {isLoading && (
              <div className="flex justify-start">
                <div className="bg-white/5 border border-border rounded-xl px-3 py-2 rounded-bl-sm">
                  <div className="flex gap-1">
                    <span className="w-2 h-2 bg-violet-light rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                    <span className="w-2 h-2 bg-violet-light rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                    <span className="w-2 h-2 bg-violet-light rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
                  </div>
                </div>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Error display */}
          {error && (
            <div className="px-4 py-2 bg-rose/10 border-t border-rose/20" role="alert">
              <div className="flex items-center justify-between">
                <p className="text-xs text-rose">{error}</p>
                <button
                  onClick={handleRetry}
                  className="text-xs text-violet-light hover:text-violet transition-colors ml-2"
                  aria-label="Retry sending message"
                >
                  Retry
                </button>
              </div>
            </div>
          )}

          {/* Input area */}
          <div className="px-4 py-3 border-t border-border">
            <div className="flex items-center gap-2">
              <input
                ref={inputRef}
                type="text"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') handleSend(); }}
                placeholder="Ask a question..."
                disabled={isLoading}
                className="flex-1 px-3 py-2 rounded-xl bg-white/5 border border-border text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:border-violet transition-colors disabled:opacity-50"
                aria-label="Type your question"
              />
              <button
                onClick={handleSend}
                disabled={isLoading || !input.trim()}
                className="p-2 rounded-xl bg-violet text-white disabled:opacity-50 disabled:cursor-not-allowed hover:bg-violet/80 transition-colors"
                aria-label="Send message"
              >
                <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                  <path d="M14 2L7 9M14 2l-5 12-2-5-5-2 12-5Z" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      )}
    </Portal>
  );
}
