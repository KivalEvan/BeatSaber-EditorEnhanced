# Motion Path Implementation Plan

Status: The timeline and bounded preparation code are complete. Runtime visual and timing measurements are pending.

## Goal

Add an experimental motion-path view for rotation and translation event boxes. The view reconstructs animated transform hierarchies, bakes world-space paths after relevant event changes, and renders only a bounded section near the current beat.

## Phases

1. Map runtime rotation and translation targets onto their Unity transform hierarchy and capture stable local transform data.
2. Implement local rotation and translation track evaluation that matches the editor effect behavior.
3. Compose animated ancestor transforms and bake world-space samples for the selected event box targets.
4. Cache baked paths and invalidate them after event, event-box, group, environment, and undo/redo changes.
5. Render active and ancestor paths with renderer-owned objects and cached GPU batches for beat markers.
6. Add visibility and sampling controls, then verify build and runtime behavior.
7. Resolve exact event indicators to authored events and add constrained, undoable value-editing gizmos.

## Initial Deliverable

Implement the smallest useful vertical slice: selected rotation/translation event-box paths, hierarchy-aware world-space composition, bounded current-beat rendering, and a master visibility toggle. Keep unsupported environment structures safe by omitting paths rather than changing live transforms.

## Current Defaults

- Motion paths are enabled by default. Path, current-beat, whole-beat, sub-beat, event, and label markers are visible.
- Focused and unfocused line widths default to 0.2 and 0.1.
- Current-beat, whole-beat, sub-beat, event, and label sizes are 0.3, 0.4, 0.5, 0.75, and 0.14. Label offset is 1.
- Display independently configurable backward and forward ranges from 0 to 8 beats; defaults are 0.5 and 1.5 beats.
- Selected operations bake only the configured visible backward and forward ranges, plus the configured bake buffer around the requested beat, at the configured samples per beat.
- Use four sub-beat divisions between whole-beat markers.
- Evaluate expanded rotation and translation events at their exact times, add those points to the path, and mark them with gold diamonds.
- Render paths outside the current event-box focus in white.
- Render at most 64 selected targets and 96 selected/ancestor paths.
- Show at most 256 event markers. A zero limit hides event markers.

## Runtime Behavior

- While the visible range remains inside the baked window, playhead movement and visual-only settings update the displayed path without recalculating path geometry.
- Path-line alpha peaks at the clamped playhead and smoothly fades to zero at each visible-range edge, including across snap-separated line sections.
- Whole-beat and sub-beat markers appear on each visible path. The primary path retains the current-beat marker and beat labels.
- The renderer caches beat and sub-beat marker matrices. It submits GPU batches of 511 matrices or fewer each frame.
- The renderer rebuilds marker matrices after a range, track plan, or marker setting change.
- If GPU instancing is unavailable, the renderer uses the existing pooled marker path.
- Mesh markers use the bundled Gizmo material; anti-aliased path lines and rotation drag indicators keep their generated line material.
- Selected plans cover only one bounded bake window. The manager does not bake the full song.
- The planner prepares one bake session for each geometry version. The session contains captured sources, animations, nodes, transforms, and event beats.
- Session preparation includes only the selected targets and the ancestors that compose their world transforms.
- The planner scans runtime bindings in small steps. It resolves events only for bindings on the selected hierarchy.
- The authored-source lookup includes only runtime group IDs that bind to the selected hierarchy.
- The source lookup includes all authored groups for each included group ID. Thus, ownership and event pruning stay unchanged.
- Each bake session creates fresh incremental windows without another read from the editor, effect managers, or live transforms.
- An uncached request first creates a completed preview at no more than eight regular samples per beat.
- Preview windows retain range endpoints, exact event beats, and snap boundaries.
- The renderer shows the completed preview before the manager creates the full-quality window for the same range.
- A configuration of eight samples per beat makes the preview full quality. The manager does not create a second window.
- The manager keeps four completed full-quality windows in a least-recently-used cache.
- A cache hit shows the full-quality window immediately. It does not create another bake operation.
- Geometry and source changes remove the cache. Sampling and geometry configuration changes also remove the cache.
- Disable and disposal actions remove the cache.
- After full-quality publication, the manager creates one overlapping forward window at full quality.
- The next window uses the forward validity boundary of the current plan as its center. This gives both windows a deterministic overlap.
- A completed forward window enters the cache. The manager shows it when the playhead enters its valid range.
- A far or backward seek removes only the pending window. The retained session and cache remain available.
- An uncached seek creates a new preview near the target. The completed preview stays visible during full-quality refinement.
- Exact event times, step beats, hierarchy evaluation, event markers, and gizmos remain preserved in each window.
- Changes to event data, selection, mode, level data, source data, sampling, bake buffer, or path limits remove the session.
- A range change keeps the session. It also keeps a plan or operation that still contains the requested visible range.
- The renderer retains full baked samples for evaluation, markers, and gizmos.
- Focused lines use at most 32 regular samples per beat. Unfocused lines use at most 16 regular samples per beat.
- Line samples retain range endpoints, track and ancestor event beats, and snap boundaries.
- The manager applies time and presentation updates in `LateTick`. It updates a clip no more than once per frame.
- The manager prepares each session on the main thread. A second resumable operation calculates samples for each window.
- Each `LateTick` gives preparation or the pending window operation a 1.5 ms budget.
- Each source-lookup step processes one group, box, filter element, base event, candidate, or source bucket.
- Data-model calls and index-filter conversion remain indivisible.
- A sample can continue across frames. Its world-matrix cache stays valid until the operation processes all tracks at that beat.
- The renderer receives a plan only after the operation creates all tracks. The renderer never receives a partial plan.
- Geometry changes remove preparation, the session, and the pending window operation. They also clear the old path.
- Request, preparation, and session versions prevent preview, refinement, prefetch, and cache publication after a geometry change.
- Presentation changes keep a valid plan and operation. A range change creates a new window only when the old window is too small.
- `LateTick` continues the instanced marker submission while an operation runs. A path can appear late to keep the editor responsive.
- Session preparation captures editor data, effect data, events, hierarchy transforms, and exact event beats on the main thread.
- A new edit, selection, geometry configuration change, disable action, or disposal action removes an incomplete preparation.
- Event markers use the current rotation or translation context, apply the configured cap in a deterministic order, and keep the lowest stable editable source for collocated markers.
- Hidden event markers have no editable handles. Visible handles continue to prefer events nearest the current playhead when they overlap.

## Editable Event Indicators

- Every valid, runtime-played translation event with an invertible authored value uses the bundled `Assets/translation.prefab` handle.
- Every valid, runtime-played rotation event with an invertible authored value uses the bundled `Assets/rotation.prefab` handle.
- The controller clones the loaded prefabs through `DiContainer`; marker creation does not reload the asset bundle.
- Motion-path handles use separate rotation and translation pools.
- Built-in prefab colliders and drag components stay disabled; all cloned descendants use interaction layer 2.
- A generated radial line supplements the bundled rotation model only during drag feedback.
- Editing changes only the authored value and preserves timing, easing, loops, direction, and use-previous state. Translation values remain unclamped, matching the runtime and regular gizmo behavior.
- Drag edits use the active scroll precision: rotation snaps by the configured angle delta, while translation snaps authored deltas using the configured number of environment-limit divisions. Preview feedback shows the same snapped authored value that is committed.
- Events from other boxes and animated ancestors can be edited without changing editor focus.
- In Event Boxes mode, exact-event indicators and editable handles are filtered to the selected rotation or translation group type. The filter includes matching events from selected boxes, other boxes, and animated ancestors; path geometry continues to evaluate both transform types.
- Spatially coincident indicators remain editable. A marker can visually represent collocated geometry and creates one handle for its lowest stable-order invertible source.
- Input is consumed only when a pointer gesture starts on an editable indicator.
- Commits use the editor's full-event command signals so undo and redo remain available.
