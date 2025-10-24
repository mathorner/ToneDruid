import { ApplicationInsights } from '@microsoft/applicationinsights-web';

let appInsights: ApplicationInsights | undefined;

export const initializeAppInsights = (connectionString?: string) => {
  if (appInsights || !connectionString) {
    return;
  }

  appInsights = new ApplicationInsights({
    config: {
      connectionString,
      enableAutoRouteTracking: true
    }
  });

  appInsights.loadAppInsights();
};

export const trackEvent = (
  name: string,
  properties?: Record<string, string>,
  measurements?: Record<string, number>
) => {
  if (!appInsights) {
    return;
  }

  appInsights.trackEvent({ name }, properties, measurements);
};
