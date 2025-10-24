import { useCallback, useState } from 'react';
import type { PatchRequestResult } from '../types/patchRequest';
import { trackEvent } from '../services/appInsights';

const buildEndpoint = () => {
  const baseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? '';
  return `${baseUrl}/api/v1/patch-request`;
};

export function usePatchRequest(prompt: string) {
  const [data, setData] = useState<PatchRequestResult | undefined>();
  const [error, setError] = useState<string | undefined>();
  const [isLoading, setIsLoading] = useState(false);

  const sendRequest = useCallback(async () => {
    const trimmedPrompt = prompt.trim();
    if (!trimmedPrompt) {
      setError('Please enter a prompt before sending.');
      return;
    }

    const clientRequestId = crypto.randomUUID();
    const endpoint = buildEndpoint();
    const startedAt = performance.now();

    trackEvent('patch_request_started', { clientRequestId });
    setIsLoading(true);
    setError(undefined);

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'x-client-request-id': clientRequestId
        },
        body: JSON.stringify({ prompt: trimmedPrompt })
      });

      const elapsed = performance.now() - startedAt;
      const contentType = response.headers.get('content-type');
      const isJson = contentType?.includes('application/json');

      if (!response.ok) {
        let message = 'Unable to complete the request.';
        if (isJson) {
          const body = await response.json();
          const errors = body?.errors as Record<string, string[]> | undefined;
          if (errors) {
            message = Object.values(errors)
              .flat()
              .join(' ');
          } else if (typeof body?.detail === 'string') {
            message = body.detail;
          }
        } else {
          message = await response.text();
        }

        trackEvent(
          'patch_request_failed',
          { clientRequestId, status: response.status.toString() },
          { durationMs: elapsed }
        );
        setError(message || 'The request failed with an unknown error.');
        setData(undefined);
        return;
      }

      const result = (isJson ? await response.json() : undefined) as PatchRequestResult | undefined;

      if (result) {
        trackEvent(
          'patch_request_succeeded',
          {
            clientRequestId,
            requestId: result.requestId
          },
          { durationMs: elapsed }
        );
        setData(result);
        setError(undefined);
      } else {
        trackEvent(
          'patch_request_failed',
          { clientRequestId, reason: 'invalid_response' },
          { durationMs: elapsed }
        );
        setError('Received an unexpected response from the server.');
        setData(undefined);
      }
    } catch (err) {
      const elapsed = performance.now() - startedAt;
      trackEvent(
        'patch_request_failed',
        { clientRequestId, reason: 'network_error' },
        { durationMs: elapsed }
      );
      setError(err instanceof Error ? err.message : 'An unknown error occurred.');
      setData(undefined);
    } finally {
      setIsLoading(false);
    }
  }, [prompt]);

  return { data, error, isLoading, sendRequest };
}
