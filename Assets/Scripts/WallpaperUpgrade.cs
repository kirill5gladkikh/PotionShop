
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WallpaperUpgrade : MonoBehaviour
{
    [Header("Wallpaper Levels")]
    [SerializeField] private Sprite[] wallpapers;

    [Header("Current Level")]
    [SerializeField] private int currentLevel = 0;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 0 at the beginning of every Play session.
        // Changes only when purchasing/equipping wallpapers.
        currentLevel = WallpaperShop.EquippedWallpaper;

        ApplyWallpaper();
    }

    public void SetWallpaper(int level)
    {
        if (wallpapers == null ||
            level < 0 ||
            level >= wallpapers.Length ||
            wallpapers[level] == null)
            return;

        currentLevel = level;
        ApplyWallpaper();
    }

    private void ApplyWallpaper()
    {
        if (spriteRenderer == null ||
            wallpapers == null ||
            currentLevel < 0 ||
            currentLevel >= wallpapers.Length ||
            wallpapers[currentLevel] == null)
            return;

        spriteRenderer.sprite = wallpapers[currentLevel];
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}
