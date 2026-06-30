/**
 * AIHelpAssistant — Floating bubble (bottom-right), useChat() streaming,
 * Ctrl+H shortcut, dismissible, session-scoped history, ARIA labels.
 *
 * Requirements: 14.1, 14.2, 14.4, 14.5, 14.7, 14.8
 */
import { useState, useEffect, useRef, useCallback } from 'react';
import type { ChatMessage } from '../types';

const MAX_MESSAGE_LENGTH = 1000;
const CHAT_API_ENDPOINT = '/v1/assistant/chat';

export function AIHelpAssistant() {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const chatContainerRef = useRef<HTMLDivElement>(null);

  // Keyboard shortcut: Ctrl+H to toggle
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey && e.key === 'h') {
        e.preventDefault();
        setIsOpen((prev) => !prev);
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, []);

  // Focus input when opened
  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus();
    }
  }, [isOpen]);

  // Scroll to bottom on new messages
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const sendMessage = useCallback(async () => {
    const trimmed = input.trim();
    if (!trimmed || isStreaming) return;

    // Sanitize: enforce length limit
    const sanitized = trimmed.slice(0, MAX_MESSAGE_LENGTH);

    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: sanitized,
      createdAt: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setError(null);
    setIsStreaming(true);

    const assistantMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: '',
      createdAt: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, assistantMessage]);

    try {
      const response = await fetch(CHAT_API_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          message: sanitized,
          history: messages.map((m) => ({ role: m.role, content: m.content })),
        }),
      });

      if (!response.ok) {
        throw new Error(`API Error: ${response.status}`);
      }

      if (!response.body) {
        throw new Error('No response body');
      }

      // Stream response
      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let fullContent = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value, { stream: true });
        fullContent += chunk;

        setMessages((prev) => {
          const updated = [...prev];
          const lastMsg = updated[updated.length - 1];
          if (lastMsg && lastMsg.role === 'assistant') {
            updated[updated.length - 1] = { ...lastMsg, content: fullContent };
          }
          return updated;
        });
      }
    } catch (err) {
      setError('Something went wrong. Please try again.');
      // Remove the empty assistant message on error
      setMessages((prev) => {
        const updated = [...prev];
        const last = updated[updated.length - 1];
        if (last && last.role === 'assistant' && last.content === '') {
          updated.pop();
        }
        return updated;
      });
    } finally {
      setIsStreaming(false);
    }
  }, [input, isStreaming, messages]);

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  const handleRetry = () => {
    setError(null);
    // Resend the last user message
    const lastUserMsg = [...messages].reverse().find((m) => m.role === 'user');
    if (lastUserMsg) {
      setInput(lastUserMsg.content);
    }
  };

  if (!isOpen) {
    return (
      <button
        data-testid="engagement-ai-help-assistant"
        onClick={() => setIsOpen(true)}
        aria-label="Open AI Help Assistant (Ctrl+H)"
        className="fixed bottom-6 right-6 z-50 w-14 h-14 rounded-full bg-primary text-[#0a1628] shadow-lg hover:shadow-glow-orange transition-shadow flex items-center justify-center"
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" />
        </svg>
      </button>
    );
  }

  return (
    <div
      data-testid="engagement-ai-help-assistant"
      ref={chatContainerRef}
      role="dialog"
      aria-label="AI Help Assistant"
      aria-describedby="ai-help-description"
      className="fixed bottom-6 right-6 z-50 w-96 h-[32rem] flex flex-col glass-card rounded-lg shadow-2xl overflow-hidden"
    >
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-[rgba(255,255,255,0.1)]">
        <div className="flex items-center gap-2">
          <span className="text-primary font-semibold text-sm" aria-hidden="true">✨</span>
          <h2 className="text-sm font-semibold text-text-primary">AI Help Assistant</h2>
        </div>
        <button
          onClick={() => setIsOpen(false)}
          aria-label="Close AI Help Assistant"
          className="p-1 rounded-sm text-text-muted hover:text-text-primary transition-colors focus-visible:ring-2 focus-visible:ring-primary outline-none"
        >
          <svg className="h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
          </svg>
        </button>
      </div>

      <p id="ai-help-description" className="sr-only">
        Ask questions about GuidedMentor platform features, navigation, and usage.
      </p>

      {/* Messages */}
      <div
        className="flex-1 overflow-y-auto p-4 space-y-3"
        role="log"
        aria-live="polite"
        aria-label="Chat messages"
      >
        {messages.length === 0 && (
          <div className="text-center text-text-muted text-sm py-8">
            <p>Hi! I'm your AI assistant. 👋</p>
            <p className="mt-1">Ask me about platform features, navigation, or how to get started.</p>
          </div>
        )}
        {messages.map((msg) => (
          <MessageBubble key={msg.id} message={msg} />
        ))}
        {isStreaming && (
          <div className="flex items-center gap-2 text-text-muted text-xs" aria-live="polite">
            <span className="animate-pulse">●</span> Thinking...
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Error */}
      {error && (
        <div className="px-4 py-2 bg-error/10 border-t border-error/20" role="alert">
          <div className="flex items-center justify-between">
            <p className="text-xs text-error">{error}</p>
            <button
              onClick={handleRetry}
              className="text-xs text-primary hover:text-primary/80 underline"
            >
              Retry
            </button>
          </div>
        </div>
      )}

      {/* Input */}
      <div className="p-3 border-t border-[rgba(255,255,255,0.1)]">
        <div className="flex gap-2">
          <textarea
            ref={inputRef}
            value={input}
            onChange={(e) => setInput(e.target.value.slice(0, MAX_MESSAGE_LENGTH))}
            onKeyDown={handleKeyDown}
            placeholder="Ask a question..."
            aria-label="Type your message"
            rows={1}
            disabled={isStreaming}
            className="flex-1 resize-none rounded-md bg-[rgba(255,255,255,0.05)] border border-[rgba(255,255,255,0.1)] px-3 py-2 text-sm text-text-primary placeholder:text-text-muted outline-none focus:ring-1 focus:ring-primary disabled:opacity-50"
          />
          <button
            onClick={sendMessage}
            disabled={!input.trim() || isStreaming}
            aria-label="Send message"
            className="px-3 py-2 bg-primary text-[#0a1628] rounded-md font-medium text-sm hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed transition-opacity"
          >
            Send
          </button>
        </div>
        <p className="text-xs text-text-muted mt-1 text-right">
          {input.length}/{MAX_MESSAGE_LENGTH}
        </p>
      </div>
    </div>
  );
}

function MessageBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === 'user';

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`
          max-w-[85%] px-3 py-2 rounded-lg text-sm
          ${isUser
            ? 'bg-primary text-[#0a1628]'
            : 'bg-[rgba(255,255,255,0.06)] text-text-primary'
          }
        `}
        role="article"
        aria-label={`${isUser ? 'You' : 'AI Assistant'}: ${message.content}`}
      >
        <p className="whitespace-pre-wrap break-words">{message.content}</p>
      </div>
    </div>
  );
}

export default AIHelpAssistant;
