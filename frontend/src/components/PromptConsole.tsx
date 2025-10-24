import { FormEvent, useMemo, useState } from 'react';
import { usePatchRequest } from '../hooks/usePatchRequest';

const PROMPT_MAX_LENGTH = 500;

const formatDate = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
};

const PromptConsole = () => {
  const [prompt, setPrompt] = useState('');
  const { data, error, isLoading, sendRequest } = usePatchRequest(prompt);

  const isSubmitDisabled = useMemo(() => {
    return isLoading || prompt.trim().length === 0;
  }, [isLoading, prompt]);

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    void sendRequest();
  };

  return (
    <section>
      <form onSubmit={handleSubmit}>
        <label htmlFor="prompt">Describe your desired tone</label>
        <div className="textarea-wrapper">
          <textarea
            id="prompt"
            name="prompt"
            placeholder="e.g. A glassy pad with shimmering motion"
            value={prompt}
            onChange={(event) => setPrompt(event.target.value)}
            disabled={isLoading}
            maxLength={PROMPT_MAX_LENGTH}
            aria-describedby="prompt-character-count"
          />
          <div className="textarea-footer">
            <span id="prompt-character-count" className="char-count">
              {prompt.length}/{PROMPT_MAX_LENGTH}
            </span>
          </div>
        </div>
        <button type="submit" disabled={isSubmitDisabled}>
          {isLoading ? 'Sending…' : 'Send'}
        </button>
      </form>

      {isLoading && (
        <div className="loading">
          <span className="spinner" aria-hidden="true" />
          <span>Contacting Tone Druid…</span>
        </div>
      )}

      {!isLoading && !data && !error && (
        <div className="placeholder-card">
          <strong>Ready when you are.</strong>
          <p>Describe a sound and Tone Druid will respond using Azure OpenAI.</p>
        </div>
      )}

      {error && (
        <div className="error-card" role="alert">
          <strong>Something went wrong.</strong>
          <p>{error}</p>
        </div>
      )}

      {data && (
        <article className="response-card">
          <h2>Response</h2>
          <p>{data.response}</p>
          <div className="meta">
            <span>
              <strong>Prompt:</strong> {data.prompt}
            </span>
            <span>
              <strong>Request Id:</strong> {data.requestId}
            </span>
            <span>
              <strong>Client Request Id:</strong> {data.clientRequestId}
            </span>
            <span>
              <strong>Generated:</strong> {formatDate(data.generatedAtUtc)}
            </span>
          </div>
        </article>
      )}
    </section>
  );
};

export default PromptConsole;
