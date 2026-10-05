# Changelog

All notable changes to the Virtual Aquarium package will be documented in this file.

## [1.0.0] - 2026-10-04

### Added
- Initial UPM package release for Unity 2022.3 LTS, Unity 2023, and Unity 6.
- Autonomous 3D Boid Fish Flocking AI (`FishAgent`).
- Customizable fish school presets and catalog ScriptableObjects (`FishCatalogSO`, `FishSchoolSO`).
- Standalone Third-Person Player Fish Controller (`FishPlayerController`).
- Dynamic runtime/edit-mode fish mesh customization with isolated `VisualSlot`.
- Procedural Seabed Decorator with interactive Editor Window (`SeabedDecoratorWindow`).
- High-performance GPU Instancing and shared material caching.
- Underwater bubble particle system (`UnderwaterBubbleEffect`).
- Assembly Definitions (`VirtualAquarium.Runtime.asmdef` and `VirtualAquarium.Editor.asmdef`).
- Unity 2022+ cross-version compatibility helpers (`UnityCompat.cs` and `PackageAssetUtility.cs`).
