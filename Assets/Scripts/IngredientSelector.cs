
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

    [Header("Mushroom")]
    public float mushroomPullDistance = 0.3f;

    [Header("Berry Juice")]
    public GameObject berryJuice;
    public float juiceExpansion = 1.5f;

    [Header("Cutting")]
    public Transform cuttingKnife;
    public int requiredCuts = 3;
    public float minimumCutDistance = 0.25f;

    [Range(0.5f, 1f)]
    public float sliceWidthMultiplier = 0.80f;

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

    private Transform activePetal;
    private Vector3 petalStartPosition;
    private Vector3 petalDragOffset;

    private Transform activeMushroomPart;
    private Vector3 mushroomStartPosition;
    private Vector3 mushroomDragOffset;

    private Vector3 transferOffset;

    private readonly List<Collider2D> petals =
        new List<Collider2D>();

    private readonly List<Transform> rootSegments =
        new List<Transform>();

    private readonly List<Collider2D> mushroomParts =
        new List<Collider2D>();

    private readonly List<GameObject> pieces =
        new List<GameObject>();

    private readonly List<Vector3> pieceStartPositions =
        new List<Vector3>();

    public bool IsSelected => isSelected;
    public bool IsGround => isGround;
    public bool IsAddedToCauldron => isAddedToCauldron;

    // =====================================
    // INITIALIZATION
    // =====================================

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

        switch (processingType)
        {
            case ProcessingType.PetalPicking:
                FindFlowerPetals();
                break;

            case ProcessingType.Cutting:
                FindRootSegments();
                InitializeKnife();
                break;

            case ProcessingType.Breaking:
                FindMushroomParts();
                break;
        }
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
    }

    // =====================================
    // FIND CHILD OBJECTS
    // =====================================

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

    void FindRootSegments()
    {
        rootSegments.Clear();

        for (int i = 1; i <= 4; i++)
        {
            Transform segment =
                transform.Find("RootSegment" + i);

            if (segment != null)
                rootSegments.Add(segment);
        }

        if (rootSegments.Count > 0 &&
            spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (rootSegments.Count >= 2)
            requiredCuts = rootSegments.Count - 1;

        if (rootSegments.Count != 4)
        {
            Debug.LogWarning(
                name + ": Expected RootSegment1-4."
            );
        }
    }

    void FindMushroomParts()
    {
        mushroomParts.Clear();

        string[] partNames =
        {
            "MushroomLeft",
            "MushroomRight"
        };

        foreach (string partName in partNames)
        {
            Transform part = transform.Find(partName);

            if (part == null)
            {
                Debug.LogWarning(
                    name + ": Missing " + partName
                );
                continue;
            }

            Collider2D col =
                part.GetComponent<Collider2D>();

            if (col != null)
            {
                mushroomParts.Add(col);
            }
            else
            {
                Debug.LogWarning(
                    partName + " needs a Collider2D."
                );
            }
        }

        if (mushroomParts.Count > 0 &&
            spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    void InitializeKnife()
    {
        if (cuttingKnife == null)
            return;

        knifeRenderer =
            cuttingKnife.GetComponent<SpriteRenderer>();

        knifeZ = cuttingKnife.position.z;

        HideKnife();
    }

    // =====================================
    // DRAG INGREDIENT TO WORKING AREA
    // =====================================

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
                    petal.OverlapPoint(mouse))
                {
                    clickedIngredient = true;
                    break;
                }
            }
        }

        if (processingType == ProcessingType.Cutting)
        {
            foreach (Transform segment in rootSegments)
            {
                if (segment == null)
                    continue;

                Collider2D col =
                    segment.GetComponent<Collider2D>();

                if (col != null &&
                    col.OverlapPoint(mouse))
                {
                    clickedIngredient = true;
                    break;
                }
            }
        }

        if (processingType == ProcessingType.Breaking)
        {
            foreach (Collider2D part in mushroomParts)
            {
                if (part != null &&
                    part.OverlapPoint(mouse))
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
                processingArea.OverlapPoint(
                    transform.position
                );

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
            }
            else
            {
                transform.position = startPosition;
            }
        }
    }

    // =====================================
    // PROCESSING MODES
    // =====================================

    void HandleProcessing()
    {
        switch (processingType)
        {
            case ProcessingType.PetalPicking:
                HandlePetalPicking();
                return;

            case ProcessingType.Cutting:
                HandleCutting();
                return;

            case ProcessingType.Breaking:
                HandleMushroomBreaking();
                return;
        }

        Vector3 mouse = GetMouseWorldPosition();

        bool insideArea =
            processingArea.OverlapPoint(mouse);

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

    // =====================================
    // MUSHROOM: ONE BREAK = TWO HALVES
    // =====================================

    void HandleMushroomBreaking()
    {
        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            foreach (Collider2D part in mushroomParts)
            {
                if (part == null || !part.enabled)
                    continue;

                if (part.OverlapPoint(mouse))
                {
                    activeMushroomPart = part.transform;

                    mushroomStartPosition =
                        activeMushroomPart.position;

                    mushroomDragOffset =
                        activeMushroomPart.position - mouse;

                    break;
                }
            }
        }

        if (activeMushroomPart == null)
            return;

        if (Mouse.current.leftButton.isPressed)
        {
            Vector3 target = mouse + mushroomDragOffset;

            activeMushroomPart.position = new Vector3(
                target.x,
                target.y,
                mushroomStartPosition.z
            );
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            float distance = Vector2.Distance(
                mushroomStartPosition,
                activeMushroomPart.position
            );

            if (distance >= mushroomPullDistance)
            {
                BreakMushroom();
            }
            else
            {
                activeMushroomPart.position =
                    mushroomStartPosition;
            }

            activeMushroomPart = null;
        }
    }

    void BreakMushroom()
    {
        if (activeMushroomPart == null)
            return;

        // One pull separates the entire mushroom.
        // Both halves become prepared immediately.

        foreach (Collider2D part in mushroomParts)
        {
            if (part == null)
                continue;

            Transform half = part.transform;

            // Preserve current world position and scale.
            Vector3 position = half.position;

            half.SetParent(null, true);
            half.position = position;

            // Disable separate clicking.
            part.enabled = false;

            // Keep both halves on the working surface.
            if (!processingArea.OverlapPoint(half.position))
            {
                Vector3 center =
                    processingArea.bounds.center;

                Vector2 offset =
                    Random.insideUnitCircle * 0.15f;

                half.position = new Vector3(
                    center.x + offset.x,
                    center.y + offset.y,
                    half.position.z
                );
            }

            // Only a small rotation for a broken look.
            half.Rotate(
                0f,
                0f,
                Random.Range(-8f, 8f)
            );

            pieces.Add(half.gameObject);
            pieceStartPositions.Add(half.position);
        }

        mushroomParts.Clear();

        FinishProcessing();

        Debug.Log(
            name + ": mushroom broken! 1/1"
        );
    }

    // =====================================
    // ROOT CUTTING
    // =====================================

    void HandleCutting()
    {
        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isCutting =
                processingArea.OverlapPoint(mouse);

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
            {
                TryCutRoot(
                    cutStartMousePosition,
                    mouse
                );
            }

            isCutting = false;
            HideKnife();
        }
    }

    void TryCutRoot(Vector3 start, Vector3 end)
    {
        if (rootSegments.Count <= 1)
            return;

        Vector2 movement = end - start;

        bool verticalSwipe =
            Mathf.Abs(movement.y) >= minimumCutDistance &&
            Mathf.Abs(movement.y) >=
                Mathf.Abs(movement.x) * 0.7f;

        if (!verticalSwipe)
            return;

        if (!TryGetRootBounds(out Bounds rootBounds))
            return;

        if (!TryFindCutPosition(
            start,
            end,
            rootBounds,
            out Vector3 cutPosition))
        {
            return;
        }

        Transform nearestSegment = null;
        float closestDistance = float.MaxValue;

        foreach (Transform segment in rootSegments)
        {
            if (segment == null)
                continue;

            SpriteRenderer renderer =
                segment.GetComponent<SpriteRenderer>();

            if (renderer == null)
                continue;

            float distance = Mathf.Abs(
                renderer.bounds.center.x - cutPosition.x
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestSegment = segment;
            }
        }

        if (nearestSegment != null)
            DetachRootSegment(nearestSegment);
    }

    bool TryGetRootBounds(out Bounds result)
    {
        result = new Bounds();
        bool found = false;

        foreach (Transform segment in rootSegments)
        {
            if (segment == null)
                continue;

            SpriteRenderer renderer =
                segment.GetComponent<SpriteRenderer>();

            if (renderer == null)
                continue;

            if (!found)
            {
                result = renderer.bounds;
                found = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    bool TryFindCutPosition(
        Vector3 start,
        Vector3 end,
        Bounds rootBounds,
        out Vector3 cutPosition)
    {
        cutPosition = Vector3.zero;

        Rect rootRect = new Rect(
            rootBounds.min.x - 0.10f,
            rootBounds.min.y - 0.06f,
            rootBounds.size.x + 0.20f,
            rootBounds.size.y + 0.12f
        );

        const int samples = 60;

        bool found = false;
        float bestDistance = float.MaxValue;

        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;

            Vector3 point =
                Vector3.Lerp(start, end, t);

            if (!rootRect.Contains(
                new Vector2(point.x, point.y)))
            {
                continue;
            }

            float distance = Mathf.Abs(
                point.y - rootBounds.center.y
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                cutPosition = point;
                found = true;
            }
        }

        return found;
    }

    void DetachRootSegment(Transform segment)
    {
        if (segment == null)
            return;

        rootSegments.Remove(segment);
        PrepareRootSlice(segment);

        cutCount++;

        Debug.Log(
            name + ": cut " +
            cutCount + "/" + requiredCuts
        );

        // Four segments require three cuts.
        // Final remaining segment becomes ready.
        if (rootSegments.Count == 1)
        {
            Transform finalSegment =
                rootSegments[0];

            rootSegments.Clear();

            PrepareRootSlice(finalSegment);
            FinishProcessing();
        }
    }

    void PrepareRootSlice(Transform segment)
    {
        if (segment == null)
            return;

        Vector3 position = segment.position;

        segment.SetParent(null, true);
        segment.position = position;

        Vector3 scale = segment.localScale;

        segment.localScale = new Vector3(
            scale.x * sliceWidthMultiplier,
            scale.y,
            scale.z
        );

        Collider2D col =
            segment.GetComponent<Collider2D>();

        if (col != null)
            col.enabled = false;

        pieces.Add(segment.gameObject);
        pieceStartPositions.Add(segment.position);
    }

    void HideKnife()
    {
        if (knifeRenderer != null)
            knifeRenderer.enabled = false;
    }

    // =====================================
    // FLOWER PETALS
    // =====================================

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

                    petalStartPosition =
                        activePetal.position;

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

        Vector3 center =
            processingArea.bounds.center;

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

    // =====================================
    // GRINDING
    // =====================================

    void HandleGrinding(
        bool insideArea,
        Vector3 mouse)
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

        if (insideArea &&
            movement > movementThreshold)
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

    // =====================================
    // BERRY VISUALS
    // =====================================

    void ApplyProcessingVisuals(float progress)
    {
        Vector3 targetScale = originalScale;

        switch (processingType)
        {
            case ProcessingType.Grinding:
                targetScale = originalScale * 0.4f;
                break;

            case ProcessingType.Crushing:
                targetScale = new Vector3(
                    originalScale.x * 1.4f,
                    originalScale.y * 0.25f,
                    originalScale.z
                );

                UpdateBerryJuice(progress);
                break;
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

    // =====================================
    // FINISH PROCESSING
    // =====================================

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
        else if (
            processingType != ProcessingType.Cutting &&
            processingType != ProcessingType.Breaking)
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

        Debug.Log(name + " fully processed!");
    }

    // =====================================
    // CREATE PIECES FOR GRINDING / BERRY
    // =====================================

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

    // =====================================
    // TRANSFER TO CAULDRON
    // =====================================

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
            if (piece != null &&
                Vector2.Distance(
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

    // =====================================
    // NEW INPUT SYSTEM
    // =====================================

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
