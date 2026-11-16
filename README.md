# Stick-Figure

A Windows desktop sandbox for procedural, hand-drawn stick figures.

## Current prototype

- WPF desktop application targeting .NET 8.
- Dark drawing workspace with freehand stroke input.
- Articulated stickman renderer with action-driven movement and pose animation.
- Draggable crate props with gravity, friction, wall bounce and mouse interaction.
- Agent architecture: utility-scored goal selection, environmental perception, short-term action memory, adaptive action preferences and an animation controller.
- Agents can explore toward chosen destinations, approach and acknowledge nearby agents, draw, rest and seek nearby props.
- Desktop mode creates four independently deciding agents sharing one perception world; they can notice and approach one another rather than following a single shared script.
- Agents can approach a crate, pick it up, carry it and throw it.
- GitHub Actions builds the solution on Windows.

This is a lightweight, local agent simulation—not a neural language model, consciousness or general-purpose intelligence. Decisions are made by utility scoring over perceived surroundings, needs, personality traits and recent action history. Memory adjusts action preferences based on outcomes; it does not train a neural network.

## Planned next milestones

1. **Drawing engine:** smoothing, curves, eraser, selection, undo/redo and a reusable stroke model.
2. **Shape recognition:** classify simple user-drawn strokes (lines, circles, rectangles) and convert them into editable scene objects.
3. **Character rig:** inverse kinematics, hand grips, expressive poses and more natural motion.
4. **Richer agent behaviour:** additional goals, object manipulation, social behaviours and event-driven reactions.
5. **Desktop interaction:** opt-in, user-controlled interaction with selected Windows applications and cursor events.

## Build

Requirements: Windows 10/11 and the .NET 8 SDK.

```powershell
dotnet build StickFigure.sln --configuration Release
dotnet run --project src/StickFigure.Desktop/StickFigure.Desktop.csproj
```
