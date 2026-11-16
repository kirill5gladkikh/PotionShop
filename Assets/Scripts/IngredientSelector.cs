
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class IngredientSelector : MonoBehaviour
{
    [Header("Selection")]
    public Collider2D mortarCollider;
    public IngredientManager ingredientManager;

    [Header("Grinding")]
    public float requiredMovement = 8f;
    public float movementThreshold = 1f;

    [Header("Cauldron")]
    public Collider2D cauldronCollider;
    public SpriteRenderer potionLiquid;

    private Camera mainCamera;
    private Collider2D ingredientCollider;
    private SpriteRenderer spriteRenderer;

    private Vector3 startPosition;
    private Vector3 originalScale;
    private Vector3 offset;

    private bool isDragging;
    private bool isSelected;
    private bool isGrinding;
    private bool isGround;
    private bool isTransferring;
    private bool isAddedToCauldron;

    private float grindProgress;
    private Vector2 lastMousePosition;
    private Color originalColor;

    private List<GameObject> pieces =
        new List<GameObject>();

    private List<Vector3> pieceStartPositions =
        new List<Vector3>();

    private Vector3 transferOffset;

    public bool IsSelected => isSelected;
    public bool IsGround => isGround;
    public bool IsAddedToCauldron => isAddedToCauldron;

    void Start()
    {
        mainCamera = Camera.main;
        ingredientCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        startPosition = transform.position;
        originalScale = transform.localScale;
        originalColor = spriteRenderer.color;
    }

    void Update()
    {
        if (Mouse.current == null ||
            mainCamera == null ||
            ingredientCollider == null)
            return;

        if (!isSelected)
        {
            HandleDragging();
        }
        else if (!isGround)
        {
            HandleGrinding();
        }
        else if (!isAddedToCauldron)
        {
            HandleTransfer();
        }
    }

    void HandleDragging()
    {
        Vector3 mousePosition = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            ingredientCollider.OverlapPoint(mousePosition))
        {
            isDragging = true;
            offset = transform.position - mousePosition;
        }

        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            transform.position = mousePosition + offset;
        }

        if (isDragging &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;

            if (mortarCollider != null &&
                mortarCollider.OverlapPoint(transform.position))
            {
                if (ingredientManager != null &&
                    ingredientManager.TrySelectIngredient(name))
                {
                    isSelected = true;

                    Vector3 target =
                        ingredientManager.GetIngredientPosition(
                            mortarCollider.transform.position,
                            name
                        );

                    target.z = transform.position.z;
                    transform.position = target;
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

    void HandleGrinding()
    {
        Vector3 mousePosition = GetMouseWorldPosition();

        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        bool insideMortar =
            mortarCollider != null &&
            mortarCollider.OverlapPoint(mousePosition);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isGrinding =
                insideMortar &&
                ingredientCollider.OverlapPoint(mousePosition);

            lastMousePosition = screenPosition;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            isGrinding = false;

        if (!isGrinding ||
            !Mouse.current.leftButton.isPressed)
            return;

        float movement = Vector2.Distance(
            screenPosition,
            lastMousePosition
        );

        if (insideMortar &&
            movement > movementThreshold)
        {
            grindProgress += movement * 0.01f;

            float progress = Mathf.Clamp01(
                grindProgress / requiredMovement
            );

            transform.localScale = Vector3.Lerp(
                originalScale,
                originalScale * 0.4f,
                progress
            );

            spriteRenderer.color = Color.Lerp(
                originalColor,
                originalColor * 0.45f,
                progress
            );

            if (progress >= 1f)
                FinishGrinding();
        }

        lastMousePosition = screenPosition;
    }

    void FinishGrinding()
    {
        isGround = true;
        isGrinding = false;

        CreatePieces();
        spriteRenderer.enabled = false;

        Debug.Log(name + " fully ground!");
    }

    void CreatePieces()
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject piece =
                new GameObject(name + "_Piece");

            SpriteRenderer pieceRenderer =
                piece.AddComponent<SpriteRenderer>();

            pieceRenderer.sprite = spriteRenderer.sprite;
            pieceRenderer.color = originalColor * 0.55f;

            pieceRenderer.sortingLayerID =
                spriteRenderer.sortingLayerID;

            pieceRenderer.sortingOrder =
                spriteRenderer.sortingOrder + 1;

            Vector2 randomOffset =
                Random.insideUnitCircle * 0.18f;

            piece.transform.position =
                transform.position +
                new Vector3(
                    randomOffset.x,
                    randomOffset.y,
                    0
                );

            float size = Random.Range(0.08f, 0.16f);

            piece.transform.localScale =
                new Vector3(size, size, 1);

            pieces.Add(piece);
            pieceStartPositions.Add(piece.transform.position);
        }
    }

    void HandleTransfer()
    {
        if (pieces.Count == 0)
            return;

        Vector3 mousePosition = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsMouseOverPieces(mousePosition))
            {
                isTransferring = true;

                transferOffset =
                    pieces[0].transform.position - mousePosition;
            }
        }

        if (isTransferring &&
            Mouse.current.leftButton.isPressed)
        {
            Vector3 target = mousePosition + transferOffset;

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
                cauldronCollider.OverlapPoint(mousePosition))
            {
                AddToCauldron();
            }
            else
            {
                ResetPieces();
            }
        }
    }

    bool IsMouseOverPieces(Vector3 mousePosition)
    {
        foreach (GameObject piece in pieces)
        {
            if (piece == null)
                continue;

            if (Vector2.Distance(
                mousePosition,
                piece.transform.position
            ) < 0.35f)
                return true;
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
            ingredientManager.AddIngredientToCauldron(name);
        }

        Debug.Log(name + " added to cauldron!");
    }


    Vector3 GetMouseWorldPosition()
    {
        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        Vector3 mousePosition = new Vector3(
            screenPosition.x,
            screenPosition.y,
            -mainCamera.transform.position.z
        );

        return mainCamera.ScreenToWorldPoint(mousePosition);
    }
}
