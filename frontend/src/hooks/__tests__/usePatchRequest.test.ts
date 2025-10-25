import { act, renderHook } from '@testing-library/react';
import { vi } from 'vitest';
import { usePatchRequest } from '../usePatchRequest';
import { trackClientWarning, trackEvent } from '../../services/appInsights';

vi.mock('../../services/appInsights', () => ({
  trackEvent: vi.fn(),
  trackClientWarning: vi.fn()
}));

describe('usePatchRequest', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  it('rejects empty prompts with validation error', async () => {
    const { result } = renderHook(() => usePatchRequest('   '));

    await act(async () => {
      await result.current.sendRequest();
    });

    expect(result.current.error).toBe('Please enter a prompt before sending.');
    expect(result.current.data).toBeUndefined();
    expect(result.current.isLoading).toBe(false);
    expect(trackClientWarning).toHaveBeenCalledWith('prompt_validation_failed', { reason: 'empty' });
    expect(trackEvent).not.toHaveBeenCalled();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('rejects prompts that exceed the maximum length', async () => {
    const longPrompt = 'a'.repeat(501);
    const { result } = renderHook(() => usePatchRequest(longPrompt));

    await act(async () => {
      await result.current.sendRequest();
    });

    expect(result.current.error).toBe('Prompt cannot exceed 500 characters.');
    expect(result.current.data).toBeUndefined();
    expect(result.current.isLoading).toBe(false);
    expect(trackClientWarning).toHaveBeenCalledWith('prompt_validation_failed', { reason: 'length_exceeded' });
    expect(trackEvent).not.toHaveBeenCalled();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('handles successful responses and emits telemetry', async () => {
    const suggestion = {
      prompt: 'Create a warm pad',
      summary: 'A lush, evolving pad sound.',
      controls: [
        {
          id: 'osc1Shape',
          label: 'Oscillator 1 Shape',
          group: 'Oscillator',
          value: 0.5,
          valueType: 'continuous',
          explanation: 'Blend between triangle and saw for warmth.',
          confidence: 'high' as const
        }
      ],
      reasoning: {
        intentSummary: 'Goal is a warm pad.',
        soundDesignNotes: ['Use smooth oscillator shapes.'],
        assumptions: []
      },
      requestId: 'req-123',
      clientRequestId: 'client-abc',
      generatedAtUtc: '2024-01-01T00:00:00Z',
      model: 'gpt-4'
    };

    fetchMock.mockResolvedValue(
      new Response(JSON.stringify(suggestion), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    );

    const { result } = renderHook(() => usePatchRequest('Create a warm pad'));

    await act(async () => {
      await result.current.sendRequest();
    });

    expect(result.current.error).toBeUndefined();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.data).toEqual(suggestion);
    expect(trackEvent).toHaveBeenNthCalledWith(1, 'patch_request_started', {
      clientRequestId: expect.any(String)
    });
    expect(trackEvent).toHaveBeenNthCalledWith(
      2,
      'patch_request_succeeded',
      expect.objectContaining({
        clientRequestId: expect.any(String),
        requestId: 'req-123',
        model: 'gpt-4',
        controlCount: suggestion.controls.length.toString(),
        hasReasoning: '1'
      }),
      expect.objectContaining({ durationMs: expect.any(Number) })
    );
  });

  it('handles invalid JSON payloads by surfacing an error and telemetry', async () => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ message: 'not a patch suggestion' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    );

    const { result } = renderHook(() => usePatchRequest('Deep bass drone'));

    await act(async () => {
      await result.current.sendRequest();
    });

    expect(result.current.error).toBe('Unable to interpret patch suggestion. Please try again.');
    expect(result.current.data).toBeUndefined();
    expect(result.current.isLoading).toBe(false);
    expect(trackEvent).toHaveBeenNthCalledWith(1, 'patch_request_started', {
      clientRequestId: expect.any(String)
    });
    expect(trackEvent).toHaveBeenNthCalledWith(
      2,
      'patch_request_failed',
      expect.objectContaining({
        clientRequestId: expect.any(String),
        reason: 'invalid_response'
      }),
      expect.objectContaining({ durationMs: expect.any(Number) })
    );
  });
});
