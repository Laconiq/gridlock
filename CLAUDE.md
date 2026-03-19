# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AIWE is a Unity 6 (6000.3.10f1) 3D project using the Universal Render Pipeline (URP). It's in early development with a foundation for a character-driven interactive experience supporting keyboard/mouse, gamepad, touch, and XR input.

## Architecture

- **Rendering:** Dual URP configuration — `PC_RPAsset` for desktop quality, `Mobile_RPAsset` for mobile. Settings live in `Assets/Settings/`.
- **Input System:** Uses Unity's new Input System (1.18.0). `Assets/Scripts/Controls.inputactions` defines all bindings. `Assets/Scripts/Controls.cs` is the auto-generated C# wrapper — do not edit manually.
- **Scene:** Single scene at `Assets/Scenes/SampleScene.unity` (build index 0).

## Key Packages

- `com.unity.render-pipelines.universal` — URP rendering
- `com.unity.inputsystem` — New Input System
- `com.unity.ai.navigation` — NavMesh/AI navigation
- `com.unity.timeline` — Cutscene/animation sequencing
- `com.unity.visualscripting` — Visual scripting support
- `com.unity.test-framework` — Testing

## Input Actions (Controls.inputactions)

**Player actions:** Move, Look, Attack, Interact (hold), Crouch, Jump, Sprint, Previous, Next
**UI actions:** Navigate, Submit, Cancel, Point, Click, RightClick, MiddleClick, ScrollWheel, TrackedDevicePosition, TrackedDeviceOrientation

## Development

- Open the project in Unity 6 (version 6000.3.10f1)
- Solution file: `AIWE.sln`
- C# scripts go in `Assets/Scripts/`
- When modifying input bindings, edit `Controls.inputactions` in Unity — the C# wrapper regenerates automatically
