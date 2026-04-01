const realtimeBaseUrl = (
  import.meta.env.VITE_REALTIME_API_URL
    ?? import.meta.env.VITE_GATEWAY_API_URL
    ?? "https://localhost:7270"
).replace(/\/+$/, "");

export const chatHubUrl = `${realtimeBaseUrl}/hubs/chat`;
