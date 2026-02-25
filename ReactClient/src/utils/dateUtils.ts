export function formatLocalTime(isoDateString: string): string {
  return new Date(isoDateString).toLocaleTimeString();
}
