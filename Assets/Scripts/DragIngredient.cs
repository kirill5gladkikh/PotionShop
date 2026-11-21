
using UnityEngine;
using UnityEngine.InputSystem;

public class DragIngredient : MonoBehaviour
{
    [Header("Ingredient")]
    public Collider2D mortarCollider;
    public IngredientManager ingredientManager;

    [Header("Bottle Filling")]
    public GrindIngredient grindIngredient;
    public Collider2D bottleCollider;
    public Transform bottleLiquid;
    public float fillingTime = 3f;

    [Header("Liquid Surface")]
    public Transform liquidTop;

    [Tooltip("Adjust the wave position relative to liquid surface")]
    public float waveYOffset = 0f;

    [Tooltip("How far the wave gently moves up and down")]
    public float waveAmplitude = 0.008f;

    public float waveSpeed = 3f;
    public float waveRotation = 1.5f;

    [Header("Bottle Cork")]
    public Transform bottleCork;
    public Transform corkPoint;
    public float corkSnapDistance = 0.6f;

    private Camera mainCamera;
    private Collider2D ingredientCollider;

    private bool isDragging;
    private bool isInMortar;

    private Vector3 startPosition;
    private Vector3 offset;

    private bool isFilling;
    private bool bottleFilled;
    private float fillProgress;

    private SpriteRenderer liquidRenderer;
    private SpriteRenderer topRenderer;

    private Vector3 liquidFullScale;
    private Vector3 liquidFullPosition;

    private Vector3 topFullScale;
    private Vector3 topFullPosition;
    private Quaternion topOriginalRotation;

    private float liquidBottomY;
    private float liquidFullTopY;
    private float liquidBaseAlpha;
    private float topBaseAlpha;

    private bool isDraggingCork;
    private bool corkPlaced;

    private Vector3 corkStartPosition;
    private Vector3 corkOffset;
    private Collider2D corkCollider;

    public bool IsInMortar => isInMortar;
    public bool IsBottleFilled => bottleFilled;
    public bool IsCorkPlaced => corkPlaced;

    void Start()
    {
        mainCamera = Camera.main;

        ingredientCollider = GetComponent<Collider2D>();
        startPosition = transform.position;

        InitializeLiquid();

        if (bottleCork != null)
        {
            corkStartPosition = bottleCork.position;
            corkCollider =
                bottleCork.GetComponent<Collider2D>();
        }
    }

    void Update()
    {
        if (Mouse.current == null || mainCamera == null)
            return;

        HandleIngredientDragging();
        HandleBottleFilling();
        HandleLiquidSurface();
        HandleCorkDragging();
    }

    // ==================================
    // INGREDIENT DRAGGING
    // ==================================

    void HandleIngredientDragging()
    {
        if (isInMortar || ingredientCollider == null)
            return;

        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            ingredientCollider.OverlapPoint(mouse))
        {
            isDragging = true;
            offset = transform.position - mouse;
        }

        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector3 target = mouse + offset;
            target.z = transform.position.z;

            transform.position = target;
        }

        if (isDragging &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;

            if (mortarCollider != null &&
                mortarCollider.OverlapPoint(transform.position))
            {
                bool accepted =
                    ingredientManager != null &&
                    ingredientManager.TrySelectIngredient(name);

                if (accepted)
                {
                    Vector3 target =
                        ingredientManager.GetIngredientPosition(
                            mortarCollider.transform.position,
                            name
                        );

                    target.z = transform.position.z;
                    transform.position = target;

                    isInMortar = true;

                    Debug.Log("Herb placed in mortar!");
                }
                else
                {
                    transform.position = startPosition;
                }
            }
            else
            {
                transform.position = startPosition;
            }
        }
    }

    // ==================================
    // INITIAL LIQUID SETUP
    // ==================================

    void InitializeLiquid()
    {
        if (bottleLiquid == null)
        {
            Debug.LogWarning("BottleLiquid is missing!");
            return;
        }

        liquidRenderer =
            bottleLiquid.GetComponent<SpriteRenderer>();

        if (liquidRenderer == null ||
            liquidRenderer.sprite == null)
        {
            Debug.LogWarning(
                "BottleLiquid needs a SpriteRenderer and sprite!"
            );
            return;
        }

        // Save the exact size and position
        // that you adjusted in the Inspector.
        liquidFullScale = bottleLiquid.localScale;
        liquidFullPosition = bottleLiquid.localPosition;

        liquidBaseAlpha = liquidRenderer.color.a;
        liquidRenderer.enabled = true;

        Sprite sprite = liquidRenderer.sprite;

        // Determine the visible bottom and top
        // using the original sprite dimensions.
        float bottomOffset =
            sprite.bounds.min.y * liquidFullScale.y;

        float topOffset =
            sprite.bounds.max.y * liquidFullScale.y;

        liquidBottomY =
            liquidFullPosition.y + bottomOffset;

        liquidFullTopY =
            liquidFullPosition.y + topOffset;

        if (liquidTop != null)
        {
            topRenderer =
                liquidTop.GetComponent<SpriteRenderer>();

            topFullScale = liquidTop.localScale;
            topFullPosition = liquidTop.localPosition;
            topOriginalRotation = liquidTop.localRotation;

            if (topRenderer != null)
            {
                topBaseAlpha = topRenderer.color.a;
                topRenderer.enabled = true;
            }
        }

        fillProgress = 0f;
        UpdateLiquidVisuals();
    }

    // ==================================
    // BOTTLE FILLING
    // ==================================

    void HandleBottleFilling()
    {
        if (grindIngredient == null ||
            !grindIngredient.IsPotionReady ||
            bottleCollider == null ||
            bottleLiquid == null ||
            liquidRenderer == null ||
            bottleFilled)
        {
            return;
        }

        Vector3 mouse = GetMouseWorldPosition();

        bool insideBottle =
            bottleCollider.OverlapPoint(mouse);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isFilling = insideBottle;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame ||
            !insideBottle)
        {
            isFilling = false;
        }

        if (isFilling && Mouse.current.leftButton.isPressed)
        {
            fillProgress +=
                Time.deltaTime / Mathf.Max(0.1f, fillingTime);

            fillProgress = Mathf.Clamp01(fillProgress);

            UpdateLiquidVisuals();

            if (fillProgress >= 1f)
            {
                bottleFilled = true;
                isFilling = false;

                Debug.Log("Potion bottled!");
            }
        }
    }

    // ==================================
    // LIQUID GROWTH
    // ==================================

    void UpdateLiquidVisuals()
    {
        if (bottleLiquid == null ||
            liquidRenderer == null ||
            liquidRenderer.sprite == null)
        {
            return;
        }

        Sprite sprite = liquidRenderer.sprite;

        // Scale relative to the ORIGINAL
        // size of the artist's sprite.
        float currentScaleY =
            liquidFullScale.y * fillProgress;

        bottleLiquid.localScale = new Vector3(
            liquidFullScale.x,
            Mathf.Max(0.00001f, currentScaleY),
            liquidFullScale.z
        );

        // Keep the visible bottom fixed.
        float bottomSpriteOffset =
            sprite.bounds.min.y * currentScaleY;

        bottleLiquid.localPosition = new Vector3(
            liquidFullPosition.x,
            liquidBottomY - bottomSpriteOffset,
            liquidFullPosition.z
        );

        // Fade in quickly at the start.
        float alpha =
            Mathf.Clamp01(fillProgress / 0.08f);

        SetAlpha(
            liquidRenderer,
            liquidBaseAlpha * alpha
        );

        UpdateLiquidTop(alpha);
    }

    // ==================================
    // LIQUID SURFACE
    // ==================================

    void UpdateLiquidTop(float alpha)
    {
        if (liquidTop == null || topRenderer == null)
            return;

        float currentSurfaceY = Mathf.Lerp(
            liquidBottomY,
            liquidFullTopY,
            fillProgress
        );

        liquidTop.localScale = topFullScale;

        float centerOffset = 0f;

        if (topRenderer.sprite != null)
        {
            centerOffset =
                topRenderer.sprite.bounds.center.y *
                topFullScale.y;
        }

        liquidTop.localPosition = new Vector3(
            topFullPosition.x,
            currentSurfaceY - centerOffset + waveYOffset,
            topFullPosition.z
        );

        SetAlpha(
            topRenderer,
            topBaseAlpha * alpha
        );
    }

    void HandleLiquidSurface()
    {
        if (liquidTop == null ||
            topRenderer == null ||
            fillProgress <= 0f)
        {
            return;
        }

        float movement =
            Mathf.Sin(Time.time * waveSpeed) *
            waveAmplitude;

        float rotation =
            Mathf.Sin(Time.time * waveSpeed * 0.7f) *
            waveRotation;

        float centerOffset = 0f;

        if (topRenderer.sprite != null)
        {
            centerOffset =
                topRenderer.sprite.bounds.center.y *
                topFullScale.y;
        }

        float currentSurfaceY = Mathf.Lerp(
            liquidBottomY,
            liquidFullTopY,
            fillProgress
        );

        liquidTop.localPosition = new Vector3(
            topFullPosition.x,
            currentSurfaceY - centerOffset +
                waveYOffset + movement,
            topFullPosition.z
        );

        liquidTop.localRotation =
            topOriginalRotation *
            Quaternion.Euler(0f, 0f, rotation);
    }

    void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
            return;

        Color color = renderer.color;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    // ==================================
    // CORK DRAGGING
    // ==================================

    void HandleCorkDragging()
    {
        if (!bottleFilled ||
            corkPlaced ||
            bottleCork == null ||
            corkPoint == null ||
            corkCollider == null)
        {
            return;
        }

        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            corkCollider.OverlapPoint(mouse))
        {
            isDraggingCork = true;
            corkOffset = bottleCork.position - mouse;
        }

        if (isDraggingCork && Mouse.current.leftButton.isPressed)
        {
            Vector3 target = mouse + corkOffset;

            bottleCork.position = new Vector3(
                target.x,
                target.y,
                corkStartPosition.z
            );
        }

        if (isDraggingCork &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDraggingCork = false;

            float distance = Vector2.Distance(
                bottleCork.position,
                corkPoint.position
            );

            if (distance <= corkSnapDistance)
            {
                bottleCork.position = new Vector3(
                    corkPoint.position.x,
                    corkPoint.position.y,
                    corkStartPosition.z
                );

                corkPlaced = true;

                Debug.Log(
                    "Potion complete! Bottle sealed!"
                );
            }
            else
            {
                bottleCork.position = corkStartPosition;
            }
        }
    }

    // ==================================
    // MOUSE
    // ==================================

    Vector3 GetMouseWorldPosition()
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
}
