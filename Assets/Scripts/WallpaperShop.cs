
using UnityEngine;
using TMPro;

public class WallpaperShop : MonoBehaviour
{
    [Header("Shop UI")]
    public TMP_Text coinsText;
    public TMP_Text wallpaperStatusText;

    [Header("Wallpaper Prices")]
    public int secondWallpaperPrice = 100;

    public static int EquippedWallpaper = 0;
    public static bool SecondWallpaperPurchased = false;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetWallpaperSession()
    {
        EquippedWallpaper = 0;
        SecondWallpaperPurchased = false;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void BuyOrEquipSecondWallpaper()
    {
        if (SecondWallpaperPurchased)
        {
            EquipWallpaper(1);
            return;
        }

        if (!SceneNavigation.SpendCoins(secondWallpaperPrice))
        {
            Debug.Log("Not enough coins!");
            UpdateUI();
            return;
        }

        SecondWallpaperPurchased = true;
        EquipWallpaper(1);

        Debug.Log("Second wallpaper purchased!");
    }

    public void EquipFirstWallpaper()
    {
        EquipWallpaper(0);
    }

    private void EquipWallpaper(int level)
    {
        EquippedWallpaper = level;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (coinsText != null)
        {
            coinsText.text =
                "Coins: " + SceneNavigation.Coins;
        }

        if (wallpaperStatusText == null)
            return;

        if (EquippedWallpaper == 1)
        {
            wallpaperStatusText.text = "Equipped";
        }
        else if (SecondWallpaperPurchased)
        {
            wallpaperStatusText.text = "Owned";
        }
        else
        {
            wallpaperStatusText.text =
                secondWallpaperPrice + " Coins";
        }
    }
}
