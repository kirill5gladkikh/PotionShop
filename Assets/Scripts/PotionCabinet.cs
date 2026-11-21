
using UnityEngine;
using System.Collections.Generic;

public class PotionCabinet : MonoBehaviour
{
    [Header("Bottle Prefab")]
    public GameObject bottlePrefab;

    [Header("Cabinet Slots")]
    public Transform[] slots = new Transform[3];

    [Header("Bottle Settings")]
    [Range(0.1f, 2f)]
    public float bottleScale = 1f;

    private Dictionary<int, GameObject> displayedBottles =
        new Dictionary<int, GameObject>();

    private Dictionary<int, int> bottleSlots =
        new Dictionary<int, int>();

    void Update()
    {
        RefreshCabinet();
    }

    void RefreshCabinet()
    {
        PotionInventory inventory =
            PotionInventory.Instance;

        if (inventory == null || bottlePrefab == null)
            return;

        HashSet<int> visibleIds = new HashSet<int>();

        int count = Mathf.Min(
            inventory.potions.Count,
            slots.Length
        );

        // Display available potions.
        for (int i = 0; i < count; i++)
        {
            if (slots[i] == null)
                continue;

            var potion = inventory.potions[i];
            visibleIds.Add(potion.id);

            if (!displayedBottles.ContainsKey(potion.id))
            {
                GameObject bottle = Instantiate(
                    bottlePrefab,
                    slots[i]
                );

                bottle.name =
                    potion.potionName + "_" + potion.id;

                bottle.transform.localPosition = Vector3.zero;
                bottle.transform.localRotation = Quaternion.identity;

                bottle.transform.localScale =
                    bottlePrefab.transform.localScale * bottleScale;

                SetPotionColor(
                    bottle,
                    potion.potionName
                );

                displayedBottles.Add(potion.id, bottle);
                bottleSlots.Add(potion.id, i);
            }
            else if (bottleSlots[potion.id] != i)
            {
                GameObject bottle =
                    displayedBottles[potion.id];

                bottle.transform.SetParent(slots[i], false);
                bottle.transform.localPosition = Vector3.zero;
                bottle.transform.localRotation = Quaternion.identity;

                bottleSlots[potion.id] = i;
            }
        }

        // Remove sold bottles from the cabinet.
        List<int> removeIds = new List<int>();

        foreach (var pair in displayedBottles)
        {
            if (!visibleIds.Contains(pair.Key))
            {
                Destroy(pair.Value);
                removeIds.Add(pair.Key);
            }
        }

        foreach (int id in removeIds)
        {
            displayedBottles.Remove(id);
            bottleSlots.Remove(id);
        }
    }

    void SetPotionColor(GameObject bottle, string potionName)
    {
        Transform liquid =
            bottle.transform.Find("BottleLiquid");

        if (liquid == null)
        {
            Debug.LogWarning(
                "BottleLiquid was not found in bottle prefab!"
            );
            return;
        }

        SpriteRenderer liquidRenderer =
            liquid.GetComponent<SpriteRenderer>();

        if (liquidRenderer == null)
            return;

        switch (potionName)
        {
            case "Healing Potion":
                liquidRenderer.color =
                    new Color(0.35f, 0.85f, 0.45f);
                break;

            case "Energy Potion":
                liquidRenderer.color =
                    new Color(1f, 0.75f, 0.2f);
                break;

            case "Sleep Potion":
                liquidRenderer.color =
                    new Color(0.55f, 0.4f, 0.9f);
                break;

            default:
                liquidRenderer.color =
                    new Color(0.6f, 0.6f, 0.6f);
                break;
        }
    }
}
