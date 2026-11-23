
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Customer Delivery")]
    public SceneNavigation shopNavigation;

    [Tooltip("Automatically finds the Customer object")]
    public SpriteRenderer customerSprite;

    [Min(0.01f)]
    public float dragThreshold = 0.15f;

    [Range(0f, 1f)]
    public float deliveryOverlapRequired = 0.1f;

    [Header("Delete Button")]
    [Range(0.1f, 0.5f)]
    public float deleteButtonSize = 0.22f;

    public Vector2 deleteButtonOffset =
        new Vector2(0.05f, 0.05f);

    private readonly Dictionary<int, GameObject> bottles =
        new Dictionary<int, GameObject>();

    private readonly Dictionary<SpriteRenderer, int> sortingOrders =
        new Dictionary<SpriteRenderer, int>();

    private Camera mainCamera;
    private Sprite crossSprite;

    private int selectedId = -1;
    private int pressedId = -1;

    private bool dragging;

    private Vector3 mouseStart;
    private Vector3 dragOffset;
    private float bottleZ;

    private GameObject deleteButton;
    private BoxCollider2D deleteCollider;

    void Start()
    {
        mainCamera = Camera.main;

        FindShopReferences();

        crossSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        RefreshCabinet();
    }

    void Update()
    {
        RefreshCabinet();

        if (Mouse.current == null || mainCamera == null)
            return;

        Vector3 mouse = MouseWorld();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            MouseDown(mouse);

        if (Mouse.current.leftButton.isPressed &&
            pressedId >= 0)
        {
            MouseHold(mouse);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            MouseUp(mouse);

        UpdateCrossPosition();
    }

    // =====================================
    // FIND CUSTOMER AND SHOP MANAGER
    // =====================================

    void FindShopReferences()
    {
        if (shopNavigation == null)
        {
            shopNavigation =
                FindFirstObjectByType<SceneNavigation>();
        }

        if (customerSprite == null)
        {
            GameObject customer =
                GameObject.Find("Customer");

            if (customer != null)
            {
                customerSprite =
                    customer.GetComponent<SpriteRenderer>();
            }
        }

        if (customerSprite == null)
        {
            Debug.LogError(
                "PotionCabinet: Customer Sprite not found!"
            );
        }
        else
        {
            Debug.Log(
                "PotionCabinet: Customer found: " +
                customerSprite.gameObject.name
            );
        }

        if (shopNavigation == null)
        {
            Debug.LogError(
                "PotionCabinet: ShopManager not found!"
            );
        }
    }

    // =====================================
    // CABINET DISPLAY
    // =====================================

    void RefreshCabinet()
    {
        PotionInventory inventory =
            PotionInventory.Instance;

        if (inventory == null || bottlePrefab == null)
            return;

        HashSet<int> validIds = new HashSet<int>();

        foreach (var potion in inventory.potions)
        {
            int index = potion.slotIndex;

            if (index < 0 ||
                index >= slots.Length ||
                slots[index] == null)
            {
                continue;
            }

            validIds.Add(potion.id);

            if (!bottles.TryGetValue(
                potion.id,
                out GameObject bottle) ||
                bottle == null)
            {
                bottle = Instantiate(
                    bottlePrefab,
                    slots[index]
                );

                bottle.name =
                    potion.potionName + "_" + potion.id;

                ResetBottle(bottle);

                SetPotionColor(
                    bottle,
                    potion.potionName
                );

                bottles[potion.id] = bottle;
            }
            else if (pressedId != potion.id &&
                     bottle.transform.parent != slots[index])
            {
                ReturnToSlot(potion.id);
            }
        }

        List<int> removed = new List<int>();

        foreach (var pair in bottles)
        {
            if (!validIds.Contains(pair.Key) ||
                pair.Value == null)
            {
                if (pair.Value != null)
                    Destroy(pair.Value);

                removed.Add(pair.Key);
            }
        }

        foreach (int id in removed)
        {
            bottles.Remove(id);

            if (selectedId == id)
                Deselect();

            if (pressedId == id)
            {
                pressedId = -1;
                dragging = false;
                RestoreSorting();
            }
        }
    }

    void ResetBottle(GameObject bottle)
    {
        bottle.transform.localPosition = Vector3.zero;
        bottle.transform.localRotation = Quaternion.identity;

        bottle.transform.localScale =
            bottlePrefab.transform.localScale * bottleScale;
    }

    // =====================================
    // MOUSE INPUT
    // =====================================

    void MouseDown(Vector3 mouse)
    {
        if (selectedId >= 0 &&
            deleteCollider != null &&
            deleteCollider.OverlapPoint(mouse))
        {
            DeletePotion();
            return;
        }

        int id = FindBottle(mouse);

        if (id < 0)
        {
            Deselect();
            return;
        }

        pressedId = id;
        mouseStart = mouse;
        dragging = false;

        GameObject bottle = bottles[id];

        dragOffset = bottle.transform.position - mouse;
        bottleZ = bottle.transform.position.z;
    }

    void MouseHold(Vector3 mouse)
    {
        if (!bottles.TryGetValue(
            pressedId,
            out GameObject bottle) ||
            bottle == null)
        {
            return;
        }

        if (!dragging)
        {
            float movement =
                Vector2.Distance(mouseStart, mouse);

            if (movement < dragThreshold)
                return;

            PotionInventory inventory =
                PotionInventory.Instance;

            if (inventory == null)
                return;

            var potion = inventory.potions.Find(
                p => p.id == pressedId
            );

            if (potion == null)
                return;

            if (!inventory.IsReady(potion))
            {
                return;
            }

            dragging = true;
            Deselect();

            bottle.transform.SetParent(null, true);

            BringToFront(bottle);

            Debug.Log(
                "BOTTLE DRAG START: " +
                potion.potionName
            );
        }

        Vector3 target = mouse + dragOffset;
        target.z = bottleZ;

        bottle.transform.position = target;
    }

    void MouseUp(Vector3 mouse)
    {
        if (pressedId < 0)
            return;

        int id = pressedId;
        bool wasDragging = dragging;

        pressedId = -1;
        dragging = false;

        if (!bottles.TryGetValue(
            id,
            out GameObject bottle) ||
            bottle == null)
        {
            return;
        }

        if (!wasDragging)
        {
            SelectBottle(id);
            return;
        }

        Debug.Log("BOTTLE RELEASED");

        bool accepted = TryDeliver(
            id,
            bottle,
            mouse
        );

        RestoreSorting();

        if (!accepted)
        {
            ReturnToSlot(id);

            Debug.Log(
                "BOTTLE RETURNED TO CABINET"
            );
        }
        else
        {
            Debug.Log(
                "BOTTLE DELIVERED SUCCESSFULLY"
            );
        }

        Deselect();
        RefreshCabinet();
    }

    // =====================================
    // CUSTOMER DELIVERY
    // =====================================

    bool TryDeliver(
        int id,
        GameObject bottle,
        Vector3 mouse)
    {
        // Find references again if necessary.
        if (shopNavigation == null ||
            customerSprite == null)
        {
            FindShopReferences();
        }

        if (shopNavigation == null)
        {
            Debug.LogError(
                "DELIVERY FAILED: ShopManager missing."
            );

            return false;
        }

        if (customerSprite == null)
        {
            Debug.LogError(
                "DELIVERY FAILED: Customer not found."
            );

            return false;
        }

        if (!customerSprite.enabled ||
            !customerSprite.gameObject.activeInHierarchy ||
            customerSprite.sprite == null)
        {
            Debug.LogWarning(
                "DELIVERY FAILED: Customer is not visible."
            );

            return false;
        }

        if (!TryGetBottleBounds(
            bottle,
            out Bounds bottleBounds))
        {
            Debug.LogWarning(
                "DELIVERY FAILED: Bottle bounds missing."
            );

            return false;
        }

        Bounds customerBounds =
            customerSprite.bounds;

        // Method 1: mouse directly over customer.
        bool cursorOverCustomer =
            mouse.x >= customerBounds.min.x &&
            mouse.x <= customerBounds.max.x &&
            mouse.y >= customerBounds.min.y &&
            mouse.y <= customerBounds.max.y;

        // Method 2: bottle sprite overlaps customer.
        float overlapWidth = Mathf.Max(
            0f,
            Mathf.Min(
                bottleBounds.max.x,
                customerBounds.max.x
            ) -
            Mathf.Max(
                bottleBounds.min.x,
                customerBounds.min.x
            )
        );

        float overlapHeight = Mathf.Max(
            0f,
            Mathf.Min(
                bottleBounds.max.y,
                customerBounds.max.y
            ) -
            Mathf.Max(
                bottleBounds.min.y,
                customerBounds.min.y
            )
        );

        float bottleArea = Mathf.Max(
            0.0001f,
            bottleBounds.size.x *
            bottleBounds.size.y
        );

        float overlapRatio =
            overlapWidth * overlapHeight /
            bottleArea;

        bool bottleOverCustomer =
            overlapRatio >= deliveryOverlapRequired;

        bool detected =
            cursorOverCustomer || bottleOverCustomer;

        Debug.Log(
            "CUSTOMER CHECK: " +
            "Cursor=" + cursorOverCustomer +
            " | Bottle overlap=" +
            (overlapRatio * 100f).ToString("F1") +
            "% | Detected=" + detected
        );

        if (!detected)
            return false;

        bool sold =
            shopNavigation.TryServePotion(id);

        Debug.Log(
            "SALE RESULT: " + sold
        );

        return sold;
    }

    // =====================================
    // BOTTLE BOUNDS
    // =====================================

    bool TryGetBottleBounds(
        GameObject bottle,
        out Bounds result)
    {
        result = new Bounds();

        bool found = false;

        SpriteRenderer[] renderers =
            bottle.GetComponentsInChildren<SpriteRenderer>(
                true
            );

        foreach (SpriteRenderer renderer in renderers)
        {
            if (!renderer.enabled ||
                !renderer.gameObject.activeInHierarchy ||
                renderer.sprite == null ||
                renderer.color.a <= 0.01f)
            {
                continue;
            }

            if (!found)
            {
                result = renderer.bounds;
                found = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    int FindBottle(Vector3 mouse)
    {
        foreach (var pair in bottles)
        {
            if (pair.Value == null)
                continue;

            if (!TryGetBottleBounds(
                pair.Value,
                out Bounds bounds))
            {
                continue;
            }

            if (mouse.x >= bounds.min.x &&
                mouse.x <= bounds.max.x &&
                mouse.y >= bounds.min.y &&
                mouse.y <= bounds.max.y)
            {
                return pair.Key;
            }
        }

        return -1;
    }

    // =====================================
    // RETURN TO CABINET
    // =====================================

    void ReturnToSlot(int id)
    {
        PotionInventory inventory =
            PotionInventory.Instance;

        if (inventory == null ||
            !bottles.TryGetValue(
                id,
                out GameObject bottle) ||
            bottle == null)
        {
            return;
        }

        var potion = inventory.potions.Find(
            p => p.id == id
        );

        if (potion == null ||
            potion.slotIndex < 0 ||
            potion.slotIndex >= slots.Length)
        {
            return;
        }

        Transform slot = slots[potion.slotIndex];

        if (slot == null)
            return;

        bottle.transform.SetParent(slot, false);

        ResetBottle(bottle);
    }

    // =====================================
    // RENDER ORDER
    // =====================================

    void BringToFront(GameObject bottle)
    {
        sortingOrders.Clear();

        foreach (SpriteRenderer renderer in
            bottle.GetComponentsInChildren<SpriteRenderer>(
                true
            ))
        {
            sortingOrders[renderer] =
                renderer.sortingOrder;

            renderer.sortingOrder += 200;
        }
    }

    void RestoreSorting()
    {
        foreach (var pair in sortingOrders)
        {
            if (pair.Key != null)
            {
                pair.Key.sortingOrder = pair.Value;
            }
        }

        sortingOrders.Clear();
    }

    // =====================================
    // SELECT BOTTLE
    // =====================================

    void SelectBottle(int id)
    {
        if (!bottles.ContainsKey(id))
            return;

        Deselect();

        selectedId = id;

        CreateDeleteCross();
        UpdateCrossPosition();
    }

    void Deselect()
    {
        selectedId = -1;

        if (deleteButton != null)
        {
            Destroy(deleteButton);
        }

        deleteButton = null;
        deleteCollider = null;
    }

    // =====================================
    // DISCARD POTION
    // =====================================

    void DeletePotion()
    {
        if (selectedId < 0)
            return;

        PotionInventory inventory =
            PotionInventory.Instance;

        if (inventory != null)
        {
            inventory.DiscardPotion(selectedId);
        }

        Deselect();
        RefreshCabinet();
    }

    // =====================================
    // DELETE CROSS
    // =====================================

    void CreateDeleteCross()
    {
        deleteButton =
            new GameObject("DeletePotionButton");

        deleteButton.transform.SetParent(
            transform,
            false
        );

        GameObject background =
            new GameObject("Background");

        background.transform.SetParent(
            deleteButton.transform,
            false
        );

        SpriteRenderer bg =
            background.AddComponent<SpriteRenderer>();

        bg.sprite = crossSprite;
        bg.color = new Color(0.75f, 0.15f, 0.15f);
        bg.sortingOrder = 501;

        background.transform.localScale =
            new Vector3(
                deleteButtonSize,
                deleteButtonSize,
                1f
            );

        CreateCrossLine(45f);
        CreateCrossLine(-45f);

        deleteCollider =
            deleteButton.AddComponent<BoxCollider2D>();

        deleteCollider.isTrigger = true;

        deleteCollider.size = new Vector2(
            deleteButtonSize,
            deleteButtonSize
        );
    }

    void CreateCrossLine(float angle)
    {
        GameObject line =
            new GameObject("CrossLine");

        line.transform.SetParent(
            deleteButton.transform,
            false
        );

        SpriteRenderer renderer =
            line.AddComponent<SpriteRenderer>();

        renderer.sprite = crossSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 502;

        line.transform.localRotation =
            Quaternion.Euler(0f, 0f, angle);

        line.transform.localScale =
            new Vector3(
                deleteButtonSize * 0.65f,
                deleteButtonSize * 0.12f,
                1f
            );
    }

    void UpdateCrossPosition()
    {
        if (deleteButton == null ||
            selectedId < 0 ||
            !bottles.TryGetValue(
                selectedId,
                out GameObject bottle) ||
            bottle == null)
        {
            return;
        }

        if (!TryGetBottleBounds(
            bottle,
            out Bounds bounds))
        {
            return;
        }

        deleteButton.transform.position =
            new Vector3(
                bounds.max.x + deleteButtonOffset.x,
                bounds.max.y + deleteButtonOffset.y,
                transform.position.z
            );

        deleteButton.transform.localScale =
            new Vector3(
                1f / Mathf.Max(
                    0.001f,
                    Mathf.Abs(transform.lossyScale.x)
                ),
                1f / Mathf.Max(
                    0.001f,
                    Mathf.Abs(transform.lossyScale.y)
                ),
                1f
            );
    }

    // =====================================
    // POTION COLORS
    // =====================================

    void SetPotionColor(
        GameObject bottle,
        string potionName)
    {
        Color color;

        switch (potionName)
        {
            case "Healing Potion":
                color = new Color(
                    0.35f, 0.85f, 0.45f
                );
                break;

            case "Energy Potion":
                color = new Color(
                    1f, 0.75f, 0.2f
                );
                break;

            case "Sleep Potion":
                color = new Color(
                    0.55f, 0.4f, 0.9f
                );
                break;

            default:
                color = Color.gray;
                break;
        }

        foreach (SpriteRenderer renderer in
            bottle.GetComponentsInChildren<SpriteRenderer>(
                true
            ))
        {
            if (renderer.gameObject.name != "BottleLiquid" &&
                renderer.gameObject.name != "LiquidTop")
            {
                continue;
            }

            color.a = renderer.color.a;
            renderer.color = color;
        }
    }

    // =====================================
    // MOUSE WORLD
    // =====================================

    Vector3 MouseWorld()
    {
        Vector2 screen =
            Mouse.current.position.ReadValue();

        return mainCamera.ScreenToWorldPoint(
            new Vector3(
                screen.x,
                screen.y,
                -mainCamera.transform.position.z
            )
        );
    }

    void OnDestroy()
    {
        RestoreSorting();

        if (crossSprite != null)
        {
            Destroy(crossSprite);
        }
    }
}
