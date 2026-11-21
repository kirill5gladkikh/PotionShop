
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class IngredientSelector : MonoBehaviour
{
    public enum ProcessingType
    {
        Grinding,
        Crushing,
        PetalPicking,
        Cutting,
        Breaking
    }

    [Header("Processing")]
    public ProcessingType processingType =
        ProcessingType.Grinding;

    public int requiredActions = 5;
    public float requiredMovement = 8f;
    public float movementThreshold = 1f;

    [Header("Working Area")]
    public Collider2D processingArea;
    public IngredientManager ingredientManager;

    [Header("Flower")]
    public float petalPullDistance = 0.3f;

    [Header("Berry Juice")]
    public GameObject berryJuice;
    public float juiceExpansion = 1.5f;

    [Header("Cutting Knife")]
    public Transform cuttingKnife;
    public int requiredCuts = 3;
    public float minimumCutDistance = 0.25f;

    [Header("New Root Sprites")]
    public Sprite rootWhole;
    public Sprite rootCut1;
    public Sprite rootCut2;
    public Sprite[] rootSlices = new Sprite[4];

    [Header("Root Cut Guide")]
    public SpriteRenderer cutGuide;

    [Range(0f, 1f)]
    public float guideFirstX = 0.25f;

    [Range(0f, 1f)]
    public float guideSecondX = 0.50f;

    [Range(0f, 1f)]
    public float guideThirdX = 0.75f;

    public float guideYOffset = 0f;

    [Range(0.01f, 0.4f)]
    public float cutTolerance = 0.12f;

    [Header("Guide Pulse")]
    [Range(0f, 1f)]
    public float guideMinAlpha = 0.35f;

    [Range(0f, 1f)]
    public float guideMaxAlpha = 1f;

    public float guidePulseSpeed = 2.5f;

    [Header("Final Root Slices")]
    [Range(0.5f, 1.5f)]
    public float rootSliceSize = 1f;

    [Range(0.5f, 1f)]
    public float sliceWidthMultiplier = 0.88f;

    [Header("Cauldron")]
    public Collider2D cauldronCollider;
    public SpriteRenderer potionLiquid;

    private Camera mainCamera;
    private Collider2D ingredientCollider;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer knifeRenderer;

    private Vector3 startPosition;
    private Vector3 originalScale;
    private Vector3 dragOffset;
    private Vector3 originalJuiceScale;

    private Color originalColor;
    private Color guideOriginalColor;

    private bool isDragging;
    private bool isSelected;
    private bool isProcessing;
    private bool isGround;
    private bool isTransferring;
    private bool isAddedToCauldron;
    private bool isCutting;

    private int actionCount;
    private int cutCount;

    private float grindProgress;
    private float knifeZ;

    private Vector2 lastMousePosition;
    private Vector3 cutStartMousePosition;
    private Vector3 transferOffset;

    private Transform activePetal;
    private Vector3 petalStartPosition;
    private Vector3 petalDragOffset;

    private Bounds originalRootBounds;
    private bool hasRootBounds;

    private readonly List<Collider2D> petals =
        new List<Collider2D>();

    private readonly List<GameObject> pieces =
        new List<GameObject>();

    private readonly List<Vector3> pieceStartPositions =
        new List<Vector3>();

    public bool IsSelected => isSelected;
    public bool IsGround => isGround;
    public bool IsAddedToCauldron => isAddedToCauldron;

    // ==================================
    // INITIALIZATION
    // ==================================

    void Start()
    {
        mainCamera = Camera.main;
        ingredientCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        startPosition = transform.position;
        originalScale = transform.localScale;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (berryJuice != null)
        {
            originalJuiceScale =
                berryJuice.transform.localScale;

            berryJuice.SetActive(false);
        }

        if (processingType == ProcessingType.PetalPicking)
            FindFlowerPetals();

        if (processingType == ProcessingType.Cutting)
            InitializeRootCutting();
    }

    void Update()
    {
        if (Mouse.current == null ||
            mainCamera == null ||
            ingredientCollider == null ||
            processingArea == null)
        {
            HideKnife();
            return;
        }

        if (!isSelected)
            HandleDragging();
        else if (!isGround)
            HandleProcessing();
        else if (!isAddedToCauldron)
            HandleTransfer();

        if (processingType == ProcessingType.Cutting)
            UpdateCutGuide();
    }

    // ==================================
    // INITIALIZE ROOT
    // ==================================

    void InitializeRootCutting()
    {
        if (spriteRenderer != null && rootWhole != null)
        {
            spriteRenderer.sprite = rootWhole;
            spriteRenderer.enabled = true;
        }

        // Old rectangle segments are no longer used.
        // Disable them, but do not destroy scene objects.
        for (int i = 1; i <= 4; i++)
        {
            Transform oldSegment =
                transform.Find("RootSegment" + i);

            if (oldSegment != null)
                oldSegment.gameObject.SetActive(false);
        }

        requiredCuts = 3;

        if (cuttingKnife != null)
        {
            knifeRenderer =
                cuttingKnife.GetComponent<SpriteRenderer>();

            knifeZ = cuttingKnife.position.z;
            HideKnife();
        }

        if (cutGuide == null)
        {
            Transform guide =
                transform.Find("CutGuide");

            if (guide != null)
                cutGuide = guide.GetComponent<SpriteRenderer>();
        }

        if (cutGuide != null)
        {
            guideOriginalColor = cutGuide.color;
            cutGuide.enabled = false;
        }

        if (rootWhole == null ||
            rootCut1 == null ||
            rootCut2 == null)
        {
            Debug.LogWarning(
                name + ": Assign all three root stage sprites."
            );
        }

        if (rootSlices == null || rootSlices.Length != 4)
        {
            Debug.LogWarning(
                name + ": Assign four root slice sprites."
            );
        }
    }

    // ==================================
    // FIND FLOWER PETALS
    // ==================================

    void FindFlowerPetals()
    {
        petals.Clear();

        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D col in colliders)
        {
            if (col != ingredientCollider &&
                col.name.StartsWith("Petal"))
            {
                petals.Add(col);
            }
        }
    }

    // ==================================
    // DRAG INGREDIENT
    // ==================================

    void HandleDragging()
    {
        Vector3 mouse = GetMouseWorldPosition();

        bool clickedIngredient =
            ingredientCollider.OverlapPoint(mouse);

        if (processingType == ProcessingType.PetalPicking)
        {
            foreach (Collider2D petal in petals)
            {
                if (petal != null &&
                    petal.enabled &&
                    petal.OverlapPoint(mouse))
                {
                    clickedIngredient = true;
                    break;
                }
            }
        }

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            clickedIngredient)
        {
            isDragging = true;
            dragOffset = transform.position - mouse;
        }

        if (isDragging &&
            Mouse.current.leftButton.isPressed)
        {
            transform.position = mouse + dragOffset;
        }

        if (isDragging &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;

            bool insideArea =
                processingArea.OverlapPoint(transform.position);

            if (insideArea &&
                ingredientManager != null &&
                ingredientManager.TrySelectIngredient(name))
            {
                isSelected = true;

                Vector3 target;

                if (processingType == ProcessingType.Cutting)
                {
                    target = processingArea.bounds.center;
                }
                else
                {
                    target =
                        ingredientManager.GetIngredientPosition(
                            processingArea.bounds.center,
                            name
                        );
                }

                target.z = transform.position.z;
                transform.position = target;

                if (processingType == ProcessingType.Cutting)
                {
                    originalRootBounds = spriteRenderer.bounds;
                    hasRootBounds = true;
                    PositionCutGuide();
                }
            }
            else
            {
                transform.position = startPosition;
            }
        }
    }

    // ==================================
    // PROCESSING MODES
    // ==================================

    void HandleProcessing()
    {
        if (processingType == ProcessingType.PetalPicking)
        {
            HandlePetalPicking();
            return;
        }

        if (processingType == ProcessingType.Cutting)
        {
            HandleCutting();
            return;
        }

        Vector3 mouse = GetMouseWorldPosition();

        bool insideArea = processingArea.OverlapPoint(mouse);

        if (processingType == ProcessingType.Grinding)
        {
            HandleGrinding(insideArea, mouse);
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            insideArea &&
            ingredientCollider.OverlapPoint(mouse))
        {
            actionCount++;

            float progress = Mathf.Clamp01(
                (float)actionCount /
                Mathf.Max(1, requiredActions)
            );

            ApplyProcessingVisuals(progress);

            if (progress >= 1f)
                FinishProcessing();
        }
    }

    // ==================================
    // CUT GUIDE
    // ==================================

    float GetCurrentGuideFraction()
    {
        if (cutCount == 0)
            return guideFirstX;

        if (cutCount == 1)
            return guideSecondX;

        return guideThirdX;
    }

    Vector3 GetGuideWorldPosition()
    {
        float fraction = GetCurrentGuideFraction();

        float x = Mathf.Lerp(
            originalRootBounds.min.x,
            originalRootBounds.max.x,
            fraction
        );

        float y =
            originalRootBounds.center.y + guideYOffset;

        return new Vector3(
            x,
            y,
            transform.position.z
        );
    }

    void PositionCutGuide()
    {
        if (cutGuide == null || !hasRootBounds)
            return;

        Vector3 guideWorld = GetGuideWorldPosition();

        cutGuide.transform.position = new Vector3(
            guideWorld.x,
            guideWorld.y,
            cutGuide.transform.position.z
        );

        cutGuide.enabled =
            isSelected && !isGround;
    }

    void UpdateCutGuide()
    {
        if (cutGuide == null)
            return;

        if (!isSelected || isGround || !hasRootBounds)
        {
            cutGuide.enabled = false;
            return;
        }

        if (!cutGuide.enabled)
            cutGuide.enabled = true;

        float pulse =
            (Mathf.Sin(Time.time * guidePulseSpeed) + 1f)
            * 0.5f;

        Color color = guideOriginalColor;

        color.a = Mathf.Lerp(
            guideMinAlpha,
            guideMaxAlpha,
            pulse
        );

        cutGuide.color = color;
    }

    // ==================================
    // ROOT CUTTING
    // ==================================

    void HandleCutting()
    {
        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isCutting = processingArea.OverlapPoint(mouse);
            cutStartMousePosition = mouse;
        }

        if (isCutting &&
            Mouse.current.leftButton.isPressed)
        {
            if (knifeRenderer != null)
                knifeRenderer.enabled = true;

            if (cuttingKnife != null)
            {
                cuttingKnife.position = new Vector3(
                    mouse.x,
                    mouse.y,
                    knifeZ
                );
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (isCutting)
                TryCutRoot(cutStartMousePosition, mouse);

            isCutting = false;
            HideKnife();
        }
    }

    void TryCutRoot(Vector3 start, Vector3 end)
    {
        if (cutCount >= 3 || !hasRootBounds)
            return;

        Vector2 movement = end - start;

        bool verticalSwipe =
            Mathf.Abs(movement.y) >= minimumCutDistance &&
            Mathf.Abs(movement.y) >=
            Mathf.Abs(movement.x) * 0.7f;

        if (!verticalSwipe)
            return;

        float deltaY = end.y - start.y;

        if (Mathf.Abs(deltaY) < 0.0001f)
            return;

        // Find where the mouse stroke crosses
        // the vertical center of the root.
        float t =
            (originalRootBounds.center.y - start.y) /
            deltaY;

        if (t < 0f || t > 1f)
            return;

        float strokeX = Mathf.Lerp(start.x, end.x, t);

        float correctX =
            GetGuideWorldPosition().x;

        float tolerance =
            originalRootBounds.size.x * cutTolerance;

        // The cut is accepted only near the
        // glowing dashed guide.
        if (Mathf.Abs(strokeX - correctX) > tolerance)
            return;

        // Require the stroke to actually pass
        // through a meaningful part of the root.
        float minimumVerticalCoverage =
            originalRootBounds.size.y * 0.20f;

        if (Mathf.Abs(movement.y) <
            minimumVerticalCoverage)
            return;

        cutCount++;

        if (cutCount == 1)
        {
            if (rootCut1 != null)
                spriteRenderer.sprite = rootCut1;

            PositionCutGuide();
        }
        else if (cutCount == 2)
        {
            if (rootCut2 != null)
                spriteRenderer.sprite = rootCut2;

            PositionCutGuide();
        }
        else
        {
            CreateFinalRootSlices();
            FinishProcessing();
        }

        Debug.Log(
            name + ": root cut " + cutCount + "/3"
        );
    }

    void CreateFinalRootSlices()
    {
        if (rootSlices == null ||
            rootSlices.Length != 4)
            return;

        float totalWidth = originalRootBounds.size.x;
        float slotWidth = totalWidth / 4f;

        float desiredWidth =
            slotWidth * sliceWidthMultiplier *
            rootSliceSize;

        float desiredHeight =
            originalRootBounds.size.y *
            rootSliceSize;

        for (int i = 0; i < 4; i++)
        {
            Sprite sliceSprite = rootSlices[i];

            if (sliceSprite == null)
            {
                Debug.LogWarning(
                    name + ": Root_Slice" + (i + 1) +
                    " is missing."
                );
                continue;
            }

            GameObject slice = new GameObject(
                name + "_Slice" + (i + 1)
            );

            SpriteRenderer renderer =
                slice.AddComponent<SpriteRenderer>();

            renderer.sprite = sliceSprite;

            renderer.sortingLayerID =
                spriteRenderer.sortingLayerID;

            renderer.sortingOrder =
                spriteRenderer.sortingOrder + 1;

            // Fit each separately cropped sprite
            // inside one of the four root slots.
            Vector2 size = sliceSprite.bounds.size;

            float scaleX =
                desiredWidth / Mathf.Max(0.001f, size.x);

            float scaleY =
                desiredHeight / Mathf.Max(0.001f, size.y);

            float scale =
                Mathf.Min(scaleX, scaleY);

            slice.transform.localScale =
                Vector3.one * scale;

            float centerX =
                originalRootBounds.min.x +
                slotWidth * (i + 0.5f);

            slice.transform.position = new Vector3(
                centerX,
                originalRootBounds.center.y,
                transform.position.z
            );

            // Compensate for the sprite's own pivot
            // so its visible bounds occupy the slot.
            Bounds displayedBounds = renderer.bounds;

            Vector3 correction =
                slice.transform.position -
                displayedBounds.center;

            slice.transform.position += correction;

            pieces.Add(slice);
            pieceStartPositions.Add(
                slice.transform.position
            );
        }

        spriteRenderer.enabled = false;

        if (cutGuide != null)
            cutGuide.enabled = false;
    }

    void HideKnife()
    {
        if (knifeRenderer != null)
            knifeRenderer.enabled = false;
    }

    // ==================================
    // FLOWER PETALS
    // ==================================

    void HandlePetalPicking()
    {
        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            foreach (Collider2D petal in petals)
            {
                if (petal == null || !petal.enabled)
                    continue;

                if (petal.OverlapPoint(mouse))
                {
                    activePetal = petal.transform;
                    petalStartPosition = activePetal.position;
                    petalDragOffset =
                        activePetal.position - mouse;
                    break;
                }
            }
        }

        if (activePetal == null)
            return;

        if (Mouse.current.leftButton.isPressed)
        {
            Vector3 target = mouse + petalDragOffset;

            activePetal.position = new Vector3(
                target.x,
                target.y,
                petalStartPosition.z
            );
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            float distance = Vector2.Distance(
                petalStartPosition,
                activePetal.position
            );

            if (distance >= petalPullDistance)
                DetachPetal();
            else
                activePetal.position = petalStartPosition;

            activePetal = null;
        }
    }

    void DetachPetal()
    {
        if (activePetal == null)
            return;

        Transform detached = activePetal;

        Collider2D col =
            detached.GetComponent<Collider2D>();

        if (col != null)
        {
            petals.Remove(col);
            col.enabled = false;
        }

        detached.SetParent(null, true);

        Vector3 center = processingArea.bounds.center;
        Vector2 offset =
            Random.insideUnitCircle * 0.25f;

        detached.position = new Vector3(
            center.x + offset.x,
            center.y + offset.y,
            detached.position.z
        );

        detached.rotation = Quaternion.Euler(
            0f,
            0f,
            Random.Range(-80f, 80f)
        );

        pieces.Add(detached.gameObject);
        pieceStartPositions.Add(detached.position);

        if (petals.Count == 0)
            FinishProcessing();
    }

    // ==================================
    // GRINDING
    // ==================================

    void HandleGrinding(bool insideArea, Vector3 mouse)
    {
        Vector2 screen =
            Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isProcessing =
                insideArea &&
                ingredientCollider.OverlapPoint(mouse);

            lastMousePosition = screen;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            isProcessing = false;

        if (!isProcessing ||
            !Mouse.current.leftButton.isPressed)
            return;

        float movement = Vector2.Distance(
            screen,
            lastMousePosition
        );

        if (insideArea && movement > movementThreshold)
        {
            grindProgress += movement * 0.01f;

            float progress = Mathf.Clamp01(
                grindProgress /
                Mathf.Max(0.1f, requiredMovement)
            );

            ApplyProcessingVisuals(progress);

            if (progress >= 1f)
                FinishProcessing();
        }

        lastMousePosition = screen;
    }

    // ==================================
    // BERRY
    // ==================================

    void ApplyProcessingVisuals(float progress)
    {
        Vector3 targetScale = originalScale;

        if (processingType == ProcessingType.Grinding)
        {
            targetScale = originalScale * 0.4f;
        }
        else if (processingType == ProcessingType.Crushing)
        {
            targetScale = new Vector3(
                originalScale.x * 1.4f,
                originalScale.y * 0.25f,
                originalScale.z
            );

            UpdateBerryJuice(progress);
        }

        transform.localScale = Vector3.Lerp(
            originalScale,
            targetScale,
            progress
        );

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(
                originalColor,
                originalColor * 0.6f,
                progress
            );
        }
    }

    void UpdateBerryJuice(float progress)
    {
        if (berryJuice == null)
            return;

        berryJuice.SetActive(true);

        float expansion = Mathf.Lerp(
            0.15f,
            juiceExpansion,
            progress
        );

        berryJuice.transform.localScale =
            new Vector3(
                originalJuiceScale.x * expansion,
                originalJuiceScale.y * expansion,
                originalJuiceScale.z
            );

        SpriteRenderer juiceRenderer =
            berryJuice.GetComponent<SpriteRenderer>();

        if (juiceRenderer != null)
        {
            Color color = juiceRenderer.color;

            color.a = Mathf.Lerp(
                0.3f,
                0.9f,
                progress
            );

            juiceRenderer.color = color;
        }
    }

    // ==================================
    // FINISH PROCESSING
    // ==================================

    void FinishProcessing()
    {
        isGround = true;
        isProcessing = false;
        isCutting = false;

        HideKnife();

        if (processingType == ProcessingType.PetalPicking)
        {
            Transform center =
                transform.Find("FlowerCenter");

            if (center != null)
                center.gameObject.SetActive(false);
        }
        else if (processingType != ProcessingType.Cutting)
        {
            CreatePieces();

            if (processingType == ProcessingType.Crushing &&
                berryJuice != null)
            {
                berryJuice.transform.SetParent(null, true);
                pieces.Add(berryJuice);
                pieceStartPositions.Add(
                    berryJuice.transform.position
                );
            }
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (cutGuide != null &&
            processingType == ProcessingType.Cutting)
        {
            cutGuide.enabled = false;
        }
    }

    void CreatePieces()
    {
        if (spriteRenderer == null)
            return;

        for (int i = 0; i < 8; i++)
        {
            GameObject piece =
                new GameObject(name + "_Piece");

            SpriteRenderer renderer =
                piece.AddComponent<SpriteRenderer>();

            renderer.sprite = spriteRenderer.sprite;
            renderer.color = originalColor * 0.8f;

            renderer.sortingLayerID =
                spriteRenderer.sortingLayerID;

            renderer.sortingOrder =
                spriteRenderer.sortingOrder + 1;

            Vector2 offset =
                Random.insideUnitCircle * 0.18f;

            piece.transform.position =
                transform.position +
                new Vector3(offset.x, offset.y, 0f);

            float size =
                Random.Range(0.10f, 0.18f);

            float width = size;
            float height = size;

            if (processingType == ProcessingType.Crushing)
                height *= 0.6f;

            piece.transform.localScale =
                new Vector3(width, height, 1f);

            pieces.Add(piece);
            pieceStartPositions.Add(piece.transform.position);
        }
    }

    // ==================================
    // TRANSFER TO CAULDRON
    // ==================================

    void HandleTransfer()
    {
        if (pieces.Count == 0)
            return;

        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            IsMouseOverPieces(mouse))
        {
            isTransferring = true;

            transferOffset =
                pieces[0].transform.position - mouse;
        }

        if (isTransferring &&
            Mouse.current.leftButton.isPressed)
        {
            Vector3 target = mouse + transferOffset;

            Vector3 movement =
                target - pieces[0].transform.position;

            foreach (GameObject piece in pieces)
            {
                if (piece != null)
                    piece.transform.position += movement;
            }
        }

        if (isTransferring &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isTransferring = false;

            if (cauldronCollider != null &&
                cauldronCollider.OverlapPoint(mouse))
            {
                AddToCauldron();
            }
            else
            {
                ResetPieces();
            }
        }
    }

    bool IsMouseOverPieces(Vector3 mouse)
    {
        foreach (GameObject piece in pieces)
        {
            if (piece == null)
                continue;

            SpriteRenderer renderer =
                piece.GetComponent<SpriteRenderer>();

            if (renderer != null &&
                renderer.bounds.Contains(
                    new Vector3(
                        mouse.x,
                        mouse.y,
                        renderer.bounds.center.z
                    )
                ))
            {
                return true;
            }

            if (Vector2.Distance(
                mouse,
                piece.transform.position
            ) < 0.35f)
            {
                return true;
            }
        }

        return false;
    }

    void ResetPieces()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] != null)
            {
                pieces[i].transform.position =
                    pieceStartPositions[i];
            }
        }
    }

    void AddToCauldron()
    {
        isAddedToCauldron = true;

        foreach (GameObject piece in pieces)
        {
            if (piece != null)
                Destroy(piece);
        }

        pieces.Clear();
        pieceStartPositions.Clear();

        if (ingredientManager != null)
        {
            ingredientManager.AddIngredientToCauldron(
                gameObject.name
            );
        }

        Debug.Log(name + " added to cauldron!");
    }

   

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
