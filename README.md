# Beat Saber Editor Enhanced

For level designer, by level designer, to improve your workflow with various QOL and features.

## Features

* v3 Light Gizmo
    * View gizmo for colour, rotation, translation and FX with their orientation
    * Highlight current event box lane and selected gizmo when hovered on lane
    * Draggable translation gizmo
        * Create event based on event box context, cursor time and event box state
    * Clickable to select event box
* Better Event Box Reordering
    * Drag the tab to reorder them than using button
    * Lane gizmo can also be dragged to reorder
* Sort Event Box
* Improved Event Box Duplicate & Copy
    * Ability to copy events, randomise seed, increment ID and add value to existing events.
* Simple Offset Button for Beat Distribution
* Random Seed Clipboard
* Randomise Seed on Paste
* Unclamped Input Value
    * Basic event float value and distribution value no longer have arbitrary clamp
    * Certain inputs like negative value and index filter field require clamp to prevent issue
* Better Scrollable Input Value
* Simple Math Evaluation on Field Input
* Built-in Autolight for v2 (Lolighter)
* Custom Precision Value
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
* [ ] Fix rotation drag gizmo
* [ ] Error checker port
* [ ] Selector plugin port
* [ ] Redesign UI (for the mod, not the base game)
* [ ] Optimisation for this mod

## Known Issue

* Drag rotation gizmo does not work as intended
* Certain UI does not have audio feedback
