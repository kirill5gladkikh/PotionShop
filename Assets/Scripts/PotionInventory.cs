
using System;
using System.Collections.Generic;
using UnityEngine;

public class PotionInventory : MonoBehaviour
{
    public static PotionInventory Instance { get; private set; }

    [Header("Testing")]
    public bool testMode = true;

    [Header("Potion Infusion Times")]
    public float healingTime = 3f;
    public float energyTime = 4f;
    public float sleepTime = 5f;

    [Serializable]
    public class StoredPotion
    {
        public int id;
        public string potionName;
        public double finishTime;
        public bool isInfusing;
    }

    public List<StoredPotion> potions = new List<StoredPotion>();

    private int nextId = 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddPotion(string potionName)
    {
        float duration = GetInfusionDuration(potionName);

        StoredPotion potion = new StoredPotion
        {
            id = nextId++,
            potionName = potionName,
            finishTime = GetCurrentTime() + duration,
            isInfusing = true
        };

        potions.Add(potion);

        Debug.Log(
            $"Added {potionName}. Infusion: {duration} seconds."
        );
    }

    public float GetInfusionDuration(string potionName)
    {
        float time = 3f;

        switch (potionName)
        {
            case "Healing Potion":
                time = healingTime;
                break;

            case "Energy Potion":
                time = energyTime;
                break;

            case "Sleep Potion":
                time = sleepTime;
                break;
        }

        return testMode ? time : time * 60f;
    }

    public double GetRemainingTime(StoredPotion potion)
    {
        if (!potion.isInfusing)
            return 0;

        return Math.Max(
            0,
            potion.finishTime - GetCurrentTime()
        );
    }

    public bool IsReady(StoredPotion potion)
    {
        return GetRemainingTime(potion) <= 0;
    }

    public bool RemovePotion(int potionId)
    {
        StoredPotion potion = potions.Find(
            p => p.id == potionId
        );

        if (potion == null || !IsReady(potion))
            return false;

        return potions.Remove(potion);
    }

    private double GetCurrentTime()
    {
        return Time.realtimeSinceStartupAsDouble;
    }
}
