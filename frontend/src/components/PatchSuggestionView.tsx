import type { PatchControl, PatchSuggestion } from '../types/patchRequest';
import controlMetadata from '../data/control-metadata.json';

type ControlMetadata = {
  id: string;
  label: string;
  group: string;
  valueType: 'continuous' | 'enumeration' | 'boolean';
  range?: {
    min: number;
    max: number;
    unit?: string;
  };
  allowedValues?: string[];
};

const metadataList = controlMetadata as ControlMetadata[];
const metadataById = new Map<string, ControlMetadata>(metadataList.map((item) => [item.id, item]));

const confidenceClassMap: Record<PatchControl['confidence'], string> = {
  low: 'confidence-low',
  medium: 'confidence-medium',
  high: 'confidence-high'
};

const formatDate = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
};

const formatValue = (control: PatchControl) => {
  if (typeof control.value === 'number') {
    return control.value;
  }

  return control.value;
};

const describeRange = (control: PatchControl) => {
  if (!control.range) {
    return undefined;
  }

  const { min, max, unit } = control.range;
  const base = `${min} – ${max}`;
  return unit ? `${base} ${unit}` : base;
};

const describeAllowedValues = (control: PatchControl) => {
  if (!control.allowedValues || control.allowedValues.length === 0) {
    return undefined;
  }

  return control.allowedValues.join(', ');
};

const isValueOutsideRange = (control: PatchControl) => {
  const metadata = metadataById.get(control.id);
  if (!metadata) {
    return false;
  }

  if (metadata.range) {
    const value = typeof control.value === 'number' ? control.value : Number(control.value);
    if (!Number.isNaN(value)) {
      if (value < metadata.range.min || value > metadata.range.max) {
        return true;
      }
    }
  }

  if (metadata.allowedValues && metadata.allowedValues.length > 0) {
    const value = typeof control.value === 'string' ? control.value : String(control.value);
    if (!metadata.allowedValues.includes(value)) {
      return true;
    }
  }

  return false;
};

const renderControlRow = (control: PatchControl) => {
  const metadata = metadataById.get(control.id);
  const rangeDescription = describeRange(control) ??
    (metadata?.range ? `${metadata.range.min} – ${metadata.range.max}${metadata.range.unit ? ` ${metadata.range.unit}` : ''}` : undefined);
  const allowedDescription = describeAllowedValues(control) ?? metadata?.allowedValues?.join(', ');
  const outOfRange = isValueOutsideRange(control);

  return (
    <tr key={control.id}>
      <th scope="row">
        <div className="control-label">{control.label}</div>
        <div className="control-meta">{control.group} · {control.id}</div>
      </th>
      <td>
        <div className={`control-value ${outOfRange ? 'value-out-of-range' : ''}`}>{formatValue(control)}</div>
        {rangeDescription && <div className="control-range">Range: {rangeDescription}</div>}
        {allowedDescription && <div className="control-allowed">Allowed: {allowedDescription}</div>}
      </td>
      <td>
        <span className={`confidence-badge ${confidenceClassMap[control.confidence]}`}>{control.confidence}</span>
      </td>
      <td>{control.explanation}</td>
    </tr>
  );
};

type PatchSuggestionViewProps = {
  suggestion: PatchSuggestion;
};

const PatchSuggestionView = ({ suggestion }: PatchSuggestionViewProps) => {
  return (
    <article className="response-card">
      <header className="response-header">
        <div>
          <h2>Patch Summary</h2>
          <p className="summary-text">{suggestion.summary}</p>
        </div>
        <dl className="response-meta">
          <div>
            <dt>Model</dt>
            <dd>{suggestion.model}</dd>
          </div>
          <div>
            <dt>Request Id</dt>
            <dd>{suggestion.requestId}</dd>
          </div>
          <div>
            <dt>Client Request Id</dt>
            <dd>{suggestion.clientRequestId}</dd>
          </div>
          <div>
            <dt>Generated</dt>
            <dd>{formatDate(suggestion.generatedAtUtc)}</dd>
          </div>
        </dl>
      </header>

      <section>
        <h3>Controls</h3>
        <div className="controls-table-wrapper">
          <table className="controls-table">
            <thead>
              <tr>
                <th scope="col">Control</th>
                <th scope="col">Value</th>
                <th scope="col">Confidence</th>
                <th scope="col">Explanation</th>
              </tr>
            </thead>
            <tbody>{suggestion.controls.map(renderControlRow)}</tbody>
          </table>
        </div>
      </section>

      <section className="reasoning-section">
        <h3>Reasoning</h3>
        <div className="reasoning-columns">
          <div>
            <h4>Intent</h4>
            <p>{suggestion.reasoning.intentSummary}</p>
          </div>
          <div>
            <h4>Sound Design Notes</h4>
            <ul>
              {suggestion.reasoning.soundDesignNotes.map((note, index) => (
                <li key={index}>{note}</li>
              ))}
            </ul>
          </div>
          <div>
            <h4>Assumptions</h4>
            {suggestion.reasoning.assumptions.length > 0 ? (
              <ul>
                {suggestion.reasoning.assumptions.map((assumption, index) => (
                  <li key={index}>{assumption}</li>
                ))}
              </ul>
            ) : (
              <p className="muted">No additional assumptions recorded.</p>
            )}
          </div>
        </div>
      </section>

      <footer className="prompt-footer">
        <strong>Prompt:</strong> {suggestion.prompt}
      </footer>
    </article>
  );
};

export default PatchSuggestionView;
