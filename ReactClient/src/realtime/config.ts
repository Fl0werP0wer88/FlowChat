const realtimeBaseUrl = (import.meta.env.VITE_REALTIME_API_URL ?? "https://localhost:7215").replace(/\/+$/, "");

export const chatHubUrl = `${realtimeBaseUrl}/hubs/chat`;
