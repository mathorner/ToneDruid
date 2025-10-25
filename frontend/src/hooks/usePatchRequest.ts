import { useCallback, useState } from 'react';
import type { PatchSuggestion } from '../types/patchRequest';
import { trackClientWarning, trackEvent } from '../services/appInsights';

const buildEndpoint = () => {
  const baseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? '';
  return `${baseUrl}/api/v1/patch-request`;
};

const isPatchSuggestion = (value: unknown): value is PatchSuggestion => {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const candidate = value as Partial<PatchSuggestion>;
  return (
    typeof candidate.prompt === 'string' &&
    typeof candidate.summary === 'string' &&
    Array.isArray(candidate.controls) &&
    candidate.controls.length >= 0 &&
    typeof candidate.requestId === 'string' &&
    typeof candidate.clientRequestId === 'string' &&
    typeof candidate.generatedAtUtc === 'string' &&
    typeof candidate.model === 'string'
  );
};

export function usePatchRequest(prompt: string) {
  const [data, setData] = useState<PatchSuggestion | undefined>();
  const [error, setError] = useState<string | undefined>();
  const [isLoading, setIsLoading] = useState(false);

  const sendRequest = useCallback(async () => {
    const trimmedPrompt = prompt.trim();
    if (!trimmedPrompt) {
      setError('Please enter a prompt before sending.');
      setData(undefined);
      trackClientWarning('prompt_validation_failed', { reason: 'empty' });
      return;
    }

    if (trimmedPrompt.length > 500) {
      setError('Prompt cannot exceed 500 characters.');
      setData(undefined);
      trackClientWarning('prompt_validation_failed', { reason: 'length_exceeded' });
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
          'x-client-request-id': clientRequestId,
          'x-iteration': '002'
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

      const result = (isJson ? await response.json() : undefined) as unknown;

      if (isPatchSuggestion(result)) {
        trackEvent(
          'patch_request_succeeded',
          {
            clientRequestId,
            requestId: result.requestId,
            model: result.model,
            controlCount: result.controls.length.toString(),
            hasReasoning: (result.reasoning?.soundDesignNotes?.length ?? 0 > 0).toString()
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
        setError('Unable to interpret patch suggestion. Please try again.');
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
