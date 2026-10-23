# GLITCH RUN — BREAK THE SIMULATION

Welcome to the repository for **GLITCH RUN — BREAK THE SIMULATION**, a fast-paced 2D cyberpunk precision platformer where reality itself is unstable. Players manipulate glitches in code to shatter the confines of an artificial simulated world.

---

## 🎮 Game Overview

In **GLITCH RUN**, you navigate a hostile, simulated reality on the verge of collapse. To escape, you must exploit system anomalies by triggering a temporary "Glitch" state that alters the laws of the simulation around you.

### Objective
1. **Collect 3 Data Fragments** scattered across the sector to decrypt the simulation core.
2. **Reach the Exit Portal**, which remains locked until all 3 fragments are secured.

---

## 🕹️ Controls

The game utilizes Unity's **New Input System** (package version 1.20.1).

| Action | Primary Input (Keyboard) | Alternative Input |
| :--- | :--- | :--- |
| **Move Left / Right** | `A` / `D` | `Left Arrow` / `Right Arrow` |
| **Jump** | `Space` | — |
| **Activate Glitch** | `E` | — |
| **Restart Level** | `R` | — |

---

## ⚡ The Glitch Ability

The core mechanic of the game allows the player to briefly tear open the simulation's logic:

- **Duration:** **3 seconds** active window.
- **Cooldown:** **8 seconds** before it can be activated again.
- **Effects:**
  - **Freezes Designated Hazards:** Halts lethal obstacles such as laser barriers, patrolling security sentries, or spikes.
  - **Reveals Hidden Platforms:** Phase-shifted glitch platforms become solid and visible, opening up secret paths and bypass routes.

---

## 🛠️ Project & Engine Specifications

- **Unity Version:** `6000.6.7f1` (Unity 6)
- **Input System:** Unity New Input System (`com.unity.inputsystem` v1.20.1)
- **Physics Engine:** Unity 2D Physics (`Rigidbody2D`, `Collider2D`)
- **Render Pipeline:** 2D Universal Render Pipeline (URP)

---

## 📜 Planned Gameplay Scripts

The core gameplay architecture is divided into modular, focused components:

1. **`PlayerController`** — Manages responsive horizontal movement, jumping, ground checks, and 2D physics integration.
2. **`GameManager`** — Tracks global game states, fragment counts, player defeat, victory triggers, and scene restarts.
3. **`GlitchAbility`** — Oversees the 3s active glitch state, 8s cooldown timer, input activation (`E`), and broadcast events.
4. **`Hazard`** — Detects lethal contact with the player and handles optional freeze/thaw behavior during glitch events.
5. **`DataFragment`** — Represents collectible simulation fragments with single-pickup validation.
6. **`ExitDoor`** — The level endpoint that unlocks upon collecting all 3 data fragments.

> [!NOTE]
> Detailed interface signatures, component requirements, inspector variables, and teammate integration notes are documented in [**`SCRIPT_CONTRACTS.md`**](file:///c:/Users/upadh/GlitchRun-Prototype/SCRIPT_CONTRACTS.md). All interfaces are currently **PROPOSED** and awaiting team review prior to writing C# code.

---

## 🚧 Development Status

> [!IMPORTANT]
> **Work in Progress:** Gameplay scripts, mechanics, and interactive prefabs are currently under active development. All mechanics must be verified and playtested within the Unity Editor (`Unity 6000.6.7f1`) prior to staging and release.
