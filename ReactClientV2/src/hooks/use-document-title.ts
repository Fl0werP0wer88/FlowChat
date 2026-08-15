import { useEffect } from 'react';

export function useDocumentTitle(title: string) {
  useEffect(() => {
    document.title = `${title} · FlowChat`;
    return () => {
      document.title = 'FlowChat';
    };
  }, [title]);
}
