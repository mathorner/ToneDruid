# Iteration 002 Implementation Notes

## Structured Patch Suggestions
- `/api/v1/patch-request` now returns a `PatchSuggestion` payload with a curated set of 5–10 controls, reasoning metadata, and correlation identifiers.
- The backend validates Azure OpenAI output using a JSON schema and additional catalog checks before responding.
- Validation failures surface as `502` with the message “Unable to interpret patch suggestion. Please try again.” while telemetry captures the raw payload (truncated) for diagnostics.

## Voice Parameter Catalog
- `voice-parameters.json` moved to `backend/ToneDruid.Api/Resources/voice-parameters.json` and is copied to the build output via the project file.
- `VoiceParameterCatalog` loads the reference data at startup, offers lookup helpers, and produces the trimmed catalog snippet embedded into the LLM system prompt.
- The system prompt template lives at `backend/ToneDruid.Api/Resources/Prompts/PatchGenerationSystemPrompt.txt` for easy iteration.

## Frontend Rendering
- React UI now renders the structured suggestion via `PatchSuggestionView`, including confidence badges, value/range context, and reasoning lists.
- Values outside the documented range or enumeration list are highlighted to flag potential issues for follow-up iterations.
- Telemetry events capture `controlCount`, `model`, and `hasReasoning` dimensions; client-side validation errors are logged as warnings.

## Control Metadata Snapshot
- A reduced `control-metadata.json` snapshot (id, label, group, range, allowed values) lives in `frontend/src/data/` for UI reference without shipping the full manual.
- Regenerate the snapshot when the backend catalog changes:

  ```bash
  node -e "const fs=require('fs');const path=require('path');const source=path.join('ToneDruid','backend','ToneDruid.Api','Resources','voice-parameters.json');const target=path.join('ToneDruid','frontend','src','data','control-metadata.json');const raw=JSON.parse(fs.readFileSync(source,'utf8'));const groups=raw.parameter_groups||[];const toValueType=(type)=>{switch((type||'').toLowerCase()){case 'enum':return 'enumeration';case 'boolean':return 'boolean';default:return 'continuous';}};const controls=[];for(const group of groups){for(const param of group.parameters||[]){const entry={id:param.id,label:param.name,group:group.label,valueType:toValueType(param.type)};if(param.range&&typeof param.range.min==='number'&&typeof param.range.max==='number'){entry.range={min:param.range.min,max:param.range.max};if(param.range.unit){entry.range.unit=param.range.unit;}}if(Array.isArray(param.values)){entry.allowedValues=param.values.map((v)=>v.value).filter(Boolean);}controls.push(entry);}}fs.mkdirSync(path.dirname(target),{recursive:true});fs.writeFileSync(target,JSON.stringify(controls,null,2));"
  ```

  The command mirrors the ad-hoc build step used for this iteration; automation can follow in a future pass.
