
using UnityEngine;

public class ShopCategoryUI : MonoBehaviour
{
    [SerializeField] private GameObject wallpapersPanel;

    public void OpenWallpapers()
    {
        wallpapersPanel.SetActive(true);
    }

    public void CloseWallpapers()
    {
        wallpapersPanel.SetActive(false);
    }
}
