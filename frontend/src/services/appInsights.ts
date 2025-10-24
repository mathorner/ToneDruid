import { ApplicationInsights } from '@microsoft/applicationinsights-web';

let appInsights: ApplicationInsights | undefined;

const looksLikeConnectionString = (value: string) => {
  return value.includes('InstrumentationKey=') || value.startsWith('Authorization=');
};

export const initializeAppInsights = (connectionString?: string) => {
  if (appInsights || !connectionString || !looksLikeConnectionString(connectionString)) {
    if (connectionString && !looksLikeConnectionString(connectionString)) {
      console.warn('Skipping Application Insights initialisation: invalid connection string format detected.');
    }
    return;
  }

  try {
    appInsights = new ApplicationInsights({
      config: {
        connectionString,
        enableAutoRouteTracking: true
      }
    });

    appInsights.loadAppInsights();
  } catch (error) {
    console.error('Unable to initialise Application Insights.', error);
    appInsights = undefined;
  }
};

export const trackEvent = (
  name: string,
  properties?: Record<string, string>,
  measurements?: Record<string, number>
) => {
  if (!appInsights) {
    return;
  }

  appInsights.trackEvent({
    name,
    properties,
    measurements
  });
};
