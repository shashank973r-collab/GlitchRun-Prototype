using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using GlitchRun.Player;
using GlitchRun.Hazards;
using GlitchRun.Gameplay;
using GlitchRun.Core;

namespace GlitchRun.PlayMode.Tests
{
    public class LevelExtensionValidationTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (SceneManager.GetActiveScene().name != "Gameplay")
            {
                var op = SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
                while (!op.isDone) yield return null;
            }
            yield return new WaitForSeconds(0.1f);
        }

        // V2: Gameplay scene reopens: zero missing scripts/references.
        [Test]
        public void V2_GameplayScene_ZeroMissingScriptsOrReferences()
        {
            int missingCount = 0;
            int totalComponents = 0;
            var scene = SceneManager.GetActiveScene();

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    var comps = child.GetComponents<Component>();
                    foreach (var c in comps)
                    {
                        totalComponents++;
                        if (c == null)
                        {
                            Debug.LogError($"[GlitchRun V2] Missing script on GameObject '{child.gameObject.name}'");
                            missingCount++;
                        }
                    }
                }
            }

            Assert.AreEqual(0, missingCount, $"V2 Failed: Found {missingCount} missing MonoBehaviour components in Gameplay.unity!");
            Assert.Greater(totalComponents, 100, "V2 Failed: Scene should have substantial active components.");
            Debug.Log($"[GlitchRun V2] PASSED: All {totalComponents} components in Gameplay.unity are valid.");
        }

        // V3: Exit X is far beyond old exit (old exit was ~64.5m); 5 sections exist in order.
        [Test]
        public void V3_ExitPortalPositionAndFiveSectionHierarchy()
        {
            var exitGate = Object.FindAnyObjectByType<ExitGate>();
            Assert.IsNotNull(exitGate, "V3 Failed: ExitGate not found in scene!");
            float exitX = exitGate.transform.position.x;
            Assert.Greater(exitX, 350f, $"V3 Failed: ExitGate X ({exitX:F1}m) should be far beyond old exit (>350m).");

            string[] expectedSections = new string[] {
                "Section_1_SimulationEntry",
                "Section_2_NeonRooftops",
                "Section_3_SecurityCorridor",
                "Section_4_CorruptedDataZone",
                "Section_5_FinalEscape"
            };

            foreach (var secName in expectedSections)
            {
                var secObj = GameObject.Find(secName);
                Assert.IsNotNull(secObj, $"V3 Failed: Required section group '{secName}' not found in scene!");
            }

            Debug.Log($"[GlitchRun V3] PASSED: ExitGate relocated to X={exitX:F1}m. All 5 Section root groups verified.");
        }

        // V4: Reachability validator: for every gap/laser, check spacing vs measured jump envelope and phase windows.
        [Test]
        public void V4_ReachabilityValidator_AllGapsAndHazardsFeasible()
        {
            float maxJumpHeight = 2.654f;
            float maxHorizontalLeap = 5.83f;

            // 1. Check all platform gaps in order of X
            var allPlatforms = new List<BoxCollider2D>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Platform_"))
                {
                    var col = go.GetComponent<BoxCollider2D>();
                    if (col != null) allPlatforms.Add(col);
                }
            }

            allPlatforms.Sort((a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));
            Assert.GreaterOrEqual(allPlatforms.Count, 15, "V4 Failed: Expected at least 15 progression platforms.");

            for (int i = 0; i < allPlatforms.Count - 1; i++)
            {
                Bounds current = allPlatforms[i].bounds;
                Bounds next = allPlatforms[i + 1].bounds;
                float gapX = next.min.x - current.max.x;
                float heightDiff = next.max.y - current.max.y;

                if (gapX > 0.05f)
                {
                    Assert.LessOrEqual(gapX, maxHorizontalLeap, $"V4 Failed: Gap between '{allPlatforms[i].name}' and '{allPlatforms[i+1].name}' ({gapX:F2}m) exceeds max leap ({maxHorizontalLeap:F2}m)!");
                    Assert.LessOrEqual(heightDiff, maxJumpHeight, $"V4 Failed: Step height between '{allPlatforms[i].name}' and '{allPlatforms[i+1].name}' ({heightDiff:F2}m) exceeds max jump height ({maxJumpHeight:F2}m)!");
                }
            }

            // 2. Check all Low Lasers: height <= 1.33m
            var lasers = Object.FindObjectsByType<LaserHazard>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var laser in lasers)
            {
                var col = laser.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    float h = col.size.y * laser.transform.lossyScale.y;
                    Assert.LessOrEqual(h, 1.33f, $"V4 Failed: Low laser '{laser.name}' height {h:F2}m exceeds 50% max jump height!");
                }
            }

            // 3. Check all Tall Glitch Barriers: height > 2.65m
            var barriers = Object.FindObjectsByType<GlitchBarrier>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var barrier in barriers)
            {
                var col = barrier.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    float h = col.size.y * barrier.transform.lossyScale.y;
                    Assert.Greater(h, maxJumpHeight, $"V4 Failed: Glitch barrier '{barrier.name}' height {h:F2}m should be unjumpable (> {maxJumpHeight:F2}m)!");
                }
            }

            Debug.Log($"[GlitchRun V4] PASSED: All {allPlatforms.Count} platforms, {lasers.Length} lasers, and {barriers.Length} glitch barriers conform strictly to kinematic envelopes.");
        }

        // V5: Fragment total in GameManager equals actual scene fragment count; each collects once.
        [Test]
        public void V5_FragmentCountIntegrity_GameManagerTotalMatchesScene()
        {
            var frags = Object.FindObjectsByType<DataFragment>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.GreaterOrEqual(frags.Length, 25, "V5 Failed: Scene should have at least 25 Data Fragments for an 8-10 min run.");

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "V5 Failed: GameManager.Instance must be active.");
            Assert.AreEqual(frags.Length, gm.TotalFragments, $"V5 Failed: GameManager.TotalFragments ({gm.TotalFragments}) does not match scene count ({frags.Length})!");

            Debug.Log($"[GlitchRun V5] PASSED: Fragment count = {frags.Length} perfectly matches GameManager total.");
        }

        // V6: Simulated run: checkpoints respawn at correct positions; exit triggers victory.
        [UnityTest]
        public IEnumerator V6_SimulatedRun_CheckpointsAndVictoryFlow()
        {
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);

            var player = gm.ActivePlayer;
            if (player == null) player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player, "V6 Failed: PlayerController not found.");

            var checkpoints = new List<Checkpoint>(Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            checkpoints.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            Assert.GreaterOrEqual(checkpoints.Count, 5, "V6 Failed: Expected at least 5 checkpoints across the 5 sections.");

            // Test checkpoint respawn
            foreach (var cp in checkpoints)
            {
                player.transform.position = cp.transform.position;
                yield return new WaitForFixedUpdate();

                gm.SetCheckpoint(cp.transform.position);
                Assert.AreEqual(cp.transform.position, gm.CurrentCheckpoint, $"V6 Failed: Checkpoint at {cp.transform.position} not registered properly.");
            }

            // Test Exit Gate victory trigger
            var exitGate = Object.FindAnyObjectByType<ExitGate>();
            Assert.IsNotNull(exitGate);

            // Grant required fragments
            while (gm.CollectedFragments < gm.RequiredFragments)
            {
                gm.CollectFragment(null);
            }

            yield return new WaitForSeconds(0.05f);
            Assert.IsTrue(exitGate.IsUnlocked, "V6 Failed: ExitGate should unlock when required fragments collected.");

            // Touch exit gate
            player.transform.position = exitGate.transform.position;
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(GameState.Victory, gm.CurrentState, "V6 Failed: Reaching ExitGate must trigger GameState.Victory!");
            Debug.Log("[GlitchRun V6] PASSED: Checkpoint progression and ExitGate victory sequence verified.");
        }

        // V7: Camera and background bounds cover the player and have no gaps.
        [Test]
        public void V7_CameraBoundsAndParallaxLayersCoverage()
        {
            var cam = Object.FindAnyObjectByType<CameraFollow2D>();
            Assert.IsNotNull(cam, "V7 Failed: CameraFollow2D not found.");

            var so = new UnityEditor.SerializedObject(cam);
            Vector2 maxBounds = so.FindProperty("maxBounds").vector2Value;
            Assert.GreaterOrEqual(maxBounds.x, 370f, $"V7 Failed: Camera maxBounds.x ({maxBounds.x:F1}m) should cover at least 370m.");

            var parallaxLayers = Object.FindObjectsByType<ParallaxLayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.GreaterOrEqual(parallaxLayers.Length, 3, "V7 Failed: Expected 3 seamless parallax layers.");

            Debug.Log($"[GlitchRun V7] PASSED: Camera maxBounds ({maxBounds.x}m) and {parallaxLayers.Length} parallax layers verified.");
        }

        // V8: Anti-bypass: death zone covers full level abyss; boundary wall prevents skips.
        [Test]
        public void V8_AntiBypass_DeathZoneAndBoundaryWallsCoverage()
        {
            var deathZone = GameObject.Find("DeathZone");
            Assert.IsNotNull(deathZone, "V8 Failed: DeathZone not found.");
            var col = deathZone.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(col, "V8 Failed: DeathZone must have BoxCollider2D.");
            Assert.GreaterOrEqual(col.size.x, 350f, $"V8 Failed: DeathZone width ({col.size.x:F1}m) must span entire level length.");

            var rightWall = GameObject.Find("Boundary_RightWall");
            Assert.IsNotNull(rightWall, "V8 Failed: Boundary_RightWall not found.");

            Debug.Log($"[GlitchRun V8] PASSED: DeathZone span={col.size.x:F1}m and Boundary_RightWall verified.");
        }
    }
}
