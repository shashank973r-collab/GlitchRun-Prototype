# GLITCH RUN — BREAK THE SIMULATION
**Complete, Playable 2D Cyberpunk Platformer**

*Engine: Unity 6 (`6000.6.7f1`) | Render Pipeline: URP 2D | Input: Unity Input System*  
*Target: Windows Standalone x64 / Live College Gameathon Demo*

---

## 🎮 Game Overview & Narrative
In **Glitch Run: Break the Simulation**, you awaken inside a crumbling, corrupted AI mainframe. High-security laser firewalls block escape corridors, patrolling reconnaissance drones sweep the platforms, and lethal gaps drop into the infinite simulation void.

Your objective:
1. Traverse five high-tension simulation sectors.
2. Collect at least **3 Data Fragments** (`[◆]`) scattered throughout the mainframe.
3. Master your signature **Glitch Phase** (`Left Shift`) to pass harmlessly through holographic magenta barriers.
4. Reach the glowing **Terminal Breach Exit Gate** to escape the simulation before total neural collapse!

---

## 🕹️ Controls

| Action | Keyboard | Gamepad |
| :--- | :--- | :--- |
| **Move Left / Right** | `A` / `D` or `←` / `→` | Left Analog Stick / D-Pad |
| **Jump** (Variable height, coyote time) | `Space`, `W`, or `↑` | `Button South` (A / ✕) |
| **Glitch Phase** (Phase through barriers) | `Left Shift` or `Right Shift` | `Right Shoulder` / `Right Trigger` |
| **Pause / Settings** | `Escape` or `P` | `Start` / Menu Button |
| **Quick Restart** (Game Over / Victory) | `R` | `Button North` (Y / △) |

> ⚡ **Glitch Phase Mechanics**: Pressing `Left Shift` grants **2.0 seconds** of phase shifting with an animated energy meter and chromatic visual distortion. While phasing, you can pass straight through magenta **Glitch Barriers**. The ability includes safe-exit boundary detection ensuring you never get trapped inside solid architecture when the phase expires. Note: Glitch Phase does not grant immunity to lethal red lasers or patrolling security drones.

---

## 🚀 How to Launch & Play

### In Unity Editor
1. Open the project in **Unity 6 (`6000.6.7f1`)**.
2. Open `Assets/Scenes/MainMenu.unity`.
3. Press **▶ Play** in the Unity toolbar.
4. Click **START SIMULATION** to breach into Zone 1!

### Scene Progression Loop
- **MainMenu (`Assets/Scenes/MainMenu.unity`)**: Parallax animated cyberpunk skyline, stylized neon title, Play, How To Play guide, Volume & Display Settings, and Quit.
- **Gameplay (`Assets/Scenes/Gameplay.unity`)**:
  - **Zone 1 (Cyber Alley)**: Movement and jump tutorial. Collect Fragment 1/5. Checkpoint 1.
  - **Zone 2 (Firewall Grid)**: Timed laser timings and holographic magenta Glitch Barrier. Collect Fragment 2/5.
  - **Zone 3 (Security Void)**: Precision floating ledges over the abyss with cycling laser defense. Collect Fragment 3/5 (*Exit Unlocked!*). Checkpoint 2.
  - **Zone 4 (Drone Corridor)**: Patrolling security drones, dual lasers, and secret elevated ledge with bonus Fragment 5/5.
  - **Zone 5 (Terminal Breach)**: Terminal exit portal that activates when 3 fragments are secured, transitioning to Victory!
- **Victory (`Assets/Scenes/Victory.unity`)**: Live breach time, fragments gathered, score tally, performance rating (`S+`, `A`, `B`, `C`), Play Again, and Main Menu buttons.
- **GameOver (`Assets/Scenes/GameOver.unity`)**: Neural flatline screen with quick restart and main menu options.

---

## 🏗️ Architecture & Scripts

All scripts are cleanly isolated under `GlitchRun.asmdef` (Runtime) and `GlitchRun.Editor.asmdef` (Editor):

```
Assets/
├── Art/
│   ├── Characters/Player/       # Teammate player art & animations (preserved)
│   ├── Environments/Background/ # Teammate 4 city JPEG backgrounds & animation (preserved)
│   ├── Sprites/                 # Platforms, lasers, drones, glitch barriers, fragments, checkpoints
│   ├── UI/                      # Cyberpunk hearts, button frames, energy meters
│   └── Decor/                   # Holographic warning street decals
├── Audio/
│   ├── Music/                   # Synthesized 16-bit cyber-synth BGM loops (gameplay & menu)
│   └── SFX/                     # Procedurally synthesized jump, pickup, glitch, damage, death, victory SFX
├── Prefabs/
│   ├── PF_Player.prefab         # Rigidbody2D player controller with GlitchAbility & i-frames
│   ├── PF_Platform.prefab       # Charcoal metallic platforms with BoxCollider2D
│   ├── PF_LaserHazard.prefab    # Continuous & cycling red laser beams
│   ├── PF_DroneHazard.prefab    # Ping-pong patrolling security recon bot
│   ├── PF_GlitchBarrier.prefab  # Holographic phase barrier
│   ├── PF_DataFragment.prefab   # Floating, bobbing, rotating collectible
│   ├── PF_Checkpoint.prefab     # Interactive respawn terminal beacon
│   └── PF_ExitGate.prefab       # Objective-locked level escape portal
├── Resources/Audio/             # Runtime audio clips for zero-dependency loading
├── Scenes/
│   ├── MainMenu.unity           # Index 0 in Build Settings
│   ├── Gameplay.unity           # Index 1 in Build Settings
│   ├── Victory.unity            # Index 2 in Build Settings
│   └── GameOver.unity           # Index 3 in Build Settings
├── Scripts/
│   ├── Core/GameManager.cs      # Authoritative state machine, lives, score, level progression
│   ├── Audio/AudioManager.cs    # Persistent BGM/SFX manager with PlayerPrefs volume persistence
│   ├── Player/                  # PlayerController.cs, GlitchAbility.cs
│   ├── Hazards/                 # LaserHazard.cs, DroneHazard.cs, GlitchBarrier.cs, DeathZone.cs
│   ├── Gameplay/                # Checkpoint.cs, DataFragment.cs, ExitGate.cs, CameraFollow2D.cs
│   ├── UI/                      # GameHUD.cs (uGUI), MainMenuController.cs, EndGameController.cs
│   └── Editor/                  # GameSceneBuilder.cs, LevelReachabilityValidator.cs, AssetGenerator.cs
└── Tests/Editor/                # Automated EditMode unit tests (NUnit)
```

---

## 🛠️ Automated Scene Re-Generation & Verification

The entire game can be rebuilt and validated automatically via Unity Editor menus or headless CLI:

- **Rebuild Entire Game**: Menu `Glitch Run > Build Complete Game (All Scenes & Prefabs)`  
  CLI: `Unity.exe -batchmode -nographics -quit -projectPath "." -executeMethod GlitchRun.EditorScripts.GameSceneBuilder.BuildCompleteGame`
- **Rebuild Art & Audio**: Menu `Glitch Run > Generate All Art & Audio Assets`  
  CLI: `Unity.exe -batchmode -nographics -quit -projectPath "." -executeMethod GlitchRun.EditorScripts.AssetGenerator.GenerateAllAssets`
- **Verify Jump Reachability**: Menu `Glitch Run > Verify Level Jump Reachability`  
  CLI: `Unity.exe -batchmode -nographics -quit -projectPath "." -executeMethod GlitchRun.EditorScripts.LevelReachabilityValidator.VerifyLevelReachability`
- **Run Automated Unit Tests**:  
  CLI: `Unity.exe -batchmode -nographics -runTests -testPlatform EditMode -testResults results.xml -projectPath "."`

---

## 📦 Building Standalone Windows Executable (.exe)

To produce a production Windows build:
1. In Unity, open **File > Build Profiles** (or **Build Settings**).
2. Ensure `MainMenu`, `Gameplay`, `Victory`, and `GameOver` are checked (indices 0 to 3).
3. Select **Windows, Mac, Linux** > Architecture: **x86_64**.
4. Click **Build** and choose an output directory (e.g. `Builds/GlitchRun_v1.0/`).
