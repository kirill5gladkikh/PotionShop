
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
    public GameObject serveButton;
    public CustomerAI customerAI;

    // Kept for compatibility with existing scripts.
    public static bool HasCompletedPotion = false;
    public static string CompletedPotionName = "";

    public static int Coins = 0;
    public static int Reputation = 50;
    public static int CurrentCustomer = 0;

    private string[] potionNames =
    {
        "Healing Potion",
        "Energy Potion",
        "Sleep Potion"
    };

    private int[] rewards = { 20, 25, 30 };

    private string customerMessage = "";

    void Start()
    {
        if (returnButton != null)
            returnButton.SetActive(false);

        SetupCustomer();
        UpdateShopUI();
    }

    void Update()
    {
        // Show return button after sealing the bottle.
        if (returnButton != null &&
            ingredient != null &&
            ingredient.IsCorkPlaced &&
            !returnButton.activeSelf)
        {
            returnButton.SetActive(true);
        }
    }

    // ==================================
    // CUSTOMERS
    // ==================================

    void SetupCustomer()
    {
        if (customerAI == null ||
            CurrentCustomer >= potionNames.Length)
        {
            return;
        }

        customerAI.SetupCharacter(CurrentCustomer);
    }

    void UpdateShopUI()
    {
        if (SceneManager.GetActiveScene().name !=
            "WinterPotionShop")
        {
            return;
        }

        if (coinsText != null)
            coinsText.text = "Coins: " + Coins;

        if (reputationText != null)
        {
            reputationText.text =
                "Reputation: " + Reputation + "/100";
        }

        bool allCompleted =
            CurrentCustomer >= potionNames.Length;

        if (orderText != null)
        {
            if (allCompleted)
            {
                orderText.text =
                    "All orders completed! " +
                    "You can still brew potions!";
            }
            else if (!string.IsNullOrEmpty(customerMessage))
            {
                orderText.text = customerMessage;
            }
            else
            {
                orderText.text =
                    "Hello! I need a " +
                    potionNames[CurrentCustomer] + "!";
            }
        }

        if (serveButton != null)
            serveButton.SetActive(!allCompleted);
    }

    // ==================================
    // SERVING POTIONS
    // ==================================

    public void ServePotion()
    {
        if (CurrentCustomer >= potionNames.Length)
            return;

        PotionInventory inventory =
            PotionInventory.Instance;

        if (inventory == null)
        {
            Debug.LogError(
                "PotionInventory was not found!"
            );

            return;
        }

        string requestedPotion =
            potionNames[CurrentCustomer];

        PotionInventory.StoredPotion readyPotion = null;
        PotionInventory.StoredPotion infusingPotion = null;

        // Search for the potion requested by the customer.
        foreach (var potion in inventory.potions)
        {
            if (potion.potionName != requestedPotion)
                continue;

            if (inventory.IsReady(potion))
            {
                readyPotion = potion;
                break;
            }

            if (infusingPotion == null ||
                inventory.GetRemainingTime(potion) <
                inventory.GetRemainingTime(infusingPotion))
            {
                infusingPotion = potion;
            }
        }

        // No ready potion available.
        if (readyPotion == null)
        {
            if (infusingPotion != null)
            {
                int secondsLeft = Mathf.CeilToInt(
                    (float)inventory.GetRemainingTime(
                        infusingPotion
                    )
                );

                customerMessage =
                    "Your potion is still infusing! " +
                    secondsLeft + "s remaining.";
            }
            else
            {
                customerMessage =
                    "No ready " + requestedPotion +
                    " in stock!";
            }

            UpdateShopUI();
            return;
        }

        // Remove the sold bottle from inventory.
        bool removed =
            inventory.RemovePotion(readyPotion.id);

        if (!removed)
            return;

        int reward = rewards[CurrentCustomer];

        Coins += reward;

        Reputation = Mathf.Clamp(
            Reputation + 5,
            0,
            100
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
            "Sold " + requestedPotion +
            "! +" + reward + " coins."
        );
    }

    // ==================================
    // ENTER BREWING STATION
    // ==================================

    public void OpenBrewingStation()
    {
        // Brewing is now always available.
        // The player does not need an active order.

        SceneManager.LoadScene("BrewingStation");
    }

    // ==================================
    // RETURN TO SHOP
    // ==================================

    public void OpenShop()
    {
        if (ingredient == null ||
            !ingredient.IsCorkPlaced)
        {
            Debug.LogWarning(
                "The bottle must be sealed first!"
            );

            return;
        }

        if (ingredientManager == null)
        {
            Debug.LogError(
                "IngredientManager is not connected!"
            );

            return;
        }

        PotionInventory inventory =
            PotionInventory.Instance;

        if (inventory == null)
        {
            Debug.LogError(
                "PotionInventory is missing! " +
                "Add it to the WinterPotionShop scene."
            );

            return;
        }

        string potionName =
            ingredientManager.PotionName;

        if (string.IsNullOrEmpty(potionName))
        {
            Debug.LogWarning(
                "Potion recipe has not been identified!"
            );

            return;
        }

        // Save the finished bottle.
        // Infusion begins immediately.
        inventory.AddPotion(potionName);

        Debug.Log(
            potionName +
            " added to inventory and started infusing!"
        );

        // Old instant-delivery system is disabled.
        HasCompletedPotion = false;
        CompletedPotionName = "";

        SceneManager.LoadScene("WinterPotionShop");
    }
}
