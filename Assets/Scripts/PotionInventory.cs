
using System;
using System.Collections.Generic;
using UnityEngine;

public class PotionInventory : MonoBehaviour
{
    public static PotionInventory Instance { get; private set; }

    [Header("Testing")]
    public bool testMode = true;

    [Header("Infusion Times")]
    public float healingTime = 3f;
    public float energyTime = 4f;
    public float sleepTime = 5f;

    [Header("Storage")]
    [Min(1)]
    public int cabinetCapacity = 3;

    [Serializable]
    public class StoredPotion
    {
        public int id;
        public string potionName;
        public int slotIndex;
        public double finishTime;
        public bool isInfusing;
    }

    public List<StoredPotion> potions =
        new List<StoredPotion>();

    private int nextId = 1;

    public int TotalCapacity => cabinetCapacity;

    public bool HasFreeSpace =>
        potions.Count < cabinetCapacity;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (StoredPotion potion in potions)
        {
            nextId = Mathf.Max(nextId, potion.id + 1);
        }
    }

    // =====================================
    // ADD POTION
    // =====================================

    public bool AddPotion(string potionName)
    {
        if (!HasFreeSpace)
        {
            Debug.LogWarning(
                "Cabinet is full! Sell or discard a potion."
            );
            return false;
        }

        int freeSlot = FindFreeCabinetSlot();

        if (freeSlot < 0)
        {
            Debug.LogWarning(
                "No free cabinet slot!"
            );
            return false;
        }

        StoredPotion potion = new StoredPotion
        {
            id = nextId++,
            potionName = potionName,
            slotIndex = freeSlot,
            finishTime =
                GetCurrentTime() +
                GetInfusionDuration(potionName),
            isInfusing = true
        };

        potions.Add(potion);

        Debug.Log(
            potionName +
            " stored in slot " +
            freeSlot
        );

        return true;
    }

    // =====================================
    // INFUSION
    // =====================================

    public float GetInfusionDuration(string potionName)
    {
        float duration = 3f;

        switch (potionName)
        {
            case "Healing Potion":
                duration = healingTime;
                break;

            case "Energy Potion":
                duration = energyTime;
                break;

            case "Sleep Potion":
                duration = sleepTime;
                break;
        }

        return testMode ? duration : duration * 60f;
    }

    public double GetRemainingTime(StoredPotion potion)
    {
        if (potion == null ||
            !potion.isInfusing ||
            potion.slotIndex < 0)
        {
            return 0;
        }

        return Math.Max(
            0,
            potion.finishTime - GetCurrentTime()
        );
    }

    public bool IsReady(StoredPotion potion)
    {
        if (potion == null ||
            !potion.isInfusing ||
            potion.slotIndex < 0)
        {
            return false;
        }

        return GetRemainingTime(potion) <= 0;
    }

    // =====================================
    // SELL POTION
    // =====================================

    public bool RemovePotion(int potionId)
    {
        StoredPotion potion = potions.Find(
            p => p.id == potionId
        );

        if (potion == null || !IsReady(potion))
            return false;

        potions.Remove(potion);

        Debug.Log(
            potion.potionName + " sold. Slot freed."
        );

        return true;
    }

    // =====================================
    // DISCARD POTION
    // =====================================

    public bool DiscardPotion(int potionId)
    {
        StoredPotion potion = potions.Find(
            p => p.id == potionId
        );

        if (potion == null)
            return false;

        string removedName = potion.potionName;

        potions.Remove(potion);

        Debug.Log(
            removedName +
            " discarded. Cabinet slot freed."
        );

        return true;
    }

    // =====================================
    // CABINET SLOTS
    // =====================================

    private int FindFreeCabinetSlot()
    {
        for (int i = 0; i < cabinetCapacity; i++)
        {
            bool occupied = potions.Exists(
                p => p.slotIndex == i
            );

            if (!occupied)
                return i;
        }

        return -1;
    }

    private double GetCurrentTime()
    {
        return Time.realtimeSinceStartupAsDouble;
    }
}
