import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './styles.css';
import { initializeAppInsights } from './services/appInsights';

initializeAppInsights(import.meta.env.VITE_APPINSIGHTS_CONNECTION_STRING);

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
