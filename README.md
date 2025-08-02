# Beat Saber Editor Enhanced

For level designer, by level designer, to improve your workflow with various QOL and features.

## Features

* v3 Light Gizmo
    * View gizmo for color, rotation, translation and FX with their orientation
    * Highlight selected gizmo when hovered on lane
    * Draggable translation gizmo
        * Creates event based on event box context, cursor time and event box state
* Better Event Box Reordering
    * Drag the tab to reorder them than using button
* Sort Event Box
* Event Box Copy & Duplicate
* Random Seed Clipboard
* Randomise Seed On Paste
* Unclamped Value
    * Basic event float value and distribution value no longer have arbitrary clamp
    * Certain input like negative value and index filter field require clamp to prevent issue
* Better Scrollable Input Value
* Simple Math Evaluation on Field Input
* Built-in auto-light for v2 (Lolighter)
* Couple of bug fixes

## Installation

Simply place `EditorEnhanced.dll` onto `Plugins` folder.

### Mods Required

* BSIPA
* SiraUtil

## To-do

* [ ] Mass Value Shift
* [ ] Hide translation event (similar to extension but for hiding them to shadow realm)
* [ ] Focused v3 light view (hides every other v3 light but selected)
* [ ] Customisable gizmo view
* [ ] Error checker port
* [ ] Selector plugin port
* [ ] Redesign UI (for the mod, not the base game)
* [ ] Optimisation for this mod

## Known Issue

* UI do not have audio feedback
