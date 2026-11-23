using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ZBattleYard.Tests.Gameplay
{
    /// <summary>Checks that the game data and project configuration are consistent.</summary>
    public class ProjectSetupTests
    {
        static IEnumerable<T> LoadAll<T>() where T : Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null);

        [Test]
        public void BuildSettings_StartWithMainMenuThenTrainStation()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

            Assert.That(scenes, Is.EqualTo(new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/TrainStation.unity" }));
            foreach (string scene in scenes)
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene), scene + " is missing");
        }

        [Test]
        public void Weapons_HaveValidStats()
        {
            var weapons = LoadAll<Weapon>().ToList();
            Assert.That(weapons, Is.Not.Empty);

            foreach (Weapon weapon in weapons)
            {
                Assert.IsNotNull(weapon.prefab, weapon.name + " has no prefab");
                Assert.Greater(weapon.damage, 0, weapon.name + " damage");
                Assert.GreaterOrEqual(weapon.fireRate, 0f, weapon.name + " fire rate");
                Assert.GreaterOrEqual(weapon.magazineSize, 0, weapon.name + " magazine size");
                Assert.GreaterOrEqual(weapon.storedAmmo, 0, weapon.name + " stored ammo");
                if (weapon.weaponType != WeaponType.Melee)
                    Assert.Greater(weapon.magazineSize, 0, weapon.name + " is a firearm without magazine");
            }
        }

        [Test]
        public void Perks_HaveIconAndPrice()
        {
            foreach (Perk perk in LoadAll<Perk>())
            {
                Assert.IsNotNull(perk.icon, perk.name + " has no icon");
                Assert.GreaterOrEqual(perk.price, 0, perk.name + " price");
            }
        }

        [Test]
        public void PowerUps_HavePrefab_AndTimedOnesHaveIcon()
        {
            var powerUps = LoadAll<PowerUp>().ToList();
            Assert.That(powerUps, Is.Not.Empty);

            foreach (PowerUp powerUp in powerUps)
            {
                Assert.IsNotNull(powerUp.prefab, powerUp.name + " has no prefab");
                // Instant power-ups (Max Ammo, Nuke) have no duration and show no HUD icon
                if (powerUp.duration > 0)
                    Assert.IsNotNull(powerUp.icon, powerUp.name + " is a timed power-up without HUD icon");
                Assert.GreaterOrEqual(powerUp.duration, 0, powerUp.name + " duration");
            }
        }

        [Test]
        public void InputActions_ContainEveryGameplayAction()
        {
            var controls = new GameControls();
            try
            {
                string[] required =
                {
                    "OnFoot/Movement", "OnFoot/Look", "OnFoot/Jump", "OnFoot/Sprint", "OnFoot/Crouch", "OnFoot/Prone",
                    "OnFoot/Interact", "OnFoot/Menu", "OnFoot/Lantern", "OnFoot/SeeStats",
                    "Weapon/Fire1Pressed", "Weapon/Reload", "Weapon/Melee", "Weapon/Grenade", "Weapon/SwitchWeapon",
                };
                foreach (string action in required)
                    Assert.IsNotNull(controls.asset.FindAction(action), action + " is missing");
            }
            finally
            {
                // Dispose() calls Object.Destroy, which is not allowed in Edit Mode
                Object.DestroyImmediate(controls.asset);
            }
        }
    }
}
