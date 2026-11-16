# Stick-Figure

A Windows desktop sandbox for procedural, hand-drawn stick figures.

## Current milestone

**Milestone 0 — project foundation**
- WPF desktop application targeting .NET 8.
- Dark drawing workspace with freehand stroke input.
- Stroke data is stored as editable vector points rather than flattened sprites.
- Clear canvas and basic project structure.

## Planned milestones

1. **Drawing engine:** pressure-ready stroke model, smoothing, curves, eraser, selection and undo/redo.
2. **Stick-figure rig:** head, torso, limbs, joints and inverse-kinematics helpers.
3. **Procedural animation:** walk, idle, reach, fall and expressive poses.
4. **Autonomous actors:** needs, traits, memory and a decision loop (simulated behaviour, not consciousness).
5. **Desktop interaction:** opt-in, user-controlled interaction with selected Windows applications.
6. **Creation system:** turn drawings into editable animated actors and props.

## Build

Requirements: Windows 10/11 and the .NET 8 SDK.

```powershell
dotnet build StickFigure.sln
dotnet run --project src/StickFigure.Desktop/StickFigure.Desktop.csproj
```

The first prototype is intentionally small. Real application automation, autonomous behaviour and multi-character simulation are not implemented yet.
