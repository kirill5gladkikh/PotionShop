
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


    void SetupCustomer()
    {
        if (customerAI == null ||
            CurrentCustomer >= potionNames.Length)
            return;

        customerAI.SetupCharacter(CurrentCustomer);
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
    }

    void UpdateShopUI()
    {
        if (SceneManager.GetActiveScene().name !=
            "WinterPotionShop")
            return;

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
                    "All orders completed! Great job!";
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

    public void ServePotion()
    {
        if (CurrentCustomer >= potionNames.Length)
            return;

        if (!HasCompletedPotion)
        {
            customerMessage =
                "You haven't prepared my potion yet!";

            UpdateShopUI();
            return;
        }

        string requestedPotion =
            potionNames[CurrentCustomer];

        if (CompletedPotionName != requestedPotion)
        {
            Reputation = Mathf.Clamp(
                Reputation - 10, 0, 100
            );

            if (customerAI != null)
            {
                customerAI.ReceiveWrongPotion();

                if (customerAI.WillLeave())
                {
                    Debug.Log("Customer left the shop!");

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
            }
            else
            {
                customerMessage =
                    "Wrong potion! I need a " +
                    requestedPotion + "!";
            }

            HasCompletedPotion = false;
            CompletedPotionName = "";

            UpdateShopUI();
            return;
        }

        int reward = rewards[CurrentCustomer];

        Coins += reward;

        Reputation = Mathf.Clamp(
            Reputation + 5, 0, 100
        );

        if (customerAI != null)
            customerAI.ReceiveCorrectPotion();

        HasCompletedPotion = false;
        CompletedPotionName = "";

        CurrentCustomer++;
        customerMessage = "";

        SetupCustomer();
        UpdateShopUI();

        Debug.Log(
            "Correct potion! +" + reward +
            " coins, +5 reputation"
        );
    }

    public void OpenBrewingStation()
    {
        if (CurrentCustomer >= potionNames.Length)
        {
            Debug.Log("All orders completed!");
            return;
        }

        HasCompletedPotion = false;
        CompletedPotionName = "";

        SceneManager.LoadScene("BrewingStation");
    }

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

        CompletedPotionName =
            ingredientManager.PotionName;

        HasCompletedPotion = true;

        SceneManager.LoadScene("WinterPotionShop");
    }
}
