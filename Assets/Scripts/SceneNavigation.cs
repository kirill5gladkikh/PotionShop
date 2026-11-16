
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
    public GameObject serveButton;

    public static bool HasCompletedPotion = false;
    public static string CompletedPotionName = "";

    public static int Coins = 0;
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

        UpdateShopUI();
    }

    void Update()
    {
        if (returnButton != null &&
            ingredient != null &&
            ingredient.IsCorkPlaced)
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
            customerMessage =
                "This isn't the potion I ordered! " +
                "I need a " + requestedPotion + "!";

            Debug.Log(
                "Wrong potion: " + CompletedPotionName
            );

            HasCompletedPotion = false;
            CompletedPotionName = "";

            UpdateShopUI();
            return;
        }

        int reward = rewards[CurrentCustomer];

        Coins += reward;
        HasCompletedPotion = false;
        CompletedPotionName = "";
        CurrentCustomer++;
        customerMessage = "";

        Debug.Log(
            "Correct potion served! +" +
            reward + " coins"
        );

        UpdateShopUI();
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

        Debug.Log(
            "Potion delivered to shop: " +
            CompletedPotionName
        );

        SceneManager.LoadScene("WinterPotionShop");
    }
}
