# Stick-Figure

A Windows desktop world for autonomous, hand-drawn stick-figure characters.

## Rebuilt foundation

The prototype has been reorganized around a shared simulation world instead of one monolithic character script.

- **World simulation:** one central clock updates every creature. Characters do not own independent animation timers.
- **Creature model:** each stickman has a stable identity, generated personality traits, changing needs, memory, a current intention and physical state.
- **Decision system:** an agent scores possible intentions using its personality, needs, nearby creatures, objects, cursor position and recent behaviour.
- **Memory:** recent actions are tracked, repeated choices are discouraged, visited locations are remembered and action preferences adjust from outcomes.
- **Perception:** agents can notice the nearest other creature, nearby physics props and the desktop cursor.
- **Action layer:** wandering, socializing, drawing, resting, following the cursor, playing with props, jumping and inspecting the screen.
- **Animation layer:** procedural poses are derived from the current action and movement state. Animation does not choose what the creature wants to do.
- **Renderer:** line segments, joints, head and persistent freehand strokes are rendered separately from decision-making.
- **Desktop mode:** a transparent, click-through overlay creates a group of creatures and feeds them the real desktop cursor position.

This is a local autonomous-agent simulation, not a neural network or conscious intelligence. The architecture is designed so perception, decisions, memory, movement and rendering can evolve independently.

## Current limitations

This is a new foundation, not a finished Desktop Goose equivalent. The current world senses the cursor and other agents, and can interact with simple physics props. It does not yet understand arbitrary application UI, read window contents, manipulate windows, or perform complex social animations. Those require dedicated perception and interaction modules.

## Build

Requirements: Windows 10/11 and the .NET 8 SDK.

```powershell
dotnet build StickFigure.sln --configuration Release
dotnet run --project src/StickFigure.Desktop/StickFigure.Desktop.csproj
```
