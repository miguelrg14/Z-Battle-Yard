using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZBattleYard.Core;

public class Inventory : MonoBehaviour
{
    #region - Variables -

    [SerializeField] public Weapon[] weapons; // 0 = primary, 1 = secondary, 2 = tertiary

    [SerializeField] public int inventorySpace = 2;

    PlayerHUD hud;
    WeaponShooting shooting;
    EquipmentManager equipmentManager;

    #endregion

    #region - Awake | Start -

    void Awake()
    {
        GetReferences();
    }
    void Start()
    {
        InitVariables();        
    }

    #endregion

    #region - Logic -

    public void AddItem(Weapon newItem) // Add item to weapon Inventory
    {
        for (int i = 0; i <= (weapons.Length - 1); i++)
        {
            if (weapons.Length < inventorySpace)
            {
                int newSpace = weapons.Length + 1;
                ResizeInventorySpace(newSpace);
            }
            if (weapons[i] == null)
            {
                weapons[i] = newItem;
                equipmentManager.ChangeCurrentlyEquippedWeapon(i);
                shooting.InitAmmo(i, newItem);
                return;
            }
        }

        weapons[equipmentManager.currentlyEquippedWeapon] = newItem;
        shooting.InitAmmo(equipmentManager.currentlyEquippedWeapon, newItem);
    }

    public void RemoveItem(int index) // Remove item from weapon Inventory
    {
        weapons[index] = null;
    }

    public Weapon GetItem(int index) // Gets if the Weapon object of the index asked with "index" (primary, secondary, tertiary)
    {
        equipmentManager.ChangeCurrentlyEquippedWeapon(index);
        return weapons[index];
    }

    public void ResizeInventorySpace(int newSize)
    {
        Array.Resize(ref weapons, newSize);
    }
    public bool WeaponInInventory(Weapon weapon) => WeaponInInventoryIndex(weapon) >= 0;

    /// <summary>Slot that holds <paramref name="weapon"/>, or -1 if the player doesn't own it.</summary>
    public int WeaponInInventoryIndex(Weapon weapon) => SlotUtility.IndexOf(weapons, weapon);
    #endregion

    void GetReferences()
    {
        shooting = GetComponent<WeaponShooting>();
        equipmentManager = GetComponent<EquipmentManager>();
    }

    void InitVariables()
    {
        ResizeInventorySpace(inventorySpace);
        
        AddItem(equipmentManager.defaultWeapon);    // Add weapon to inventory array        
        equipmentManager.EquipWeapon(equipmentManager.defaultWeapon);   // Equip default weapon
    }
}
