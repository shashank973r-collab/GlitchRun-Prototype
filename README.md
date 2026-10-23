# GLITCH RUN — BREAK THE SIMULATION
**Playable Prototype — 2D Cyberpunk Side-Scroller**

*Engine: Unity 6 (6000.6.7f1) | URP 2D | Branch: `feature/playable-prototype`*

---

## 🎮 Game Overview & Objective
In **Glitch Run**, you control a cyber-runner attempting to breach a high-security virtual simulation. Navigate across charcoal industrial platforms, leap over bottomless voids, collect neon-green data fragments, and use your signature **Glitch Phase** ability to phase unharmed through lethal red laser firewalls to reach the terminal breach exit gate.

---

## 🕹️ Controls

| Action | Keyboard | Gamepad |
| :--- | :--- | :--- |
| **Move Left / Right** | `A` / `D` or `←` / `→` | Left Stick / D-Pad |
| **Jump** (Variable height) | `Space`, `W`, or `↑` | `Button South` (A / ✕) |
| **Glitch Phase** (Hazard Immunity) | `Left Shift` or `Right Shift` | `Right Shoulder` / `Right Trigger` |
| **Restart Simulation** | `R` (on GameOver / Victory) | `Start` / Menu Button |

> **Glitch Tip**: Red lasers are normally lethal on contact! Trigger your **Glitch Ability** (`Shift`) just before touching a laser to phase straight through the beam unharmed. Keep an eye on your Glitch meter: it provides 2.0 seconds of phase time with a 3.5-second cooldown.

---

## 🚀 How to Play the Prototype

1. Open the project in **Unity 6 (6000.6.7f1)**.
2. In the Project window, navigate to:
   `Assets/Scenes/PrototypeScene.unity`
3. Double-click to open `PrototypeScene.unity`.
4. Press the **Play** button in the Unity Editor toolbar.
5. **Level Progression**:
   - **Section 1 (Ground Zero)**: Practice movement and jump controls. Collect Data Fragment 1/5.
   - **Section 2 (The Firewall)**: Encounter Laser Hazard 1. Press `[Left Shift]` to Glitch through the red beam and retrieve Data Fragment 2/5.
   - **Section 3 (Stepping Stones & Cycling Laser)**: Leap across floating ledges over the abyss. Time or glitch through the cycling laser to secure Data Fragment 3/5 (*Exit Objective Unlocked!*).
   - **Section 4 (The Core Run)**: Face dual laser defenses. Collect bonus Data Fragment 4/5 and wallop a high jump for secret bonus Data Fragment 5/5.
   - **Section 5 (Terminal Breach)**: Reach the glowing neon-green Exit Gate and escape to trigger the **Victory** screen!

---

## 🏗️ Architecture & Scripts

All gameplay scripts are located in `Assets/Scripts/` and isolated within `GlitchRun.asmdef` for clean, modular compilation:

```
Assets/
├── Art/
│   ├── Characters/Player/         # Teammate player art & PF_PlayerVisual prefab (preserved)
│   ├── Environments/Background/   # Teammate 4 city JPEG backgrounds & animation (preserved)
│   └── Sprites/                   # Cyberpunk platforms, lasers, fragments, exit gate
├── Prefabs/
│   ├── PF_Player.prefab           # Player with physics, GlitchAbility, & PlayerController
│   ├── PF_Platform.prefab         # Charcoal metallic platforms with BoxCollider2D
│   ├── PF_LaserHazard.prefab      # Lethal red laser beam with glitch detection
│   ├── PF_DataFragment.prefab     # Animated floating neon-green collectible
│   └── PF_ExitGate.prefab         # Unlocks when 3 fragments secured; triggers victory
├── Scenes/
│   ├── ArtTestScene.unity         # Teammates' original visual test scene (unmodified)
│   └── PrototypeScene.unity       # Dedicated fully-playable level
└── Scripts/
    ├── Core/
    │   └── GameManager.cs         # Simulation state, lives, fragments, level flow
    ├── Player/
    │   ├── PlayerController.cs    # 2D physics, ground check, coyote time, jump buffering
    │   └── GlitchAbility.cs       # 2.0s hazard phasing, visual glitch flicker, cooldown
    ├── Hazards/
    │   ├── LaserHazard.cs         # Red laser logic, constant/cycling modes, glitch bypass
    │   └── DeathZone.cs           # Bottom abyss trigger catching pit falls
    ├── Gameplay/
    │   ├── DataFragment.cs        # Bobbing/spinning neon collectible
    │   ├── ExitGate.cs            # Terminal state transition (locked amber -> unlocked green)
    │   └── CameraFollow2D.cs      # Smooth camera tracking, clamping, & city parallax
    ├── UI/
    │   └── GameHUD.cs             # Cyberpunk HUD, glitch meter, GameOver/Victory screens
    └── Editor/
        └── PrototypeSceneBuilder.cs # Automated scene & prefab generator utility
```

---

## 🎨 Visual Direction & Palette
- **Environments**: Charcoal / near-black metallic platforms (`#12141A`, `#1A1E28`) with neon edge highlights.
- **Hazards**: Intense neon-red laser beams (`#FF1144`, `#FF003C`) with pulsing electrical core.
- **Collectibles**: Neon-green cyber fragments (`#00FF66`, `#39FF14`) with rotational and bobbing animation.
- **Player**: Teammate's cyberpunk player sprite with pale-green accents and digital chromatic flickering during glitch phasing.
- **Background**: Teammate's animated 4-frame cyberpunk city skyline (`BG_City_Loop.anim`) scrolling with smooth parallax.
- **HUD & Screens**: High-contrast cyberpunk styling with real-time integrity gauge, data counter, glitch cooldown bar, and dedicated game-over / victory interfaces.

---

## 🛡️ Safety & Team Preservation Compliance
1. **Teammate Work Preserved**:
   - `Assets/Scenes/ArtTestScene.unity` was completely preserved without edits.
   - `Assets/Art/Characters/Player/` (`main_player.png`, `PF_PlayerVisual.prefab`) and all `.meta` files are intact.
   - `Assets/Art/Environments/Background/` (`BG_City_01` to `04`, `BG_City_Loop.anim`, `BG_City_Animated.controller`) are untouched.
2. **Branching**: Created dedicated branch `feature/playable-prototype` based on `feature/cyberpunk-art`.
3. **No External Plugin Bloat**: Pure Unity 6 standard libraries and package-included URP 2D / Input System. No heavy post-processing, 3D assets, or unneeded third-party plugins.

---

## ⚙️ Technical Notes & Known Limitations
- **Input System**: Built using the Unity Input System Package (`activeInputHandler: 1` in Project Settings), utilizing `UnityEngine.InputSystem.Keyboard` and `Gamepad`.
- **UI Dual-Mode**: `GameHUD` is equipped with an automatic Cyberpunk IMGUI fallback that renders high-contrast, responsive UI directly in `OnGUI()`. Even if uGUI Canvas components are not wired up, the HUD, GameOver, and Victory screens are 100% visible and interactive.
- **Re-Generation**: The entire scene and all prefabs can be rebuilt at any time via the Editor menu: **`Glitch Run > Build Prototype Scene and Prefabs`**.
