import api from './client';
import type { ChatbotResponse } from '@/types/api';

export const chatbotApi = {
  ask: (question: string, context?: string) =>
    api.post<ChatbotResponse>('/chatbot/ask', { question, context }).then((r) => r.data),
};
