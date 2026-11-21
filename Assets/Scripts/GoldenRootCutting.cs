
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GoldenRootCutting : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private Collider2D rootCollider;
    [SerializeField] private Collider2D cuttingBoard;
    [SerializeField] private Collider2D cauldronCollider;
    [SerializeField] private IngredientManager ingredientManager;
    [SerializeField] private Transform cuttingKnife;
    [SerializeField] private SpriteRenderer cutGuide;

    [Header("Sprites")]
    [SerializeField] private Sprite wholeSprite;

    [Tooltip("Two sprites after the first cut")]
    [SerializeField]
    private Sprite[] firstCutSprites =
        new Sprite[2];

    [Tooltip("Three sprites after the second cut")]
    [SerializeField]
    private Sprite[] secondCutSprites =
        new Sprite[3];

    [Tooltip("Four pieces before final rotation")]
    [SerializeField]
    private Sprite[] thirdCutSprites =
        new Sprite[4];

    [Tooltip("Four final sprites from the bottom row")]
    [SerializeField]
    private Sprite[] finalSliceSprites =
        new Sprite[4];

    [Header("Cut Guide Positions")]
    [Range(0f, 1f)]
    [SerializeField] private float firstCutX = 0.28f;

    [Range(0f, 1f)]
    [SerializeField] private float secondCutX = 0.49f;

    [Range(0f, 1f)]
    [SerializeField] private float thirdCutX = 0.71f;

    [SerializeField] private float guideYOffset = 0f;

    [Header("Guide Pulse")]
    [Range(0f, 1f)]
    [SerializeField] private float guideMinAlpha = 0.35f;

    [Range(0f, 1f)]
    [SerializeField] private float guideMaxAlpha = 1f;

    [SerializeField] private float guidePulseSpeed = 2.2f;

    [Header("Cut Detection")]
    [SerializeField] private float minimumCutDistance = 0.25f;

    [Range(0.01f, 0.25f)]
    [SerializeField] private float cutToleranceFraction = 0.08f;

    [Header("Piece Layout")]
    [SerializeField] private float firstCutSpread = 1.08f;
    [SerializeField] private float secondCutSpread = 1.12f;
    [SerializeField] private float thirdCutSpread = 1.18f;
    [SerializeField] private float finalCutSpread = 1.18f;
    [SerializeField] private float pieceGapFraction = 0.045f;

    [Header("Smooth Final Transition")]
    [SerializeField] private float finalTransitionDuration = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float crossfadeStart = 0.25f;

    [Range(0f, 1f)]
    [SerializeField] private float crossfadeEnd = 0.85f;

    [SerializeField] private float finalOutwardOffset = 0.06f;
    [SerializeField] private float finalVerticalJitter = 0.02f;

    [Header("Final Piece Rotation")]
    [SerializeField] private float firstPieceRotation = -6f;
    [SerializeField] private float secondPieceRotation = -3f;
    [SerializeField] private float thirdPieceRotation = 4f;
    [SerializeField] private float fourthPieceRotation = 7f;

    private enum RootStage
    {
        Whole,
        FirstCut,
        SecondCut,
        AnimatingToFinal,
        FinalReady
    }

    private RootStage stage = RootStage.Whole;

    private Camera mainCamera;
    private SpriteRenderer knifeRenderer;

    private Vector3 startPosition;
    private Vector3 boardPosition;
    private Vector3 dragOffset;
    private Vector3 transferDragOffset;
    private Vector3 cutStartMouse;

    private bool isDraggingRoot;
    private bool isDraggingFinalGroup;
    private bool isSelected;
    private bool isAddedToCauldron;
    private bool isCutting;
    private bool finalTransitionFinished;

    private float knifeZ;
    private float rootWorldWidth;
    private Color guideBaseColor;

    private readonly List<RootPiece> spawnedPieces =
        new List<RootPiece>();

    private class RootPiece
    {
        public GameObject objectRoot;
        public SpriteRenderer beforeRenderer;
        public SpriteRenderer afterRenderer;
        public Collider2D collider;
    }

    private struct LayoutData
    {
        public Vector3[] localPositions;
        public Vector3 localScale;
    }

    // =====================================
    // INITIALIZATION
    // =====================================

    void Start()
    {
        mainCamera = Camera.main;

        if (mainRenderer == null)
            mainRenderer = GetComponent<SpriteRenderer>();

        if (rootCollider == null)
            rootCollider = GetComponent<Collider2D>();

        if (mainRenderer != null && wholeSprite != null)
        {
            mainRenderer.sprite = wholeSprite;
            mainRenderer.enabled = true;
        }

        if (cuttingKnife != null)
        {
            knifeRenderer =
                cuttingKnife.GetComponent<SpriteRenderer>();

            knifeZ = cuttingKnife.position.z;
            HideKnife();
        }

        if (cutGuide != null)
        {
            guideBaseColor = cutGuide.color;
            cutGuide.enabled = false;
        }

        startPosition = transform.position;
    }

    void Update()
    {
        if (Mouse.current == null ||
            mainCamera == null ||
            isAddedToCauldron)
        {
            return;
        }

        if (!isSelected)
        {
            HandleDragToBoard();
            return;
        }

        UpdateGuidePulse();

        if (stage == RootStage.AnimatingToFinal)
            return;

        if (stage == RootStage.FinalReady)
        {
            if (finalTransitionFinished)
                HandleFinalTransfer();

            return;
        }

        HandleCutting();
    }

    void OnDisable()
    {
        HideKnife();
    }

    // =====================================
    // DRAG TO CUTTING BOARD
    // =====================================

    void HandleDragToBoard()
    {
        if (rootCollider == null || cuttingBoard == null)
            return;

        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            rootCollider.OverlapPoint(mouse))
        {
            isDraggingRoot = true;
            dragOffset = transform.position - mouse;
        }

        if (isDraggingRoot && Mouse.current.leftButton.isPressed)
        {
            Vector3 target = mouse + dragOffset;
            target.z = transform.position.z;

            transform.position = target;
        }

        if (isDraggingRoot &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDraggingRoot = false;

            bool onBoard =
                cuttingBoard.OverlapPoint(transform.position);

            if (onBoard &&
                ingredientManager != null &&
                ingredientManager.TrySelectIngredient(gameObject.name))
            {
                isSelected = true;

                boardPosition = cuttingBoard.bounds.center;
                boardPosition.z = transform.position.z;

                transform.position = boardPosition;

                if (mainRenderer != null && wholeSprite != null)
                {
                    mainRenderer.sprite = wholeSprite;
                    mainRenderer.enabled = true;

                    rootWorldWidth =
                        mainRenderer.bounds.size.x;
                }

                stage = RootStage.Whole;
                ShowGuideAtCurrentCut();
            }
            else
            {
                transform.position = startPosition;
            }
        }
    }

    // =====================================
    // CUTTING
    // =====================================

    void HandleCutting()
    {
        if (cuttingBoard == null)
            return;

        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isCutting = cuttingBoard.OverlapPoint(mouse);
            cutStartMouse = mouse;
        }

        if (isCutting && Mouse.current.leftButton.isPressed)
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

        if (isCutting &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            TryPerformCut(cutStartMouse, mouse);

            isCutting = false;
            HideKnife();
        }
    }

    void TryPerformCut(Vector3 start, Vector3 end)
    {
        Vector2 movement = end - start;

        bool verticalSwipe =
            Mathf.Abs(movement.y) >= minimumCutDistance &&
            Mathf.Abs(movement.y) >=
            Mathf.Abs(movement.x) * 0.7f;

        if (!verticalSwipe)
            return;

        Vector3 guidePosition =
            GetCurrentGuideWorldPosition();

        float deltaY = end.y - start.y;

        if (Mathf.Abs(deltaY) < 0.0001f)
            return;

        float t =
            (guidePosition.y - start.y) / deltaY;

        if (t < 0f || t > 1f)
            return;

        float crossingX =
            Mathf.Lerp(start.x, end.x, t);

        float tolerance =
            rootWorldWidth * cutToleranceFraction;

        if (Mathf.Abs(crossingX - guidePosition.x) >
            tolerance)
        {
            return;
        }

        ApplyNextCut();
    }

    void ApplyNextCut()
    {
        if (stage == RootStage.Whole)
        {
            if (!HasSprites(firstCutSprites, 2))
                return;

            ShowStagePieces(
                firstCutSprites,
                firstCutSpread
            );

            stage = RootStage.FirstCut;

            ShowGuideAtCurrentCut();
            Debug.Log("GoldenRoot: cut 1/3");

            return;
        }

        if (stage == RootStage.FirstCut)
        {
            if (!HasSprites(secondCutSprites, 3))
                return;

            ShowStagePieces(
                secondCutSprites,
                secondCutSpread
            );

            stage = RootStage.SecondCut;

            ShowGuideAtCurrentCut();
            Debug.Log("GoldenRoot: cut 2/3");

            return;
        }

        if (stage == RootStage.SecondCut)
        {
            if (!HasSprites(thirdCutSprites, 4) ||
                !HasSprites(finalSliceSprites, 4))
            {
                Debug.LogError(
                    "GoldenRoot: assign all third-cut " +
                    "and final slice sprites."
                );

                return;
            }

            ShowStagePieces(
                thirdCutSprites,
                thirdCutSpread
            );

            stage = RootStage.AnimatingToFinal;

            if (cutGuide != null)
                cutGuide.enabled = false;

            StartCoroutine(AnimateIntoFinalState());

            Debug.Log("GoldenRoot: cut 3/3");
        }
    }

    // =====================================
    // CREATING STAGE PIECES
    // =====================================

    void ShowStagePieces(
        Sprite[] sprites,
        float spreadMultiplier)
    {
        ClearSpawnedPieces();

        if (mainRenderer != null)
            mainRenderer.enabled = false;

        LayoutData layout = CalculateLayout(
            sprites,
            spreadMultiplier
        );

        for (int i = 0; i < sprites.Length; i++)
        {
            GameObject root = new GameObject(
                "RootPiece_" + (i + 1)
            );

            root.transform.SetParent(transform, false);

            root.transform.localPosition =
                layout.localPositions[i];

            root.transform.localScale =
                layout.localScale;

            RootPiece piece = new RootPiece();

            piece.objectRoot = root;

            piece.beforeRenderer =
                CreateVisualRenderer(
                    root.transform,
                    "Before",
                    sprites[i],
                    1f
                );

            // No collider is needed before
            // the player completes cutting.
            spawnedPieces.Add(piece);
        }
    }

    SpriteRenderer CreateVisualRenderer(
        Transform parent,
        string objectName,
        Sprite sprite,
        float alpha)
    {
        GameObject visual =
            new GameObject(objectName);

        visual.transform.SetParent(parent, false);

        // Align the visible center instead of
        // relying on different sprite pivots.
        visual.transform.localPosition =
            -sprite.bounds.center;

        visual.transform.localRotation =
            Quaternion.identity;

        visual.transform.localScale =
            Vector3.one;

        SpriteRenderer renderer =
            visual.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;

        if (mainRenderer != null)
        {
            renderer.sortingLayerID =
                mainRenderer.sortingLayerID;

            renderer.sortingOrder =
                mainRenderer.sortingOrder + 1;

            Color color = mainRenderer.color;
            color.a *= alpha;

            renderer.color = color;
        }

        return renderer;
    }

    LayoutData CalculateLayout(
        Sprite[] sprites,
        float spreadMultiplier)
    {
        LayoutData result = new LayoutData();

        int count = sprites.Length;

        result.localPositions =
            new Vector3[count];

        float totalSpriteWidth = 0f;

        foreach (Sprite sprite in sprites)
        {
            if (sprite != null)
                totalSpriteWidth += sprite.bounds.size.x;
        }

        float availableWidth =
            rootWorldWidth * spreadMultiplier;

        float gap =
            rootWorldWidth * pieceGapFraction;

        float remainingWidth = Mathf.Max(
            0.01f,
            availableWidth - gap * (count - 1)
        );

        float worldScale =
            remainingWidth /
            Mathf.Max(0.001f, totalSpriteWidth);

        float parentScaleX = Mathf.Max(
            0.001f,
            Mathf.Abs(transform.lossyScale.x)
        );

        float parentScaleY = Mathf.Max(
            0.001f,
            Mathf.Abs(transform.lossyScale.y)
        );

        result.localScale = new Vector3(
            worldScale / parentScaleX,
            worldScale / parentScaleY,
            1f
        );

        float totalContentWidth =
            totalSpriteWidth * worldScale +
            gap * (count - 1);

        float cursor =
            -totalContentWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float width =
                sprites[i].bounds.size.x *
                worldScale;

            float centerX =
                cursor + width * 0.5f;

            result.localPositions[i] = new Vector3(
                centerX / parentScaleX,
                0f,
                0f
            );

            cursor += width + gap;
        }

        return result;
    }

    bool HasSprites(Sprite[] sprites, int expectedCount)
    {
        if (sprites == null ||
            sprites.Length != expectedCount)
        {
            return false;
        }

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
                return false;
        }

        return true;
    }

    // =====================================
    // SMOOTH FINAL ANIMATION
    // =====================================

    IEnumerator AnimateIntoFinalState()
    {
        finalTransitionFinished = false;

        int count = spawnedPieces.Count;

        LayoutData finalLayout =
            CalculateLayout(
                finalSliceSprites,
                finalCutSpread
            );

        Vector3[] startPositions =
            new Vector3[count];

        Vector3[] targetPositions =
            new Vector3[count];

        Vector3[] startScales =
            new Vector3[count];

        float[] targetRotations =
        {
            firstPieceRotation,
            secondPieceRotation,
            thirdPieceRotation,
            fourthPieceRotation
        };

        float parentScaleX = Mathf.Max(
            0.001f,
            Mathf.Abs(transform.lossyScale.x)
        );

        float parentScaleY = Mathf.Max(
            0.001f,
            Mathf.Abs(transform.lossyScale.y)
        );

        // Create transparent final sprites
        // without removing old sprites.
        for (int i = 0; i < count; i++)
        {
            RootPiece piece = spawnedPieces[i];

            Transform root =
                piece.objectRoot.transform;

            startPositions[i] =
                root.localPosition;

            startScales[i] =
                root.localScale;

            Vector3 target =
                finalLayout.localPositions[i];

            if (i == 0)
                target.x -= finalOutwardOffset / parentScaleX;

            if (i == count - 1)
                target.x += finalOutwardOffset / parentScaleX;

            if (i == 0)
                target.y -= finalVerticalJitter / parentScaleY;

            if (i == 1)
                target.y += finalVerticalJitter * 0.5f /
                    parentScaleY;

            if (i == 2)
                target.y -= finalVerticalJitter * 0.25f /
                    parentScaleY;

            if (i == 3)
                target.y -= finalVerticalJitter * 0.5f /
                    parentScaleY;

            targetPositions[i] = target;

            piece.afterRenderer =
                CreateVisualRenderer(
                    root,
                    "After",
                    finalSliceSprites[i],
                    0f
                );

            // Render the final sprite above the old one.
            piece.afterRenderer.sortingOrder =
                piece.beforeRenderer.sortingOrder + 1;
        }

        float elapsed = 0f;

        float duration = Mathf.Max(
            0.01f,
            finalTransitionDuration
        );

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / duration
            );

            // Smooth acceleration and deceleration.
            float movement = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            // Crossfade runs during a portion
            // of the movement instead of
            // swapping images in one frame.
            float fade = Mathf.InverseLerp(
                crossfadeStart,
                Mathf.Max(
                    crossfadeStart + 0.01f,
                    crossfadeEnd
                ),
                progress
            );

            fade = Mathf.SmoothStep(
                0f,
                1f,
                fade
            );

            for (int i = 0; i < count; i++)
            {
                RootPiece piece = spawnedPieces[i];

                Transform root =
                    piece.objectRoot.transform;

                root.localPosition = Vector3.Lerp(
                    startPositions[i],
                    targetPositions[i],
                    movement
                );

                root.localScale = Vector3.Lerp(
                    startScales[i],
                    finalLayout.localScale,
                    movement
                );

                float angle = Mathf.Lerp(
                    0f,
                    targetRotations[i],
                    movement
                );

                root.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        angle
                    );

                SetRendererAlpha(
                    piece.beforeRenderer,
                    1f - fade
                );

                SetRendererAlpha(
                    piece.afterRenderer,
                    fade
                );
            }

            yield return null;
        }

        // Final exact positions and sprites.
        for (int i = 0; i < count; i++)
        {
            RootPiece piece = spawnedPieces[i];

            Transform root =
                piece.objectRoot.transform;

            root.localPosition =
                targetPositions[i];

            root.localScale =
                finalLayout.localScale;

            root.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    targetRotations[i]
                );

            SetRendererAlpha(
                piece.afterRenderer,
                1f
            );

            // Remove the old image.
            if (piece.beforeRenderer != null)
            {
                Destroy(
                    piece.beforeRenderer.gameObject
                );
            }

            piece.beforeRenderer = null;

            // Add a collider for dragging
            // the finished slices to the cauldron.
            BoxCollider2D collider =
                piece.objectRoot.AddComponent<BoxCollider2D>();

            collider.isTrigger = true;

            // Use the finished sprite's visible bounds.
            Sprite finalSprite = finalSliceSprites[i];

            collider.size = finalSprite.bounds.size;
            collider.offset = Vector2.zero;

            piece.collider = collider;
        }

        stage = RootStage.FinalReady;
        finalTransitionFinished = true;

        Debug.Log(
            "GoldenRoot: smooth final transition complete!"
        );
    }

    void SetRendererAlpha(
        SpriteRenderer renderer,
        float alpha)
    {
        if (renderer == null)
            return;

        Color color = renderer.color;

        float baseAlpha = mainRenderer != null
            ? mainRenderer.color.a
            : 1f;

        color.a = Mathf.Clamp01(alpha) * baseAlpha;

        renderer.color = color;
    }

    // =====================================
    // FINAL TRANSFER TO CAULDRON
    // =====================================

    void HandleFinalTransfer()
    {
        if (spawnedPieces.Count == 0 ||
            cauldronCollider == null)
        {
            return;
        }

        Vector3 mouse = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            IsMouseOverFinalPiece(mouse))
        {
            isDraggingFinalGroup = true;

            transferDragOffset =
                transform.position - mouse;
        }

        if (isDraggingFinalGroup &&
            Mouse.current.leftButton.isPressed)
        {
            Vector3 target =
                mouse + transferDragOffset;

            target.z = transform.position.z;

            transform.position = target;
        }

        if (isDraggingFinalGroup &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDraggingFinalGroup = false;

            if (cauldronCollider.OverlapPoint(mouse))
            {
                AddToCauldron();
            }
            else
            {
                transform.position = boardPosition;
            }
        }
    }

    bool IsMouseOverFinalPiece(Vector3 mouse)
    {
        foreach (RootPiece piece in spawnedPieces)
        {
            if (piece.objectRoot == null)
                continue;

            // Check the actual visible sprite.
            SpriteRenderer renderer =
                piece.afterRenderer;

            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;

                if (mouse.x >= bounds.min.x &&
                    mouse.x <= bounds.max.x &&
                    mouse.y >= bounds.min.y &&
                    mouse.y <= bounds.max.y)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void AddToCauldron()
    {
        if (isAddedToCauldron)
            return;

        isAddedToCauldron = true;

        if (ingredientManager != null)
        {
            ingredientManager.AddIngredientToCauldron(
                gameObject.name
            );
        }

        ClearSpawnedPieces();

        if (mainRenderer != null)
            mainRenderer.enabled = false;

        if (cutGuide != null)
            cutGuide.enabled = false;

        Debug.Log("GoldenRoot added to cauldron!");
    }

    // =====================================
    // PULSING CUT GUIDE
    // =====================================

    void ShowGuideAtCurrentCut()
    {
        if (cutGuide == null || !isSelected)
            return;

        cutGuide.transform.position =
            GetCurrentGuideWorldPosition();

        cutGuide.enabled = true;
    }

    void UpdateGuidePulse()
    {
        if (cutGuide == null)
            return;

        if (!isSelected ||
            stage == RootStage.AnimatingToFinal ||
            stage == RootStage.FinalReady)
        {
            cutGuide.enabled = false;
            return;
        }

        cutGuide.transform.position =
            GetCurrentGuideWorldPosition();

        cutGuide.enabled = true;

        float pulse =
            (Mathf.Sin(
                Time.time * guidePulseSpeed
            ) + 1f) * 0.5f;

        Color color = guideBaseColor;

        color.a = Mathf.Lerp(
            guideMinAlpha,
            guideMaxAlpha,
            pulse
        );

        cutGuide.color = color;
    }

    Vector3 GetCurrentGuideWorldPosition()
    {
        float fraction = firstCutX;

        if (stage == RootStage.FirstCut)
            fraction = secondCutX;

        if (stage == RootStage.SecondCut)
            fraction = thirdCutX;

        float x =
            boardPosition.x -
            rootWorldWidth * 0.5f +
            rootWorldWidth * fraction;

        float y =
            boardPosition.y + guideYOffset;

        float z = cutGuide != null
            ? cutGuide.transform.position.z
            : transform.position.z;

        return new Vector3(x, y, z);
    }

    // =====================================
    // HELPERS
    // =====================================

    void HideKnife()
    {
        if (knifeRenderer != null)
            knifeRenderer.enabled = false;
    }

    void ClearSpawnedPieces()
    {
        foreach (RootPiece piece in spawnedPieces)
        {
            if (piece.objectRoot != null)
                Destroy(piece.objectRoot);
        }

        spawnedPieces.Clear();
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
