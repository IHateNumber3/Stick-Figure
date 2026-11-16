# Stick-Figure

A Windows desktop sandbox for procedural, hand-drawn stick figures.

## Current prototype

- WPF desktop application targeting .NET 8.
- Dark drawing workspace with freehand stroke input.
- Vector stickman with articulated arms and legs, idle/walk animation, gravity and floor collision.
- Draggable crate props with gravity, friction, wall bounce and mouse interaction.
- Autonomous crate interaction: the stickman approaches a nearby crate, grabs it, carries it briefly and throws it.
- Autonomous doodling: while idle, the stickman occasionally reaches out and draws a small circular sketch directly onto the canvas.
- GitHub Actions builds the solution on Windows.

These behaviours are procedural simulations. The stickman does not have consciousness or general-purpose AI.

## Planned next milestones

1. **Drawing engine:** smoothing, curves, eraser, selection, undo/redo and a reusable stroke model.
2. **Shape recognition:** classify simple user-drawn strokes (lines, circles, rectangles) and convert them into editable scene objects.
3. **Character rig:** inverse kinematics, hand grips, expressive poses and more natural motion.
4. **Behaviour system:** traits, goals, simple memory and interactions between multiple actors.
5. **Desktop interaction:** opt-in, user-controlled interaction with selected Windows applications.

## Build

Requirements: Windows 10/11 and the .NET 8 SDK.

```powershell
dotnet build StickFigure.sln --configuration Release
dotnet run --project src/StickFigure.Desktop/StickFigure.Desktop.csproj
```
