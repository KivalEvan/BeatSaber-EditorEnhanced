# Motion Path Implementation Plan

Status: The rolling chunk cache, progressive renderer, and worker-thread sampler are complete. Runtime visual and timing measurements are pending.

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
- The default sampling density is 48 samples per beat. The permitted range is 8 through 128 samples per beat.
- The planner divides the song into fixed four-beat chunks from song beat zero.
- Use four sub-beat divisions between whole-beat markers.
- Evaluate expanded rotation and translation events at their exact times, add those points to the path, and mark them with gold diamonds.
- Render paths outside the current event-box focus in white.
- Render at most 64 selected targets and 96 selected/ancestor paths.
- Show at most 256 event markers. A zero limit hides event markers.

## Runtime Behavior

- The manager requests only the visible chunks, two chunks ahead, and one chunk behind.
- The final chunk ends at the song end. The manager does not start a full-song bake.
- A cache key contains the chunk index, the geometry generation, and the sampling density.
- The manager keeps at most ten completed chunks in a least-recently-used cache.
- At 1,024 tracks and 128 samples per beat, ten full chunks contain about 80 MiB of raw regular samples.
- This estimate excludes exact-event samples, collection storage, immutable wrappers, event data, and renderer copies.
- The manager pins the visible, forward, and backward chunks during cache removal.
- Transition placeholders stay only in the renderer. They do not increase the completed-cache limit.
- A prepared hierarchy session creates incremental operations for one chunk and one contiguous track range.
- The manager uses one explicit work queue with this priority:
  1. Visible focused segments.
  2. Visible remaining segments.
  3. Forward focused segments.
  4. Forward remaining segments.
  5. Backward segments.
- A cache hit publishes an available focused segment before the manager starts missing background work.
- The planner publishes focused tracks before unfocused tracks in each requested range.
- The manager publishes at most one immutable track segment in each `LateTick`.
- Each `LateTick` gives preparation or the active chunk operation a 1.5 ms budget.
- A far seek removes only incompatible active work. Compatible completed chunks and partial segments remain reusable.
- Visible work preempts background work. An active background operation continues while no visible segment is missing.
- Equal-priority order changes do not cancel an active operation.
- Obsolete partial chunks do not remain outside the visible and prefetch set.
- Operation construction does not allocate native arrays, flatten a hierarchy, or create a track range.
- Budgeted advancement finds the shared event range and creates one baked track at a time.

## Job Sampling

- Unity 6000.0.40f1 standard Jobs run the computational sample stage. Plugin jobs do not use `BurstCompile` or rely on Burst AOT.
- An operation divides its ordered sample beats into sequential `IJobParallelFor` batches.
- The operation schedules one batch at a time for one chunk and contiguous track range.
- The manager permits at most one scheduled or draining motion-path job.
- Each worker iteration evaluates all required nodes parent first, then writes world positions for every selected track at that beat.
- Worker input contains only blittable node, binding, event, track-index, and sample-time records in `NativeArray` storage.
- Nodes store parent indices, captured local TRS values, animation flags, and the last active binding index for each axis.
- Bindings store contiguous event ranges, mirroring, and translation and distribution limits.
- Runtime events store time, value, distribution, easing, loop, direction, and next-event indices.
- Output and world-matrix scratch use sample-major layouts. Each worker iteration writes disjoint ranges.
- Reusable flattened native-input payload has a 16 MiB limit. This limit includes nodes, bindings, events, track indices, and sample times.
- Each batch has a 4 MiB world-matrix scratch limit and a separate 4 MiB output limit.
- The node count and track count determine the sample count for each batch.
- At 1,024 nodes and 1,024 tracks, a batch contains at most 64 samples.
- This batch uses 4 MiB of matrix scratch and 768 KiB of position output.
- The payload limits exclude native allocation headers.
- The operation builds sample beats, flattened descriptors, native allocations, and native copies incrementally inside the 1.5 ms budget.
- Reusable input arrays use persistent allocation until all batches finish.
- Batch scratch and output arrays use persistent allocation until the operation consumes that batch.
- Safe native arrays use uninitialized allocation. The operation fills each input before job scheduling.
- Regular ticks poll `JobHandle.IsCompleted`. They call `Complete` only after the handle reports completion.
- A stale operation rejects publication and remains owned until its current batch completes.
- The stale operation then disposes the batch arrays and all reusable input arrays.
- Plugin shutdown can complete an in-flight job synchronously so native memory is always released.
- A concrete flattening, allocation, native-copy, or scheduling failure activates the managed sampler for that geometry generation.
- The fallback logs once for the generation and does not combine managed and job output in one operation.
- The managed fallback retains the fixed 4,096-slot ancestor cache, 3,072-entry reset limit, and 320 KiB array payload.
- Job execution faults do not silently fall back. They enter the manager's existing bake-failure path.
- Authored event-source collection, exact event-point finalization, visual-index selection, publication, caching, and rendering stay on the main thread.

## Chunk Boundaries

- The sample grid uses absolute ticks from song beat zero.
- A nonfinal chunk owns its start beat and excludes its end beat.
- The next chunk owns the shared boundary. The final chunk owns the song-end sample.
- Exact event points and step beats use the same boundary rule.
- The renderer stores ordered chunk segments for each planner track.
- Segment insertion, replacement, and removal do not clear unrelated tracks or segments.
- The renderer joins consecutive chunks and stores one copy of an equal boundary sample.
- The renderer does not join consecutive chunks from different geometry generations.
- A mixed-generation boundary remains a visual discontinuity until the new segment replaces the placeholder.
- Missing chunks remain gaps. The renderer does not draw a line across a gap.

## Invalidation and Presentation

- An event or geometry edit starts a new generation and removes the old completed cache.
- Old-generation cache entries cannot satisfy new requests or publish new segments.
- The renderer keeps old visible segments as transition placeholders during a rebuild.
- Event handles stay disabled until all visible segments use the current generation.
- A current-generation publication replaces the old segment with the same track and chunk index.
- The manager removes a placeholder after replacement or after its chunk leaves the required set.
- Selection, mode, disable, and disposal changes can clear all renderer and planner state.
- Request, preparation, session, operation, and chunk keys prevent stale publication.
- Presentation-only changes keep the prepared session and chunk data.
- Path alpha peaks at the playhead and fades to zero at the visible-range edges.
- Whole-beat, sub-beat, event, label, and gizmo data use the same joined visible segments.
- A segment publication rebuilds only its rendered track and its marker state.
- If the playhead changed, publication also refreshes all existing track lines in the same `LateTick`.
- If the playhead did not change, publication does not scan all track lines unless a presentation update is pending.
- The renderer defers and coalesces timeline descriptors, gizmos, and global marker presentation until the end of `LateTick`.
- Descriptor rebuilding uses the current segment set. Thus, stale event sources do not remain.
- The renderer submits marker batches of 511 matrices or fewer. It uses pooled markers when GPU instancing is unavailable.
- Focused lines use at most 32 visual samples per beat. Unfocused lines use at most 16 visual samples per beat.
- Session preparation captures editor data, effect data, events, hierarchy transforms, and exact event beats on the main thread.
- Each source-lookup step processes one group, box, filter element, base event, candidate, or source bucket.
- Data-model calls and index-filter conversion remain indivisible.
- Event markers keep the lowest stable editable source for collocated markers.
- Hidden event markers have no editable handles.
- The fixed chunk model does not use or show the old bake-buffer setting.

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
