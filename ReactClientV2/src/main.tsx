import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { AppProvider } from '@/app/provider';
import { AppRouter } from '@/app/router';

import './styles.css';

const rootElement = document.getElementById('root');

if (!rootElement) throw new Error('Application root element was not found.');

createRoot(rootElement).render(
  <StrictMode>
    <AppProvider>
      <AppRouter />
    </AppProvider>
  </StrictMode>,
);
