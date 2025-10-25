export type ConfidenceLevel = 'low' | 'medium' | 'high';

export type PatchControl = {
  id: string;
  label: string;
  group: string;
  value: number | string;
  valueType: 'continuous' | 'enumeration' | 'boolean';
  range?: {
    min: number;
    max: number;
    unit?: string;
  } | null;
  allowedValues?: string[];
  explanation: string;
  confidence: ConfidenceLevel;
};

export type PatchSuggestion = {
  prompt: string;
  summary: string;
  controls: PatchControl[];
  reasoning: {
    intentSummary: string;
    soundDesignNotes: string[];
    assumptions: string[];
  };
  requestId: string;
  clientRequestId: string;
  generatedAtUtc: string;
  model: string;
};
