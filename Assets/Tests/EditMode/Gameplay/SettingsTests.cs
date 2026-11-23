using NUnit.Framework;
using UnityEngine;

namespace ZBattleYard.Tests.Gameplay
{
    public class SettingsTests
    {
        const string LegacySensitivityKey = "sensivity";

        string savedSensitivity;
        string savedFov;

        [SetUp]
        public void SaveAndClearPrefs()
        {
            savedSensitivity = PlayerPrefs.HasKey(GameSettings.SensitivityKey) ? PlayerPrefs.GetFloat(GameSettings.SensitivityKey).ToString("R") : null;
            savedFov = PlayerPrefs.HasKey(GameSettings.FieldOfViewKey) ? PlayerPrefs.GetInt(GameSettings.FieldOfViewKey).ToString() : null;
            PlayerPrefs.DeleteKey(GameSettings.SensitivityKey);
            PlayerPrefs.DeleteKey(LegacySensitivityKey);
            PlayerPrefs.DeleteKey(GameSettings.FieldOfViewKey);
        }

        [TearDown]
        public void RestorePrefs()
        {
            PlayerPrefs.DeleteKey(GameSettings.SensitivityKey);
            PlayerPrefs.DeleteKey(LegacySensitivityKey);
            PlayerPrefs.DeleteKey(GameSettings.FieldOfViewKey);
            if (savedSensitivity != null) PlayerPrefs.SetFloat(GameSettings.SensitivityKey, float.Parse(savedSensitivity));
            if (savedFov != null) PlayerPrefs.SetInt(GameSettings.FieldOfViewKey, int.Parse(savedFov));
        }

        [Test]
        public void FirstLaunch_UsesUsableDefaults()
        {
            Assert.AreEqual(GameSettings.DefaultSensitivity, GameSettings.MouseSensitivity);
            Assert.AreEqual(GameSettings.DefaultFieldOfView, GameSettings.FieldOfView);
        }

        [Test]
        public void LegacySensitivityKey_IsStillRead()
        {
            PlayerPrefs.SetFloat(LegacySensitivityKey, 3.5f);

            Assert.AreEqual(3.5f, GameSettings.MouseSensitivity);
        }

        [Test]
        public void InvalidFieldOfView_FallsBackToDefault()
        {
            PlayerPrefs.SetInt(GameSettings.FieldOfViewKey, 0);

            Assert.AreEqual(GameSettings.DefaultFieldOfView, GameSettings.FieldOfView);
        }

        [TestCase(1f, 0f)]
        [TestCase(0.1f, -20f)]
        [TestCase(0f, -80f)]
        public void LinearToDecibels_ClampsSilence(float linear, float expectedDb)
        {
            Assert.AreEqual(expectedDb, AudioOptionsManager.LinearToDecibels(linear), 0.01f);
        }
    }
}
