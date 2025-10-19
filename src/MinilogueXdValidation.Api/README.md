# Minilogue XD Patch Validation API

## Endpoint
- `POST /api/v1/minilogue-xd/patches/validate`

## Request Payload
- Content-Type: `application/json`
- Body: JSON object with eight sections mirroring the schema groups (`master`, `oscillators`, `mixer`, `filter`, `envelopes`, `lfo`, `effects`, `program_edit`).
- Each section is a JSON object whose property names match the `id` fields defined in `schemas/minilogue-xd/voice-parameters.json`. Provide values that conform to the type/range/enum constraints captured in the schema.

```json
{
  "master": {
    "master.tempo": 120.0,
    "voice_mode.type": "poly"
  },
  "oscillators": {
    "vco1.wave": "saw",
    "vco2.wave": "triangle"
  },
  "mixer": {
    "mixer.vco1_level": 768
  },
  "filter": {
    "filter.cutoff": 512
  },
  "envelopes": {
    "amp_eg.attack": 256
  },
  "lfo": {
    "lfo.wave": "triangle"
  },
  "effects": {
    "effects.type": "reverb"
  },
  "program_edit": {
    "cv.mode": "modulation",
    "joystick.assignable_targets": ["cutoff"]
  }
}
```

## Responses

### 200 OK — Valid Patch
```json
{
  "valid": true
}
```

### 400 Bad Request — Validation Errors
```json
{
  "valid": false,
  "errors": [
    {
      "field": "vco1.wave",
      "value": "sine",
      "message": "Parameter 'vco1.wave' must be one of: saw, triangle, square."
    }
  ]
}
```

## Schema Source & Versioning
- The validator loads `schemas/minilogue-xd/voice-parameters.json` at startup. The schema path defaults to the repository artifact but can be overridden via the `MinilogueXdSchema:SchemaPath` configuration key.
- Schema metadata includes the instrument name, version, and manual references for auditing and future upgrades.

## Error Semantics
- `field`: Fully-qualified parameter identifier or section name.
- `value`: The raw value received in the request (if available).
- `message`: Human-readable guidance sourced from the schema constraints (allowed enum values, numeric ranges, or structural instructions).

## Integration Notes
- All validation occurs synchronously and does not mutate the payload.
- Alias parameters (e.g., `cv.assignable_targets`) resolve against their canonical definitions before validation, enabling shared allowlists for joystick/CV assignment targets.
- Additional sections or parameters not defined in the schema trigger explicit errors to prevent silent drift.
