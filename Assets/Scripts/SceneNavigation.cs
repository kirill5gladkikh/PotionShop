
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneNavigation : MonoBehaviour
{
    [Header("Brewing Station")]
    public GameObject returnButton;
    public DragIngredient ingredient;
    public IngredientManager ingredientManager;

    [Header("Shop")]
    public TMP_Text orderText;
    public TMP_Text coinsText;
    public TMP_Text reputationText;
    public CustomerAI customerAI;

    [Header("Customer Delivery")]
    public SpriteRenderer customerSprite;

    public static bool HasCompletedPotion = false;
    public static string CompletedPotionName = "";

    public static int Coins = 0;
    public static int Reputation = 50;
    public static int CurrentCustomer = 0;

    private readonly string[] potionNames =
    {
        "Healing Potion",
        "Energy Potion",
        "Sleep Potion"
    };

    private readonly int[] rewards = { 20, 25, 30 };

    private string customerMessage = "";
    private bool cabinetFullMessageShown;

    public bool HasActiveCustomer =>
        CurrentCustomer < potionNames.Length;

    // Reset everything when Play starts,
    // including when Domain Reload is disabled.
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGameSession()
    {
        Coins = 0;
        Reputation = 50;
        CurrentCustomer = 0;
        HasCompletedPotion = false;
        CompletedPotionName = "";
    }

    void Start()
    {
        if (returnButton != null)
            returnButton.SetActive(false);

        SetupCustomer();
        UpdateShopUI();
    }

    void Update()
    {
        if (returnButton != null &&
            ingredient != null &&
            ingredient.IsCorkPlaced &&
            !returnButton.activeSelf)
        {
            returnButton.SetActive(true);
        }

        if (cabinetFullMessageShown)
        {
            PotionInventory inventory =
                PotionInventory.Instance;

            if (inventory != null && inventory.HasFreeSpace)
            {
                cabinetFullMessageShown = false;
                customerMessage = "";
                UpdateShopUI();
            }
        }
    }

    // =================================
    // COINS
    // =================================

    public static bool SpendCoins(int amount)
    {
        if (amount < 0 || Coins < amount)
            return false;

        Coins -= amount;
        return true;
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        Coins += amount;
    }

    // =================================
    // CUSTOMER
    // =================================

    void SetupCustomer()
    {
        if (customerAI == null || !HasActiveCustomer)
            return;

        customerAI.SetupCharacter(CurrentCustomer);
    }

    void UpdateShopUI()
    {
        if (SceneManager.GetActiveScene().name !=
            "WinterPotionShop")
            return;

        if (coinsText != null)
            coinsText.text = "Coins: " + Coins;

        if (reputationText != null)
            reputationText.text =
                "Reputation: " + Reputation + "/100";

        if (orderText == null)
            return;

        if (cabinetFullMessageShown)
        {
            orderText.text =
                "Your potion cabinet is full!\n" +
                "Sell or discard a potion to make space.";
        }
        else if (!string.IsNullOrEmpty(customerMessage))
        {
            orderText.text = customerMessage;
        }
        else if (!HasActiveCustomer)
        {
            orderText.text =
                "All orders completed! " +
                "You can still brew potions!";
        }
        else
        {
            orderText.text =
                "Hello! I need a " +
                potionNames[CurrentCustomer] + "!";
        }
    }

    // =================================
    // SCREEN-SPACE DELIVERY DETECTION
    // =================================

    public bool IsBottleOverCustomer(Bounds bottleBounds)
    {
        if (!HasActiveCustomer)
            return false;

        if (customerSprite == null ||
            !customerSprite.enabled ||
            !customerSprite.gameObject.activeInHierarchy ||
            customerSprite.sprite == null)
        {
            Debug.LogWarning(
                "Customer Sprite is not assigned or visible!"
            );
            return false;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("Main Camera not found!");
            return false;
        }

        Rect bottleRect = WorldBoundsToScreenRect(
            cam, bottleBounds
        );

        Rect customerRect = WorldBoundsToScreenRect(
            cam, customerSprite.bounds
        );

        if (bottleRect.width <= 0 ||
            bottleRect.height <= 0 ||
            customerRect.width <= 0 ||
            customerRect.height <= 0)
        {
            return false;
        }

        float overlapWidth = Mathf.Max(
            0f,
            Mathf.Min(bottleRect.xMax, customerRect.xMax) -
            Mathf.Max(bottleRect.xMin, customerRect.xMin)
        );

        float overlapHeight = Mathf.Max(
            0f,
            Mathf.Min(bottleRect.yMax, customerRect.yMax) -
            Mathf.Max(bottleRect.yMin, customerRect.yMin)
        );

        float overlapArea = overlapWidth * overlapHeight;
        float bottleArea = bottleRect.width * bottleRect.height;

        float overlapRatio =
            overlapArea / Mathf.Max(1f, bottleArea);

        bool accepted = overlapRatio >= 0.10f;

        Debug.Log(
            "Bottle/customer overlap: " +
            (overlapRatio * 100f).ToString("F1") + "%" +
            " | Target detected: " + accepted
        );

        return accepted;
    }

    private Rect WorldBoundsToScreenRect(
        Camera cam,
        Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3 a = cam.WorldToScreenPoint(
            new Vector3(min.x, min.y, bounds.center.z)
        );

        Vector3 b = cam.WorldToScreenPoint(
            new Vector3(max.x, min.y, bounds.center.z)
        );

        Vector3 c = cam.WorldToScreenPoint(
            new Vector3(min.x, max.y, bounds.center.z)
        );

        Vector3 d = cam.WorldToScreenPoint(
            new Vector3(max.x, max.y, bounds.center.z)
        );

        float left = Mathf.Min(a.x, b.x, c.x, d.x);
        float right = Mathf.Max(a.x, b.x, c.x, d.x);
        float bottom = Mathf.Min(a.y, b.y, c.y, d.y);
        float top = Mathf.Max(a.y, b.y, c.y, d.y);

        return Rect.MinMaxRect(
            left, bottom, right, top
        );
    }

    // =================================
    // SELL EXACT SELECTED POTION
    // =================================

    public bool TryServePotion(int potionId)
    {
        if (!HasActiveCustomer)
            return false;

        PotionInventory inventory =
            PotionInventory.Instance;

        if (inventory == null)
            return false;

        PotionInventory.StoredPotion potion =
            inventory.potions.Find(p => p.id == potionId);

        if (potion == null || !inventory.IsReady(potion))
            return false;

        string requestedPotion =
            potionNames[CurrentCustomer];

        string deliveredPotion = potion.potionName;

        if (!inventory.RemovePotion(potionId))
            return false;

        cabinetFullMessageShown = false;

        if (deliveredPotion != requestedPotion)
        {
            Reputation = Mathf.Clamp(
                Reputation - 10, 0, 100
            );

            bool customerLeaves = false;

            if (customerAI != null)
            {
                customerAI.ReceiveWrongPotion();
                customerLeaves = customerAI.WillLeave();
            }

            if (customerLeaves)
            {
                CurrentCustomer++;
                customerMessage = "";
                SetupCustomer();
            }
            else
            {
                customerMessage =
                    "Wrong potion! I need a " +
                    requestedPotion + "!";
            }

            UpdateShopUI();

            Debug.Log(
                "Wrong potion delivered: " + deliveredPotion
            );

            return true;
        }

        int reward = rewards[CurrentCustomer];

        AddCoins(reward);

        Reputation = Mathf.Clamp(
            Reputation + 5, 0, 100
        );

        if (customerAI != null)
            customerAI.ReceiveCorrectPotion();

        CurrentCustomer++;
        customerMessage = "";

        HasCompletedPotion = false;
        CompletedPotionName = "";

        SetupCustomer();
        UpdateShopUI();

        Debug.Log(
            "Sold " + deliveredPotion +
            "! +" + reward + " coins."
        );

        return true;
    }

    // =================================
    // BREWING
    // =================================

    public void OpenBrewingStation()
    {
        PotionInventory inventory =
            PotionInventory.Instance;

        if (inventory == null)
        {
            Debug.LogError("PotionInventory is missing!");
            return;
        }

        if (!inventory.HasFreeSpace)
        {
            cabinetFullMessageShown = true;
            UpdateShopUI();
            return;
        }

        cabinetFullMessageShown = false;

        SceneManager.LoadScene("BrewingStation");
    }

    public void OpenShop()
    {
        if (ingredient == null || !ingredient.IsCorkPlaced)
        {
            Debug.LogWarning(
                "The bottle must be sealed first!"
            );
            return;
        }

        if (ingredientManager == null)
        {
            Debug.LogError(
                "IngredientManager is missing!"
            );
            return;
        }

        PotionInventory inventory =
            PotionInventory.Instance;

        if (inventory == null)
        {
            Debug.LogError("PotionInventory is missing!");
            return;
        }

        string potionName = ingredientManager.PotionName;

        if (string.IsNullOrEmpty(potionName))
        {
            Debug.LogWarning(
                "Potion recipe has not been identified!"
            );
            return;
        }

        if (!inventory.AddPotion(potionName))
        {
            Debug.LogWarning(
                "Cabinet full! Cannot store potion."
            );
            return;
        }

        HasCompletedPotion = false;
        CompletedPotionName = "";

        SceneManager.LoadScene("WinterPotionShop");
    }

    // =================================
    // UPGRADE SHOP
    // =================================

    public void OpenUpgradeShop()
    {
        SceneManager.LoadScene("UpgradeShop");
    }

    public void ReturnFromUpgradeShop()
    {
        SceneManager.LoadScene("WinterPotionShop");
    }
}
