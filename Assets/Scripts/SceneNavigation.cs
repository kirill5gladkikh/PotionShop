
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneNavigation : MonoBehaviour
{
    [Header("Brewing Station")]
    public GameObject returnButton;
    public DragIngredient ingredient;

    [Header("Shop")]
    public TMP_Text orderText;
    public TMP_Text coinsText;
    public GameObject serveButton;

    public static bool HasCompletedPotion = false;
    public static int Coins = 0;
    public static bool OrderCompleted = false;

    void Start()
    {
        if (returnButton != null)
            returnButton.SetActive(false);

        UpdateShopUI();
    }

    void Update()
    {
        if (returnButton != null && ingredient != null)
        {
            if (ingredient.IsCorkPlaced &&
                !returnButton.activeSelf)
            {
                returnButton.SetActive(true);
            }
        }
    }

    void UpdateShopUI()
    {
        if (SceneManager.GetActiveScene().name !=
            "WinterPotionShop")
            return;

        if (coinsText != null)
            coinsText.text = "Coins: " + Coins;

        if (orderText != null)
        {
            if (OrderCompleted)
            {
                orderText.text =
                    "Thank you! This potion is perfect!";
            }
            else
            {
                orderText.text =
                    "Hello! I need a healing potion!";
            }
        }

        if (serveButton != null)
            serveButton.SetActive(!OrderCompleted);
    }

    public void ServePotion()
    {
        if (OrderCompleted)
            return;

        if (!HasCompletedPotion)
        {
            Debug.Log("You need to brew a potion first!");
            return;
        }

        HasCompletedPotion = false;
        OrderCompleted = true;
        Coins += 20;

        Debug.Log("Potion served! +20 coins");

        UpdateShopUI();
    }

    public void OpenBrewingStation()
    {
        if (OrderCompleted)
        {
            Debug.Log("Order already completed!");
            return;
        }

        HasCompletedPotion = false;
        SceneManager.LoadScene("BrewingStation");
    }

    public void OpenShop()
    {
        if (ingredient != null && ingredient.IsCorkPlaced)
        {
            HasCompletedPotion = true;
        }

        SceneManager.LoadScene("WinterPotionShop");
    }
}
