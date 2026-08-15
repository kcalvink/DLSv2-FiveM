# DLSv2 → FiveM Compatibility Map

This document treats the in-repo DLSv2 source as the canonical specification and tracks verified compatibility status.

Status values:
- `COMPLETE`
- `PARTIAL`
- `FIVE_M_LIMITATION`
- `NOT_IMPLEMENTED`

| DLSv2 Feature | FiveM Implementation | Status | Notes |
|---|---|---|---|
| XML discovery (`Plugins\\DLS\\*.xml`) | Validator scans explicit file path(s); sample configs in `configs/vehicles` | PARTIAL | Runtime auto-discovery in FiveM resource not implemented yet. |
| XML root model (`Model vehicles=...`) | `tools/DLSv2Validator` validates root structure and vehicles list | COMPLETE | Verified against `Core/DLSModel.cs`. |
| Light modes (`Modes/Mode`) | Validator parses and normalizes mode names | COMPLETE | Runtime mode execution not yet ported. |
| Audio modes (`Audio/AudioModes/AudioMode`) | Validator parses and normalizes audio mode names | COMPLETE | Runtime audio playback parity not yet ported. |
| Control groups (`ControlGroups`, `AudioControlGroups`) | Validator parses group/mode mappings and key names | COMPLETE | Runtime keybinding behavior not yet ported. |
| Dynamic condition XML element mapping (`TypeName.Replace("Condition", "")`) | Validator recognizes DLSv2 condition element set and timing attributes | PARTIAL | Condition runtime evaluation engine in FiveM not implemented yet. |
| Condition timing (`delay_time`, `max_on_time`, `min_on_time`, `stay_on_time`) | Validator recognizes attributes and reports unsupported fields | COMPLETE | Runtime timing state machine not yet ported. |
| Sequences shorthand (`Mode/Sequences/Item`) | Validator parses and normalizes sequence items | COMPLETE | Runtime sequence → siren application not yet ported. |
| Siren settings schema (`SirenSettings/...`) | Validator supports schema and unknown-property detection | COMPLETE | Runtime per-light siren behavior not yet ported. |
| Vehicle matching by model list (`vehicles` CSV) | Validator supports `--model` checks | COMPLETE | Case-insensitive model comparison. |
| Extras handling and restore | Not implemented in FiveM runtime | NOT_IMPLEMENTED | Needs per-vehicle state tracking and restore semantics from `ManagedVehicle`. |
| Indicators/hazards interactions | Not implemented in FiveM runtime | NOT_IMPLEMENTED | Requires control + mode layering parity. |
| Stage/mode merge precedence | Not implemented in FiveM runtime | NOT_IMPLEMENTED | Must match `ManagedVehicle.UpdateLights` ordering rules. |
| Audio control hold/toggle/cycle semantics | Not implemented in FiveM runtime | NOT_IMPLEMENTED | Must match `ManagedVehicle.RegisterInputs` behavior. |
| Trigger system (all condition types) | Not implemented in FiveM runtime | NOT_IMPLEMENTED | Full vehicle/global/road condition parity pending. |
| Traffic advisor directional patterns | Not implemented in FiveM runtime | NOT_IMPLEMENTED | Requires sequencer-compatible TA mapping. |
| Pattern sync (`PatternSync`) | Not implemented in FiveM runtime | NOT_IMPLEMENTED | DLSv2 hash-offset sync model pending. |
| Drift range (`SpeedDrift`) | Not implemented in FiveM runtime | NOT_IMPLEMENTED | DLSv2 time-multiplier drift behavior pending. |
| Multiplayer replication and state recovery | Not implemented in FiveM runtime | NOT_IMPLEMENTED | Needs state bag/event ownership design and recovery logic. |
| RAGE Plugin Hook memory-dependent behavior | FiveM-native replacement required | FIVE_M_LIMITATION | Direct RPH memory hooks (`Memory`, `SirenInstance`, `SirenSounds`) are not portable as-is. |
| Debug current modes tooling | Not implemented in FiveM runtime | NOT_IMPLEMENTED | FiveM debug overlay/logger still pending. |
| Config validator tool | `dotnet run --project tools/DLSv2Validator -- --file <xml> [--model <name>]` | COMPLETE | Reports parse errors, unsupported properties, normalized summary, and vehicle matching. |

## Verified source references

- XML schema and models: `DLSv2/Core/DLSModel.cs`, `DLSv2/Core/SirenSetting.cs`
- Condition engine and attributes: `DLSv2/Core/Condition.cs`, `DLSv2/Core/GroupConditions.cs`, `DLSv2/Conditions/*.cs`
- Mode/control behavior: `DLSv2/Core/ManagedVehicle.cs`, `DLSv2/Core/Wrappers.cs`
- Loading/default mode handling: `DLSv2/Utils/Loaders.cs`
- Sync/drift behavior: `DLSv2/Utils/SyncManager.cs`
