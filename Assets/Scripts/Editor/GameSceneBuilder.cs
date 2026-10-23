using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GlitchRun.Core;
using GlitchRun.Player;
using GlitchRun.Hazards;
using GlitchRun.Gameplay;
using GlitchRun.UI;
using GlitchRun.Audio;

namespace GlitchRun.EditorScripts
{
    public static class GameSceneBuilder
    {
        [MenuItem("Glitch Run/Build Complete Game (All Scenes & Prefabs)", priority = 10)]
        public static void BuildCompleteGame()
        {
            Debug.Log("[GlitchRun] Starting complete game construction...");

            EnsureDirectories();
            var prefabs = BuildPrefabs();
            BuildMainMenuScene();
            BuildGameplayScene(prefabs);
            BuildVictoryScene();
            BuildGameOverScene();
            RegisterBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GlitchRun] COMPLETE GAME CONSTRUCTED SUCCESSFULLY! All 4 scenes registered.");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { "Assets/Prefabs", "Assets/Scenes" };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
        }

        public struct PrefabCollection
        {
            public GameObject Player;
            public GameObject Platform;
            public GameObject Laser;
            public GameObject Drone;
            public GameObject GlitchBarrier;
            public GameObject Fragment;
            public GameObject Checkpoint;
            public GameObject ExitGate;
        }

        #region Prefab Construction
        public static PrefabCollection BuildPrefabs()
        {
            PrefabCollection p = new PrefabCollection();

            var platSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_platform.png");
            var laserSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_laser.png");
            var droneSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_drone.png");
            var barrierSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_glitch_barrier.png");
            var fragSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_fragment.png");
            var checkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_checkpoint.png");
            var exitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_exit_portal.png");
            var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/main_player.png");

            // 1. Platform
            var platObj = new GameObject("PF_Platform");
            var platSr = platObj.AddComponent<SpriteRenderer>();
            platSr.sprite = platSprite;
            platSr.sortingOrder = 1;
            var platCol = platObj.AddComponent<BoxCollider2D>();
            if (platSprite != null) platCol.size = platSprite.bounds.size;
            p.Platform = PrefabUtility.SaveAsPrefabAsset(platObj, "Assets/Prefabs/PF_Platform.prefab");
            UnityEngine.Object.DestroyImmediate(platObj);

            // 2. Laser Hazard
            var laserObj = new GameObject("PF_LaserHazard");
            var laserSr = laserObj.AddComponent<SpriteRenderer>();
            laserSr.sprite = laserSprite;
            laserSr.sortingOrder = 5;
            laserSr.color = new Color(1f, 0.1f, 0.25f, 0.95f);
            var laserCol = laserObj.AddComponent<BoxCollider2D>();
            laserCol.isTrigger = true;
            if (laserSprite != null) laserCol.size = laserSprite.bounds.size;
            laserObj.AddComponent<LaserHazard>();
            p.Laser = PrefabUtility.SaveAsPrefabAsset(laserObj, "Assets/Prefabs/PF_LaserHazard.prefab");
            UnityEngine.Object.DestroyImmediate(laserObj);

            // 3. Drone Hazard
            var droneObj = new GameObject("PF_DroneHazard");
            var droneSr = droneObj.AddComponent<SpriteRenderer>();
            droneSr.sprite = droneSprite;
            droneSr.sortingOrder = 6;
            var droneCol = droneObj.AddComponent<CircleCollider2D>();
            droneCol.isTrigger = true;
            droneCol.radius = 0.45f;
            droneObj.AddComponent<DroneHazard>();
            p.Drone = PrefabUtility.SaveAsPrefabAsset(droneObj, "Assets/Prefabs/PF_DroneHazard.prefab");
            UnityEngine.Object.DestroyImmediate(droneObj);

            // 4. Glitch Barrier
            var barObj = new GameObject("PF_GlitchBarrier");
            var barSr = barObj.AddComponent<SpriteRenderer>();
            barSr.sprite = barrierSprite;
            barSr.sortingOrder = 4;
            barSr.color = new Color(0.85f, 0.1f, 0.95f, 0.85f);
            var barCol = barObj.AddComponent<BoxCollider2D>();
            if (barrierSprite != null) barCol.size = barrierSprite.bounds.size;
            barObj.AddComponent<GlitchBarrier>();
            p.GlitchBarrier = PrefabUtility.SaveAsPrefabAsset(barObj, "Assets/Prefabs/PF_GlitchBarrier.prefab");
            UnityEngine.Object.DestroyImmediate(barObj);

            // 5. Data Fragment
            var fragObj = new GameObject("PF_DataFragment");
            fragObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            var fragSr = fragObj.AddComponent<SpriteRenderer>();
            fragSr.sprite = fragSprite;
            fragSr.sortingOrder = 6;
            var fragCol = fragObj.AddComponent<CircleCollider2D>();
            fragCol.isTrigger = true;
            fragCol.radius = 0.35f;
            fragObj.AddComponent<DataFragment>();
            p.Fragment = PrefabUtility.SaveAsPrefabAsset(fragObj, "Assets/Prefabs/PF_DataFragment.prefab");
            UnityEngine.Object.DestroyImmediate(fragObj);

            // 6. Checkpoint
            var cpObj = new GameObject("PF_Checkpoint");
            var cpSr = cpObj.AddComponent<SpriteRenderer>();
            cpSr.sprite = checkSprite;
            cpSr.sortingOrder = 3;
            var cpCol = cpObj.AddComponent<BoxCollider2D>();
            cpCol.isTrigger = true;
            if (checkSprite != null) cpCol.size = checkSprite.bounds.size;
            cpObj.AddComponent<Checkpoint>();
            p.Checkpoint = PrefabUtility.SaveAsPrefabAsset(cpObj, "Assets/Prefabs/PF_Checkpoint.prefab");
            UnityEngine.Object.DestroyImmediate(cpObj);

            // 7. Exit Gate
            var exitObj = new GameObject("PF_ExitGate");
            exitObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            var exitSr = exitObj.AddComponent<SpriteRenderer>();
            exitSr.sprite = exitSprite;
            exitSr.sortingOrder = 2;
            var exitCol = exitObj.AddComponent<BoxCollider2D>();
            exitCol.isTrigger = true;
            if (exitSprite != null) exitCol.size = exitSprite.bounds.size;
            exitObj.AddComponent<ExitGate>();
            p.ExitGate = PrefabUtility.SaveAsPrefabAsset(exitObj, "Assets/Prefabs/PF_ExitGate.prefab");
            UnityEngine.Object.DestroyImmediate(exitObj);

            // 8. Player Prefab
            var playerObj = new GameObject("PF_Player");
            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3.5f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var playerCol = playerObj.AddComponent<CapsuleCollider2D>();
            playerCol.size = new Vector2(0.85f, 1.8f);
            playerCol.offset = new Vector2(0f, 0.1f);

            var visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(playerObj.transform, false);
            visualObj.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
            var visualSr = visualObj.AddComponent<SpriteRenderer>();
            visualSr.sprite = playerSprite;
            visualSr.sortingOrder = 10;

            playerObj.AddComponent<GlitchAbility>();
            playerObj.AddComponent<PlayerController>();

            p.Player = PrefabUtility.SaveAsPrefabAsset(playerObj, "Assets/Prefabs/PF_Player.prefab");
            UnityEngine.Object.DestroyImmediate(playerObj);

            return p;
        }
        #endregion

        #region Scene Construction: MainMenu
        public static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            var audioType = System.Type.GetType("UnityEngine.AudioListener, UnityEngine.AudioModule") ?? System.Type.GetType("UnityEngine.AudioListener, UnityEngine");
            if (audioType != null) camObj.AddComponent(audioType);

            // Background
            var bgObj = new GameObject("BG_City");
            bgObj.transform.position = new Vector3(0f, 0f, 1f);
            bgObj.transform.localScale = new Vector3(2.8f, 2.8f, 1f);
            var bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Background/BG_City_01.jpeg");
            bgSr.sortingOrder = -10;

            // Audio Manager
            var audioObj = new GameObject("AudioManager");
            SetupAudioManager(audioObj);

            // UI Canvas
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Event System
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();

            // UI Elements
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Title Header
            var titleObj = CreateText(canvasObj.transform, "TitleText", "GLITCH RUN", font, 84, TextAnchor.MiddleCenter, new Color(0.0f, 1.0f, 0.85f, 1f), new Vector2(0, 220), new Vector2(800, 120));
            CreateText(canvasObj.transform, "SubtitleText", "BREAK THE SIMULATION", font, 36, TextAnchor.MiddleCenter, new Color(0.9f, 0.1f, 0.95f, 1f), new Vector2(0, 140), new Vector2(800, 60));

            // Menu Buttons Container
            var btnPlay = CreateButton(canvasObj.transform, "Btn_Play", "START SIMULATION", font, 28, new Vector2(0, 10), new Vector2(320, 60));
            var btnHow = CreateButton(canvasObj.transform, "Btn_HowToPlay", "HOW TO PLAY", font, 28, new Vector2(0, -70), new Vector2(320, 60));
            var btnSettings = CreateButton(canvasObj.transform, "Btn_Settings", "SYSTEM SETTINGS", font, 28, new Vector2(0, -150), new Vector2(320, 60));
            var btnQuit = CreateButton(canvasObj.transform, "Btn_Quit", "TERMINATE", font, 28, new Vector2(0, -230), new Vector2(320, 60));

            // How To Play Modal
            var howModal = CreatePanel(canvasObj.transform, "HowToPlayModal", new Vector2(0, 0), new Vector2(700, 480), new Color(0.05f, 0.08f, 0.12f, 0.95f));
            CreateText(howModal.transform, "HowTitle", "CYBER RUNNER DIRECTIVE", font, 34, TextAnchor.UpperCenter, new Color(0.0f, 1f, 0.85f, 1f), new Vector2(0, 180), new Vector2(600, 50));
            string controlsText = "CONTROLS:\n" +
                                  "• A / D or Arrow Keys: Move Left / Right\n" +
                                  "• Space / W / Up: Variable Jump (Coyote Time + Buffer)\n" +
                                  "• Left Shift: GLITCH PHASE (Phase through magenta barriers)\n" +
                                  "• Esc: Pause Simulation / Settings\n\n" +
                                  "OBJECTIVE:\n" +
                                  "• Collect at least 3 Data Fragments [◆] to unlock the Exit Gate\n" +
                                  "• Avoid red lasers and security drones\n" +
                                  "• Escape before simulation collapse!";
            CreateText(howModal.transform, "HowBody", controlsText, font, 22, TextAnchor.MiddleCenter, Color.white, new Vector2(0, 20), new Vector2(600, 240));
            var btnCloseHow = CreateButton(howModal.transform, "Btn_CloseHow", "DISMISS", font, 24, new Vector2(0, -180), new Vector2(200, 50));
            howModal.SetActive(false);

            // Settings Modal
            var settingsModal = CreatePanel(canvasObj.transform, "SettingsModal", new Vector2(0, 0), new Vector2(650, 450), new Color(0.05f, 0.08f, 0.12f, 0.95f));
            CreateText(settingsModal.transform, "SetTitle", "AUDIO & VISUAL CONFIG", font, 32, TextAnchor.UpperCenter, new Color(0.0f, 1f, 0.85f, 1f), new Vector2(0, 160), new Vector2(500, 50));
            var masterSlider = CreateSlider(settingsModal.transform, "MasterSlider", "MASTER VOL", font, new Vector2(0, 80));
            var musicSlider = CreateSlider(settingsModal.transform, "MusicSlider", "MUSIC VOL", font, new Vector2(0, 20));
            var sfxSlider = CreateSlider(settingsModal.transform, "SfxSlider", "SFX VOL", font, new Vector2(0, -40));
            var btnCloseSet = CreateButton(settingsModal.transform, "Btn_CloseSet", "SAVE & CLOSE", font, 24, new Vector2(0, -150), new Vector2(220, 50));
            settingsModal.SetActive(false);

            // MainMenuController setup
            var menuCtrl = canvasObj.AddComponent<MainMenuController>();
            var so = new SerializedObject(menuCtrl);
            so.FindProperty("playButton").objectReferenceValue = btnPlay.GetComponent<Button>();
            so.FindProperty("howToPlayButton").objectReferenceValue = btnHow.GetComponent<Button>();
            so.FindProperty("settingsButton").objectReferenceValue = btnSettings.GetComponent<Button>();
            so.FindProperty("quitButton").objectReferenceValue = btnQuit.GetComponent<Button>();
            so.FindProperty("howToPlayModal").objectReferenceValue = howModal;
            so.FindProperty("closeHowToPlayButton").objectReferenceValue = btnCloseHow.GetComponent<Button>();
            so.FindProperty("settingsModal").objectReferenceValue = settingsModal;
            so.FindProperty("masterSlider").objectReferenceValue = masterSlider;
            so.FindProperty("musicSlider").objectReferenceValue = musicSlider;
            so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("closeSettingsButton").objectReferenceValue = btnCloseSet.GetComponent<Button>();
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
            Debug.Log("[GlitchRun] MainMenu.unity created and saved.");
        }
        #endregion

        #region Scene Construction: Gameplay
        public static void BuildGameplayScene(PrefabCollection prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Animated City Background
            var bgObj = new GameObject("BG_City_Animated");
            bgObj.transform.position = new Vector3(15f, 2.5f, 1f);
            bgObj.transform.localScale = new Vector3(2.8f, 2.8f, 1f);
            var bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Background/BG_City_01.jpeg");
            bgSr.sortingOrder = -10;
            var bgAnim = bgObj.AddComponent<Animator>();
            bgAnim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Environments/Background/BG_City_Animated.controller");

            // 2. Audio Manager
            var audioObj = new GameObject("AudioManager");
            SetupAudioManager(audioObj);

            // 3. Player Instance
            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Player);
            playerInstance.name = "Player";
            playerInstance.transform.position = new Vector3(0f, 0f, 0f);

            // 4. Main Camera
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            var audioType = System.Type.GetType("UnityEngine.AudioListener, UnityEngine.AudioModule") ?? System.Type.GetType("UnityEngine.AudioListener, UnityEngine");
            if (audioType != null) camObj.AddComponent(audioType);

            var follow = camObj.AddComponent<CameraFollow2D>();
            var followSo = new SerializedObject(follow);
            followSo.FindProperty("target").objectReferenceValue = playerInstance.transform;
            followSo.FindProperty("backgroundTransform").objectReferenceValue = bgObj.transform;
            followSo.FindProperty("minBounds").vector2Value = new Vector2(-2f, -1f);
            followSo.FindProperty("maxBounds").vector2Value = new Vector2(68f, 8f);
            followSo.ApplyModifiedProperties();

            // 5. Game Manager
            var gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            // 6. Death Zone (covers entire level abyss)
            var deathObj = new GameObject("DeathZone");
            deathObj.transform.position = new Vector3(32f, -6f, 0f);
            var deathCol = deathObj.AddComponent<BoxCollider2D>();
            deathCol.isTrigger = true;
            deathCol.size = new Vector2(120f, 4f);
            deathObj.AddComponent<DeathZone>();

            // 7. Handcrafted Level Geometry
            // Section 1: Ground Zero & Movement Tutorial
            SpawnPlatform(prefabs.Platform, new Vector3(2f, -1.5f, 0f), new Vector3(6.0f, 1.0f, 1f), "Platform_Start");
            SpawnFragment(prefabs.Fragment, new Vector3(5.0f, 0.0f, 0f), "DataFragment_01");
            SpawnPlatform(prefabs.Platform, new Vector3(11.0f, -1.5f, 0f), new Vector3(3.5f, 1.0f, 1f), "Platform_Checkpoint1");
            SpawnCheckpoint(prefabs.Checkpoint, new Vector3(11.0f, -0.6f, 0f), "Checkpoint_01");

            // Section 2: Firewall Grid (Laser Timing & Glitch Barrier)
            SpawnPlatform(prefabs.Platform, new Vector3(18.0f, -1.5f, 0f), new Vector3(5.5f, 1.0f, 1f), "Platform_Section2");
            SpawnLaser(prefabs.Laser, new Vector3(16.5f, 0.2f, 0f), new Vector3(1.2f, 2.4f, 1f), false, "LaserHazard_Static_01");
            SpawnGlitchBarrier(prefabs.GlitchBarrier, new Vector3(19.5f, 0.3f, 0f), new Vector3(0.8f, 2.6f, 1f), "GlitchBarrier_01");
            SpawnFragment(prefabs.Fragment, new Vector3(20.5f, 0.0f, 0f), "DataFragment_02");

            // Section 3: Security Void (Ascending Platforms & Cycling Laser)
            SpawnPlatform(prefabs.Platform, new Vector3(26.5f, -0.5f, 0f), new Vector3(2.0f, 1.0f, 1f), "Platform_Step_A");
            SpawnPlatform(prefabs.Platform, new Vector3(31.5f, 1.0f, 0f), new Vector3(2.2f, 1.0f, 1f), "Platform_Step_B");
            SpawnLaser(prefabs.Laser, new Vector3(31.5f, 2.7f, 0f), new Vector3(1.2f, 2.2f, 1f), true, "LaserHazard_Cycling_02");
            SpawnFragment(prefabs.Fragment, new Vector3(31.5f, 4.2f, 0f), "DataFragment_03_ObjectiveMet");
            SpawnPlatform(prefabs.Platform, new Vector3(36.5f, 0.0f, 0f), new Vector3(2.0f, 1.0f, 1f), "Platform_Step_C");
            SpawnCheckpoint(prefabs.Checkpoint, new Vector3(36.5f, 0.9f, 0f), "Checkpoint_02");

            // Section 4: Drone Corridor (Patrolling Drone + Dual Lasers + Secret Ledge)
            SpawnPlatform(prefabs.Platform, new Vector3(45.5f, -1.5f, 0f), new Vector3(6.5f, 1.0f, 1f), "Platform_Corridor");
            SpawnDrone(prefabs.Drone, new Vector3(44.0f, 0.5f, 0f), new Vector3(43f, 0.5f, 0f), new Vector3(48f, 0.5f, 0f), "Drone_Patrol_01");
            SpawnLaser(prefabs.Laser, new Vector3(47.5f, 0.2f, 0f), new Vector3(1.2f, 2.4f, 1f), true, "LaserHazard_Cycling_03");
            SpawnFragment(prefabs.Fragment, new Vector3(45.5f, 0.0f, 0f), "DataFragment_04");

            // High secret bonus platform
            SpawnPlatform(prefabs.Platform, new Vector3(45.5f, 2.8f, 0f), new Vector3(1.8f, 0.8f, 1f), "Platform_Secret");
            SpawnFragment(prefabs.Fragment, new Vector3(45.5f, 4.0f, 0f), "DataFragment_05_Bonus");

            // Section 5: Terminal Breach (Exit Gate)
            SpawnPlatform(prefabs.Platform, new Vector3(57.0f, -1.0f, 0f), new Vector3(5.0f, 1.2f, 1f), "Platform_Exit");
            GameObject exitGate = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.ExitGate);
            exitGate.name = "ExitGate";
            exitGate.transform.position = new Vector3(58.5f, 0.6f, 0f);

            // 8. uGUI HUD Canvas Construction
            BuildGameplayHUD(camObj);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Gameplay.unity");
            Debug.Log("[GlitchRun] Gameplay.unity created and saved.");
        }

        private static void BuildGameplayHUD(GameObject camObj)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            var heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/spr_heart.png");
            var meterSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/spr_meter_fill.png");

            var canvasObj = new GameObject("HUDCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Event System
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();

            // Top-left: Hearts (Integrity)
            var livesPanel = CreatePanel(canvasObj.transform, "LivesPanel", new Vector2(-750, 480), new Vector2(250, 60), new Color(0.08f, 0.1f, 0.16f, 0.7f));
            CreateText(livesPanel.transform, "LivesLabel", "INTEGRITY:", font, 20, TextAnchor.MiddleLeft, new Color(0.0f, 1f, 0.85f, 1f), new Vector2(-50, 0), new Vector2(120, 40));
            Image[] hearts = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var hObj = new GameObject($"Heart_{i + 1}");
                hObj.transform.SetParent(livesPanel.transform, false);
                var rt = hObj.AddComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(30 + i * 36, 0);
                rt.sizeDelta = new Vector2(30, 30);
                var img = hObj.AddComponent<Image>();
                img.sprite = heartSprite;
                img.color = new Color(0.0f, 1f, 0.85f, 1f);
                hearts[i] = img;
            }

            // Top-center: Score & Time
            var infoPanel = CreatePanel(canvasObj.transform, "InfoPanel", new Vector2(0, 480), new Vector2(400, 60), new Color(0.08f, 0.1f, 0.16f, 0.7f));
            var scoreTxt = CreateText(infoPanel.transform, "ScoreText", "SCORE: 00000", font, 24, TextAnchor.MiddleLeft, Color.white, new Vector2(-80, 0), new Vector2(200, 40));
            var timerTxt = CreateText(infoPanel.transform, "TimerText", "TIME: 00:00", font, 24, TextAnchor.MiddleRight, new Color(0.9f, 0.1f, 0.95f, 1f), new Vector2(80, 0), new Vector2(180, 40));

            // Top-right: Data Fragments
            var fragPanel = CreatePanel(canvasObj.transform, "FragPanel", new Vector2(750, 480), new Vector2(240, 60), new Color(0.08f, 0.1f, 0.16f, 0.7f));
            var fragTxt = CreateText(fragPanel.transform, "FragText", "DATA: 0 / 3", font, 22, TextAnchor.MiddleCenter, new Color(0.1f, 1f, 0.6f, 1f), new Vector2(0, 0), new Vector2(200, 40));

            // Bottom-center: Glitch Meter
            var glitchPanel = CreatePanel(canvasObj.transform, "GlitchPanel", new Vector2(0, -480), new Vector2(420, 65), new Color(0.08f, 0.1f, 0.16f, 0.8f));
            var glitchStatusTxt = CreateText(glitchPanel.transform, "GlitchStatus", "GLITCH READY [SHIFT]", font, 18, TextAnchor.MiddleCenter, new Color(0.0f, 1f, 0.85f, 1f), new Vector2(0, 15), new Vector2(380, 25));

            var meterBg = new GameObject("MeterFill");
            meterBg.transform.SetParent(glitchPanel.transform, false);
            var meterRt = meterBg.AddComponent<RectTransform>();
            meterRt.anchoredPosition = new Vector2(0, -12);
            meterRt.sizeDelta = new Vector2(380, 14);
            var meterImg = meterBg.AddComponent<Image>();
            meterImg.sprite = meterSprite;
            meterImg.type = Image.Type.Filled;
            meterImg.fillMethod = Image.FillMethod.Horizontal;
            meterImg.fillAmount = 1f;

            // Pause Overlay
            var pausePanel = CreatePanel(canvasObj.transform, "PauseOverlay", Vector2.zero, new Vector2(500, 400), new Color(0.05f, 0.08f, 0.14f, 0.95f));
            CreateText(pausePanel.transform, "PauseTitle", "SIMULATION PAUSED", font, 36, TextAnchor.UpperCenter, new Color(0.0f, 1f, 0.85f, 1f), new Vector2(0, 140), new Vector2(400, 50));
            var btnResume = CreateButton(pausePanel.transform, "Btn_Resume", "RESUME", font, 24, new Vector2(0, 60), new Vector2(260, 50));
            var btnRestart = CreateButton(pausePanel.transform, "Btn_Restart", "RESTART LEVEL", font, 24, new Vector2(0, -5), new Vector2(260, 50));
            var btnSettings = CreateButton(pausePanel.transform, "Btn_Settings", "SETTINGS", font, 24, new Vector2(0, -70), new Vector2(260, 50));
            var btnMenu = CreateButton(pausePanel.transform, "Btn_Menu", "MAIN MENU", font, 24, new Vector2(0, -135), new Vector2(260, 50));
            pausePanel.SetActive(false);

            // Settings Overlay
            var settingsPanel = CreatePanel(canvasObj.transform, "SettingsOverlay", Vector2.zero, new Vector2(600, 420), new Color(0.05f, 0.08f, 0.14f, 0.98f));
            CreateText(settingsPanel.transform, "SetTitle", "SYSTEM SETTINGS", font, 32, TextAnchor.UpperCenter, new Color(0.0f, 1f, 0.85f, 1f), new Vector2(0, 150), new Vector2(400, 50));
            var masterSlider = CreateSlider(settingsPanel.transform, "MasterSlider", "MASTER", font, new Vector2(0, 70));
            var musicSlider = CreateSlider(settingsPanel.transform, "MusicSlider", "MUSIC", font, new Vector2(0, 10));
            var sfxSlider = CreateSlider(settingsPanel.transform, "SfxSlider", "SFX", font, new Vector2(0, -50));
            var btnCloseSet = CreateButton(settingsPanel.transform, "Btn_CloseSet", "CLOSE", font, 22, new Vector2(0, -140), new Vector2(200, 45));
            settingsPanel.SetActive(false);

            // GameHUD component
            var hud = canvasObj.AddComponent<GameHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("scoreText").objectReferenceValue = scoreTxt.GetComponent<Text>();
            so.FindProperty("fragmentsText").objectReferenceValue = fragTxt.GetComponent<Text>();
            so.FindProperty("timerText").objectReferenceValue = timerTxt.GetComponent<Text>();
            so.FindProperty("glitchMeterFill").objectReferenceValue = meterImg;
            so.FindProperty("glitchStatusText").objectReferenceValue = glitchStatusTxt.GetComponent<Text>();

            var heartsProp = so.FindProperty("lifeHearts");
            heartsProp.arraySize = 3;
            for (int i = 0; i < 3; i++) heartsProp.GetArrayElementAtIndex(i).objectReferenceValue = hearts[i];

            so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            so.FindProperty("resumeButton").objectReferenceValue = btnResume.GetComponent<Button>();
            so.FindProperty("restartButton").objectReferenceValue = btnRestart.GetComponent<Button>();
            so.FindProperty("settingsButton").objectReferenceValue = btnSettings.GetComponent<Button>();
            so.FindProperty("mainMenuButton").objectReferenceValue = btnMenu.GetComponent<Button>();

            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("masterSlider").objectReferenceValue = masterSlider;
            so.FindProperty("musicSlider").objectReferenceValue = musicSlider;
            so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("closeSettingsButton").objectReferenceValue = btnCloseSet.GetComponent<Button>();
            so.ApplyModifiedProperties();
        }
        #endregion

        #region Scene Construction: Victory & GameOver
        public static void BuildVictoryScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.04f, 0.08f, 0.06f, 1f); // Cyber green tint

            // Audio Manager
            var audioObj = new GameObject("AudioManager");
            SetupAudioManager(audioObj);

            // Canvas
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var panel = CreatePanel(canvasObj.transform, "VictoryPanel", Vector2.zero, new Vector2(800, 600), new Color(0.06f, 0.1f, 0.14f, 0.95f));
            var title = CreateText(panel.transform, "Title", "SIMULATION BREACHED", font, 52, TextAnchor.UpperCenter, new Color(0.0f, 1.0f, 0.85f, 1f), new Vector2(0, 220), new Vector2(700, 70));
            CreateText(panel.transform, "Sub", "TERMINAL ESCAPE SUCCESSFUL", font, 24, TextAnchor.UpperCenter, new Color(0.9f, 0.1f, 0.95f, 1f), new Vector2(0, 160), new Vector2(600, 40));

            var scoreTxt = CreateText(panel.transform, "Score", "SCORE: 03500", font, 32, TextAnchor.MiddleCenter, Color.white, new Vector2(0, 80), new Vector2(500, 45));
            var fragTxt = CreateText(panel.transform, "Frags", "FRAGMENTS: 3 / 3", font, 26, TextAnchor.MiddleCenter, new Color(0.1f, 1f, 0.6f, 1f), new Vector2(0, 25), new Vector2(500, 40));
            var timeTxt = CreateText(panel.transform, "Time", "BREACH TIME: 01:24", font, 26, TextAnchor.MiddleCenter, new Color(0.0f, 1f, 0.85f, 1f), new Vector2(0, -25), new Vector2(500, 40));
            var rankTxt = CreateText(panel.transform, "Rank", "RATING: S+ [SIMULATION GOD]", font, 30, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.0f, 1f), new Vector2(0, -80), new Vector2(600, 45));

            var btnRetry = CreateButton(panel.transform, "Btn_PlayAgain", "PLAY AGAIN", font, 26, new Vector2(-150, -180), new Vector2(240, 55));
            var btnMenu = CreateButton(panel.transform, "Btn_Menu", "MAIN MENU", font, 26, new Vector2(150, -180), new Vector2(240, 55));

            var ctrl = canvasObj.AddComponent<EndGameController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("isVictoryScreen").boolValue = true;
            so.FindProperty("titleText").objectReferenceValue = title.GetComponent<Text>();
            so.FindProperty("scoreText").objectReferenceValue = scoreTxt.GetComponent<Text>();
            so.FindProperty("fragmentsText").objectReferenceValue = fragTxt.GetComponent<Text>();
            so.FindProperty("timeText").objectReferenceValue = timeTxt.GetComponent<Text>();
            so.FindProperty("rankText").objectReferenceValue = rankTxt.GetComponent<Text>();
            so.FindProperty("retryButton").objectReferenceValue = btnRetry.GetComponent<Button>();
            so.FindProperty("mainMenuButton").objectReferenceValue = btnMenu.GetComponent<Button>();
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Victory.unity");
            Debug.Log("[GlitchRun] Victory.unity created and saved.");
        }

        public static void BuildGameOverScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.08f, 0.03f, 0.04f, 1f); // Cyber red void

            var audioObj = new GameObject("AudioManager");
            SetupAudioManager(audioObj);

            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var panel = CreatePanel(canvasObj.transform, "GameOverPanel", Vector2.zero, new Vector2(800, 550), new Color(0.12f, 0.05f, 0.06f, 0.95f));
            var title = CreateText(panel.transform, "Title", "NEURAL FLATLINE", font, 52, TextAnchor.UpperCenter, new Color(1f, 0.1f, 0.25f, 1f), new Vector2(0, 190), new Vector2(700, 70));
            CreateText(panel.transform, "Sub", "SIMULATION TERMINATED — RUNNER COMPROMISED", font, 22, TextAnchor.UpperCenter, new Color(0.9f, 0.5f, 0.1f, 1f), new Vector2(0, 130), new Vector2(650, 40));

            var scoreTxt = CreateText(panel.transform, "Score", "FINAL SCORE: 00750", font, 30, TextAnchor.MiddleCenter, Color.white, new Vector2(0, 50), new Vector2(500, 45));
            var fragTxt = CreateText(panel.transform, "Frags", "DATA RECOVERED: 1 / 3", font, 24, TextAnchor.MiddleCenter, new Color(0.1f, 1f, 0.6f, 1f), new Vector2(0, -5), new Vector2(500, 40));
            var timeTxt = CreateText(panel.transform, "Time", "ELAPSED TIME: 00:48", font, 24, TextAnchor.MiddleCenter, new Color(0.0f, 1f, 0.85f, 1f), new Vector2(0, -55), new Vector2(500, 40));

            var btnRetry = CreateButton(panel.transform, "Btn_Restart", "RESTART SIMULATION", font, 24, new Vector2(-150, -160), new Vector2(250, 55));
            var btnMenu = CreateButton(panel.transform, "Btn_Menu", "MAIN MENU", font, 24, new Vector2(150, -160), new Vector2(250, 55));

            var ctrl = canvasObj.AddComponent<EndGameController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("isVictoryScreen").boolValue = false;
            so.FindProperty("titleText").objectReferenceValue = title.GetComponent<Text>();
            so.FindProperty("scoreText").objectReferenceValue = scoreTxt.GetComponent<Text>();
            so.FindProperty("fragmentsText").objectReferenceValue = fragTxt.GetComponent<Text>();
            so.FindProperty("timeText").objectReferenceValue = timeTxt.GetComponent<Text>();
            so.FindProperty("retryButton").objectReferenceValue = btnRetry.GetComponent<Button>();
            so.FindProperty("mainMenuButton").objectReferenceValue = btnMenu.GetComponent<Button>();
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameOver.unity");
            Debug.Log("[GlitchRun] GameOver.unity created and saved.");
        }
        #endregion

        #region Helpers
        private static GameObject SpawnPlatform(GameObject prefab, Vector3 pos, Vector3 scale, string name)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            return obj;
        }

        private static GameObject SpawnLaser(GameObject prefab, Vector3 pos, Vector3 scale, bool cycling, string name)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;

            if (cycling)
            {
                var hazard = obj.GetComponent<LaserHazard>();
                if (hazard != null)
                {
                    var so = new SerializedObject(hazard);
                    so.FindProperty("isCycling").boolValue = true;
                    so.FindProperty("activeTime").floatValue = 2.2f;
                    so.FindProperty("inactiveTime").floatValue = 1.6f;
                    so.ApplyModifiedProperties();
                }
            }
            return obj;
        }

        private static GameObject SpawnDrone(GameObject prefab, Vector3 pos, Vector3 wpA, Vector3 wpB, string name)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;

            var drone = obj.GetComponent<DroneHazard>();
            if (drone != null)
            {
                drone.SetWaypoints(wpA, wpB);
            }
            return obj;
        }

        private static GameObject SpawnGlitchBarrier(GameObject prefab, Vector3 pos, Vector3 scale, string name)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            return obj;
        }

        private static GameObject SpawnFragment(GameObject prefab, Vector3 pos, string name)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;
            return obj;
        }

        private static GameObject SpawnCheckpoint(GameObject prefab, Vector3 pos, string name)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;
            return obj;
        }

        private static GameObject CreateText(Transform parent, string name, string text, Font font, int fontSize, TextAnchor alignment, Color color, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            return obj;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Font font, int fontSize, Vector2 pos, Vector2 size)
        {
            var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/spr_ui_button.png");

            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = obj.AddComponent<Image>();
            img.sprite = btnSprite;
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            var btn = obj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.0f, 1f, 0.85f, 1f);
            colors.pressedColor = new Color(0.9f, 0.1f, 0.95f, 1f);
            btn.colors = colors;

            CreateText(obj.transform, "Label", label, font, fontSize, TextAnchor.MiddleCenter, Color.white, Vector2.zero, size);
            return obj;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static Slider CreateSlider(Transform parent, string name, string label, Font font, Vector2 pos)
        {
            var container = new GameObject(name + "_Container");
            container.transform.SetParent(parent, false);
            var contRt = container.AddComponent<RectTransform>();
            contRt.anchoredPosition = pos;
            contRt.sizeDelta = new Vector2(400, 40);

            CreateText(container.transform, "Label", label, font, 20, TextAnchor.MiddleLeft, Color.white, new Vector2(-120, 0), new Vector2(140, 30));

            var sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(container.transform, false);
            var sRt = sliderObj.AddComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(60, 0);
            sRt.sizeDelta = new Vector2(220, 20);

            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.2f, 0.28f, 0.8f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.AddComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;
            var fImg = fill.AddComponent<Image>();
            fImg.color = new Color(0.0f, 1f, 0.85f, 1f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fRt;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static void SetupAudioManager(GameObject audioObj)
        {
            var am = audioObj.AddComponent<AudioManager>();
            var so = new SerializedObject(am);
            so.FindProperty("bgmMenu").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/Music/bgm_menu.wav");
            so.FindProperty("bgmGameplay").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/Music/bgm_gameplay.wav");
            so.FindProperty("sfxJump").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/sfx_jump.wav");
            so.FindProperty("sfxPickup").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/sfx_pickup.wav");
            so.FindProperty("sfxGlitchOn").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/sfx_glitch_on.wav");
            so.FindProperty("sfxGlitchOff").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/sfx_glitch_off.wav");
            so.FindProperty("sfxDamage").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/sfx_damage.wav");
            so.FindProperty("sfxDeath").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/sfx_death.wav");
            so.FindProperty("sfxCheckpoint").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/sfx_checkpoint.wav");
            so.FindProperty("sfxVictory").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/sfx_victory.wav");
            so.FindProperty("sfxClick").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/sfx_ui_click.wav");
            so.ApplyModifiedProperties();
        }

        public static void RegisterBuildSettings()
        {
            string[] scenePaths = {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Gameplay.unity",
                "Assets/Scenes/Victory.unity",
                "Assets/Scenes/GameOver.unity"
            };

            var buildScenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }
            EditorBuildSettings.scenes = buildScenes;
            Debug.Log("[GlitchRun] Registered all 4 scenes in EditorBuildSettings (MainMenu index 0).");
        }
        #endregion
    }
}
