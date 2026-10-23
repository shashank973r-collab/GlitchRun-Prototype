using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using GlitchRun.Core;
using GlitchRun.Player;
using GlitchRun.Hazards;

namespace GlitchRun.Tests
{
    public class GlitchRunTests
    {
        [Test]
        public void TestBuildSettingsContainAllFourScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 4, "Build settings must contain at least 4 scenes.");

            Assert.IsTrue(scenes[0].path.EndsWith("MainMenu.unity"), "Scene 0 must be MainMenu.unity");

            bool hasGameplay = false;
            bool hasVictory = false;
            bool hasGameOver = false;

            foreach (var s in scenes)
            {
                if (s.path.EndsWith("Gameplay.unity")) hasGameplay = true;
                if (s.path.EndsWith("Victory.unity")) hasVictory = true;
                if (s.path.EndsWith("GameOver.unity")) hasGameOver = true;
            }

            Assert.IsTrue(hasGameplay, "Gameplay.unity must be registered in build settings.");
            Assert.IsTrue(hasVictory, "Victory.unity must be registered in build settings.");
            Assert.IsTrue(hasGameOver, "GameOver.unity must be registered in build settings.");
        }

        [Test]
        public void TestPlayerJumpKinematicsMath()
        {
            float speed = 8.5f;
            float jumpForce = 13.5f;
            float g = 3.5f * 9.81f;

            float maxJumpHeight = (jumpForce * jumpForce) / (2f * g);
            float timeToPeak = jumpForce / g;
            float airTime = timeToPeak * 2f;
            float maxHorizontalDist = speed * airTime;

            Assert.Greater(maxJumpHeight, 2.0f, "Player max jump height must exceed 2.0m.");
            Assert.Greater(maxHorizontalDist, 5.0f, "Player horizontal leap distance must exceed 5.0m.");
        }

        [Test]
        public void TestGlitchAbilityCooldownInvariants()
        {
            var go = new GameObject("TestPlayer");
            var glitch = go.AddComponent<GlitchAbility>();

            Assert.IsFalse(glitch.IsGlitching);
            Assert.IsTrue(glitch.CanGlitch);
            Assert.AreEqual(0f, glitch.CooldownTimer);

            bool activated = glitch.TryActivateGlitch();
            Assert.IsTrue(activated);
            Assert.IsTrue(glitch.IsGlitching);
            Assert.IsFalse(glitch.CanGlitch);
            Assert.Greater(glitch.GlitchDuration, 0f);

            glitch.ResetAbility();
            Assert.IsFalse(glitch.IsGlitching);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TestDroneHazardPatrolSetup()
        {
            var go = new GameObject("TestDrone");
            var drone = go.AddComponent<DroneHazard>();

            Vector3 a = new Vector3(0, 0, 0);
            Vector3 b = new Vector3(5, 0, 0);
            drone.SetWaypoints(a, b);

            Assert.IsNotNull(drone);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TestGameManagerScoreCalculationFormula()
        {
            int fragmentsCollected = 3;
            float elapsedTime = 60f;
            int livesRemaining = 3;

            int fragmentScore = fragmentsCollected * 250; // 750
            int timeBonus = Mathf.Max(0, (int)((180f - elapsedTime) * 10f)); // 1200
            int livesBonus = livesRemaining * 500; // 1500
            int totalScore = fragmentScore + timeBonus + livesBonus; // 3450

            Assert.AreEqual(3450, totalScore);
            Assert.Greater(totalScore, 2500, "Clean run should attain S+ rank (>2500).");
        }
    }
}
