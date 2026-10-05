# Virtual Aquarium for Unity (URP)

A modular, high-performance 3D underwater simulation and procedural aquarium environment package for Unity (Universal Render Pipeline). Compatible with **Unity 2022.3 LTS**, **Unity 2023**, and **Unity 6+**.

---

## ✨ Features

- 🐟 **Flocking Boid AI (`FishAgent`)**:
  - Natural school cohesion, alignment, and separation.
  - Threat detection and panic fleeing behavior.
  - Depth habitat stratification (Surface, MidWater, DeepWater, BottomDweller, FullColumn).
  - Realistic banking roll angles and swimming noise.
- 🎮 **3rd-Person Fish Player Controller (`FishPlayerController`)**:
  - Input System swimming controls (W/A/S/D swim & turn, Space/Ctrl ascend/descend, Shift boost).
  - Dynamic runtime fish mesh swapping with visual scale multiplier.
  - Real-time third-person camera tracking with zoom & overview modes.
  - Fish possession mode (`Tab` key).
- 🪸 **Procedural Seabed Decorator (`SeabedDecorator` & GUI Window)**:
  - Procedurally clusters rocks, coral reefs, kelp forests, anemones, and benthic fauna.
  - Mathematical vertex-accurate seabed grounding (no floating decorations).
  - GPU Instancing and shared material caching for high frame rates.
  - Drag-and-drop exportable `AquariumEnvironment.prefab`.
- 🫧 **Volumetric Bubble Particle System (`UnderwaterBubbleEffect`)**:
  - Rising animated air bubbles with horizontal buoyancy wobble, depth fade, and burst scaling.

---

## 📦 Installation

### Method 1: Via Unity Package Manager & Git URL (Recommended)
1. Open your Unity Project (**Unity 2022.3 LTS or newer**).
2. Go to **Window > Package Manager**.
3. Click the **`+`** button in the top-left corner and select **Add package from git URL...**.
4. Enter the repository URL:
   ```
   https://github.com/Aleclock/VirtualAquarium.git
   ```
5. Click **Add**.

### Method 2: Via Local Disk
1. Clone or download this repository.
2. In Unity Package Manager, click **`+` > Add package from disk...**.
3. Select the `package.json` file in the root folder.

---

## 🚀 Quick Start Guide

### 1. Generate an Aquarium in your Scene
1. In the Unity Editor menu, click **`Aquarium > Create Full Aquarium Setup`**.
2. This creates:
   - `Aquarium Controller` (with `AquariumBounds`, `FishSpawner`, `FishCatalogSO`).
   - `PlayerFish` (3rd-person playable fish with camera rig).
   - `[Seabed Environment]` (procedurally generated corals, rocks, and bubble vents).

### 2. Customize Seabed Decorations
Open **`Aquarium > Seabed Decorator Settings...`** to adjust:
- Density, Reef Knoll frequency, Kelp Forest density, and Fauna distribution.
- Random Seed and Bubble Vents count.
- Click **Decorate Seabed (Generate)** to preview and generate instantly.

### 3. Change Player Fish Mesh
Select `PlayerFish` in the Hierarchy -> Inspect `Fish Player Controller`:
- Assign any fish prefab into **Fish Prefab** (e.g. `BlueTang`, `Clownfish`, `Lionfish`).
- Adjust **Mesh Scale** to resize the model.

---

## 📋 Requirements
- **Unity Version**: 2022.3 LTS or newer
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Input System**: `com.unity.inputsystem` (1.5.0+)

---

## 📚 References & Resources

- **Fish 3D Models & Animations**: [Animated Fish Bundle](https://poly.pizza/bundle/Animated-Fish-Bundle-44zhHN1UbT) on Poly Pizza (by Quaternius).
- **Environment & Seabed Props**: 3D sea floor assets (corals, rocks, plants, anemones, shells, dock props) sourced from [Poly Pizza](https://poly.pizza/).

---

## 📄 License
MIT License - Copyright (c) 2026 Alessandro Clocchiatti
