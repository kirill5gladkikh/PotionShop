
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MushroomTear : MonoBehaviour
{
    [Header("Mushroom Sprites")]
    [SerializeField]
    private Sprite[] tearSprites = new Sprite[5];

    [Header("Final Mushroom Halves")]
    [SerializeField] private Sprite leftHalf;
    [SerializeField] private Sprite rightHalf;

    [Header("Half Positions")]
    [SerializeField] private float halfDistance = 0.5f;

    [Header("Tear Settings")]
    [SerializeField] private float progressPerPress = 12f;
    [SerializeField] private float recoveryDelay = 0.15f;

    [Header("Recovery Speed")]
    [SerializeField] private float fastRecovery = 150f;
    [SerializeField] private float slowRecovery = 55f;

    [Header("Smooth Stretching")]
    [SerializeField] private float maxStretchX = 1.18f;
    [SerializeField] private float minStretchY = 0.94f;
    [SerializeField] private float stretchSmoothness = 12f;

    [Header("Final Tear")]
    [SerializeField] private float finalReturnSpeed = 18f;

    [Header("Sprite Transition")]
    [SerializeField] private float transitionSpeed = 14f;

    [Header("Potion Shop Integration")]
    [SerializeField] private Collider2D processingArea;
    [SerializeField] private IngredientManager ingredientManager;
    [SerializeField] private Collider2D cauldronCollider;

    private Camera mainCamera;
    private Collider2D mushroomCollider;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer transitionRenderer;

    private float tearProgress;
    private float lastPressTime;
    private float transitionAlpha;

    private bool isSelected;
    private bool isTorn;
    private bool isAddedToCauldron;

    private bool isDraggingIngredient;
    private bool isDraggingHalves;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Vector3 dragOffset;

    private Vector3 halvesDragOffset;

    private readonly List<Transform> halves =
        new List<Transform>();

    public bool IsSelected => isSelected;
    public bool IsTorn => isTorn;
    public bool IsAddedToCauldron => isAddedToCauldron;

    // ======================================
    // INITIALIZATION
    // ======================================

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mushroomCollider = GetComponent<Collider2D>();

        originalScale = transform.localScale;
        originalPosition = transform.position;

        lastPressTime = Time.time;

        GameObject overlay =
            new GameObject("MushroomTransition");

        overlay.transform.SetParent(transform, false);

        transitionRenderer =
            overlay.AddComponent<SpriteRenderer>();

        transitionRenderer.sortingLayerID =
            spriteRenderer.sortingLayerID;

        transitionRenderer.sortingOrder =
            spriteRenderer.sortingOrder + 1;

        transitionRenderer.enabled = false;

        UpdateSprite();
    }

    void Start()
    {
        mainCamera = Camera.main;

        if (mushroomCollider == null)
        {
            Debug.LogError(
                name + ": Collider2D is missing!"
            );
        }

        if (processingArea == null)
        {
            Debug.LogError(
                name + ": Processing Area is not assigned!"
            );
        }

        if (ingredientManager == null)
        {
            Debug.LogError(
                name + ": IngredientManager is not assigned!"
            );
        }
    }

    // ======================================
    // UPDATE
    // ======================================

    void Update()
    {
        if (Mouse.current == null ||
            mainCamera == null ||
            isAddedToCauldron)
        {
            return;
        }

        // Before selection, only dragging works.
        if (!isSelected)
        {
            HandleIngredientDragging();
            return;
        }

        // Once placed on the cutting board,
        // the mushroom can be torn using Space.
        if (!isTorn)
        {
            HandleTearing();
            UpdateSprite();
            UpdateStretch();
            return;
        }

        // After tearing, finish the visual
        // transition and allow transferring.
        UpdateFinalTear();
        HandleHalvesTransfer();
    }

    // ======================================
    // DRAG WHOLE MUSHROOM TO BOARD
    // ======================================

    void HandleIngredientDragging()
    {
        if (processingArea == null ||
            mushroomCollider == null)
        {
            return;
        }

        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            mushroomCollider.OverlapPoint(mouse))
        {
            isDraggingIngredient = true;

            dragOffset =
                transform.position - mouse;
        }

        if (isDraggingIngredient &&
            Mouse.current.leftButton.isPressed)
        {
            Vector3 target = mouse + dragOffset;

            target.z = originalPosition.z;

            transform.position = target;
        }

        if (isDraggingIngredient &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDraggingIngredient = false;

            bool insideBoard =
                processingArea.OverlapPoint(
                    transform.position
                );

            if (insideBoard &&
                ingredientManager != null &&
                ingredientManager.TrySelectIngredient(name))
            {
                isSelected = true;

                // The mushroom is centered
                // on its own working surface.
                Vector3 boardCenter =
                    processingArea.bounds.center;

                transform.position = new Vector3(
                    boardCenter.x,
                    boardCenter.y,
                    originalPosition.z
                );

                lastPressTime = Time.time;

                Debug.Log(
                    name + " placed on cutting board!"
                );
            }
            else
            {
                transform.position =
                    originalPosition;
            }
        }
    }

    // ======================================
    // ORIGINAL TEAR MECHANIC
    // ======================================

    void HandleTearing()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            tearProgress = Mathf.Min(
                tearProgress + progressPerPress,
                100f
            );

            lastPressTime = Time.time;

            if (tearProgress >= 100f)
            {
                if (leftHalf == null ||
                    rightHalf == null)
                {
                    Debug.LogError(
                        name +
                        ": Assign both mushroom halves!"
                    );

                    tearProgress = 99f;
                    return;
                }

                tearProgress = 100f;

                CreateHalves();

                isTorn = true;

                Debug.Log(
                    name + ": mushroom torn! 1/1"
                );

                return;
            }
        }

        // Restore the mushroom when
        // the player stops pressing Space.
        if (Time.time - lastPressTime > recoveryDelay)
        {
            float recoverySpeed = Mathf.Lerp(
                fastRecovery,
                slowRecovery,
                tearProgress / 100f
            );

            tearProgress = Mathf.Max(
                0f,
                tearProgress -
                recoverySpeed * Time.deltaTime
            );
        }
    }

    // ======================================
    // ORIGINAL SPRITE ANIMATION
    // ======================================

    void UpdateSprite()
    {
        if (spriteRenderer == null ||
            tearSprites == null ||
            tearSprites.Length < 5)
        {
            return;
        }

        if (isTorn)
        {
            transitionRenderer.enabled = false;
            return;
        }

        if (tearProgress >= 20f &&
            tearProgress < 60f)
        {
            spriteRenderer.sprite =
                tearSprites[1];

            transitionRenderer.sprite =
                tearSprites[2];

            transitionRenderer.enabled = true;

            float targetAlpha =
                Mathf.InverseLerp(
                    25f,
                    45f,
                    tearProgress
                );

            float factor =
                1f - Mathf.Exp(
                    -transitionSpeed *
                    Time.deltaTime
                );

            transitionAlpha = Mathf.Lerp(
                transitionAlpha,
                targetAlpha,
                factor
            );

            Color color =
                spriteRenderer.color;

            color.a = transitionAlpha;

            transitionRenderer.color =
                color;

            return;
        }

        transitionRenderer.enabled = false;

        if (tearProgress < 20f)
        {
            spriteRenderer.sprite =
                tearSprites[0];

            transitionAlpha = 0f;
        }
        else
        {
            spriteRenderer.sprite =
                tearSprites[3];

            transitionAlpha = 1f;
        }
    }

    // ======================================
    // ORIGINAL STRETCH ANIMATION
    // ======================================

    void UpdateStretch()
    {
        float progress =
            tearProgress / 100f;

        Vector3 targetScale =
            new Vector3(
                originalScale.x *
                    Mathf.Lerp(
                        1f,
                        maxStretchX,
                        progress
                    ),

                originalScale.y *
                    Mathf.Lerp(
                        1f,
                        minStretchY,
                        progress
                    ),

                originalScale.z
            );

        float factor =
            1f - Mathf.Exp(
                -stretchSmoothness *
                Time.deltaTime
            );

        transform.localScale =
            Vector3.Lerp(
                transform.localScale,
                targetScale,
                factor
            );
    }

    // ======================================
    // FINAL TEAR ANIMATION
    // ======================================

    void UpdateFinalTear()
    {
        float factor =
            1f - Mathf.Exp(
                -finalReturnSpeed *
                Time.deltaTime
            );

        transform.localScale =
            Vector3.Lerp(
                transform.localScale,
                originalScale,
                factor
            );

        if (Vector3.Distance(
            transform.localScale,
            originalScale
        ) < 0.001f)
        {
            transform.localScale =
                originalScale;
        }
    }

    // ======================================
    // CREATE TWO MUSHROOM HALVES
    // ======================================

    void CreateHalves()
    {
        transitionRenderer.enabled = false;
        spriteRenderer.enabled = false;

        // Whole mushroom is no longer clickable.
        if (mushroomCollider != null)
            mushroomCollider.enabled = false;

        halves.Clear();

        CreateHalf(
            "Mushroom_Left",
            leftHalf,
            -halfDistance
        );

        CreateHalf(
            "Mushroom_Right",
            rightHalf,
            halfDistance
        );
    }

    void CreateHalf(
        string objectName,
        Sprite sprite,
        float offsetX
    )
    {
        GameObject half =
            new GameObject(objectName);

        half.transform.SetParent(
            transform,
            false
        );

        half.transform.localPosition =
            new Vector3(
                offsetX,
                0f,
                0f
            );

        half.transform.localRotation =
            Quaternion.identity;

        half.transform.localScale =
            Vector3.one;

        SpriteRenderer sr =
            half.AddComponent<SpriteRenderer>();

        sr.sprite = sprite;

        sr.sortingLayerID =
            spriteRenderer.sortingLayerID;

        sr.sortingOrder =
            spriteRenderer.sortingOrder;

        sr.color =
            spriteRenderer.color;

        half.AddComponent<PolygonCollider2D>();

        halves.Add(half.transform);
    }

    // ======================================
    // DRAG BOTH HALVES TO CAULDRON
    // ======================================

    void HandleHalvesTransfer()
    {
        if (halves.Count != 2 ||
            cauldronCollider == null)
        {
            return;
        }

        Vector3 mouse =
            GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsMouseOverHalf(mouse))
            {
                isDraggingHalves = true;

                halvesDragOffset =
                    halves[0].position - mouse;
            }
        }

        if (isDraggingHalves &&
            Mouse.current.leftButton.isPressed)
        {
            Vector3 target =
                mouse + halvesDragOffset;

            Vector3 movement =
                target - halves[0].position;

            foreach (Transform half in halves)
            {
                if (half != null)
                {
                    half.position += movement;
                }
            }
        }

        if (isDraggingHalves &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDraggingHalves = false;

            if (cauldronCollider.OverlapPoint(mouse))
            {
                AddToCauldron();
            }
            else
            {
                ResetHalvesToBoard();
            }
        }
    }

    bool IsMouseOverHalf(Vector3 mouse)
    {
        foreach (Transform half in halves)
        {
            if (half == null)
                continue;

            Collider2D col =
                half.GetComponent<Collider2D>();

            if (col != null &&
                col.OverlapPoint(mouse))
            {
                return true;
            }
        }

        return false;
    }

    void ResetHalvesToBoard()
    {
        if (processingArea == null ||
            halves.Count != 2)
        {
            return;
        }

        Vector3 center =
            processingArea.bounds.center;

        halves[0].localPosition =
            new Vector3(
                -halfDistance,
                0f,
                0f
            );

        halves[1].localPosition =
            new Vector3(
                halfDistance,
                0f,
                0f
            );
    }

    // ======================================
    // ADD MUSHROOM TO CAULDRON
    // ======================================

    void AddToCauldron()
    {
        if (isAddedToCauldron)
            return;

        isAddedToCauldron = true;

        // Register the same ingredient name
        // used by the existing recipe system.
        if (ingredientManager != null)
        {
            ingredientManager.AddIngredientToCauldron(
                gameObject.name
            );
        }

        foreach (Transform half in halves)
        {
            if (half != null)
                Destroy(half.gameObject);
        }

        halves.Clear();

        Debug.Log(
            name + " added to cauldron!"
        );
    }

    // ======================================
    // NEW INPUT SYSTEM
    // ======================================

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
