# 🪙 Coin Chase 3D: The Labyrinth Escape

A 3D labyrinth adventure game built in Unity featuring dual PC and Mobile input support, responsive player movement, dynamic hazards, and an in-game maze navigation assistance system.

---

## 🎮 Features

- **Dual-Device Input Mode**:
  - Switch seamlessly between **PC (Keyboard & Mouse)** and **Mobile (Virtual Touch Controls)** directly from the Main Menu.
  - Automatically saves your preference via `PlayerPrefs`.
- **Cinematic 3D Main Menu**:
  - Rotating gold coin centerpiece, ambient nature environment, and floating particle sparkles.
  - Live controls guide bar reflecting your active device choice.
- **Mobile Touch Overlay**:
  - Left virtual joystick for movement (`WASD`).
  - Right-screen swipe zone for camera look.
  - Dedicated on-screen **JUMP**, **RUN**, **CHEAT**, and **PAUSE** buttons.
  - Auto-hides during Pause, Game Over, and Victory menus to keep UI clean.
- **Maze Navigation Assistance (Cheat Mode)**:
  - Toggle directional guide arrows at any time (press **[M]** on PC or tap the on-screen **CHEAT** button on mobile) to point your way out of the maze.
- **Hazards & Checkpoint System**:
  - Obstacle collisions with invincibility frames and hit points.
  - Instant hazard detection (water / death zones).
  - Respawn from checkpoints or restart.
- **Victory & Scoring**:
  - Collect gold coins throughout the labyrinth.
  - Final reward and score tally upon escaping.

---

## 🕹️ Controls

| Action | PC (Keyboard & Mouse) | Mobile (Touch Screen) |
|---|---|---|
| **Move** | `W`, `A`, `S`, `D` | Left Virtual Joystick |
| **Look / Aim** | Mouse Movement | Right-Screen Drag / Swipe |
| **Jump** | `Spacebar` | `JUMP` Button |
| **Sprint** | `Left Shift` | `RUN` Toggle Button |
| **Maze Guide Arrows** | `M` Key | `CHEAT` Button (Top-Right) |
| **Pause / Menu** | `Escape` Key | `PAUSE` Button (Top-Left) |

---

## 🛠️ Tech Stack & Requirements

- **Engine:** Unity 6 (`6000.3.24f1`)
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Input:** Unity New Input System
- **Language:** C#
