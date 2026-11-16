
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

    [Header("Bottle Cork")]
    public Transform bottleCork;
    public Transform corkPoint;
    public float corkSnapDistance = 0.6f;

    private bool isDragging;
    private bool isInMortar;
    private Vector3 startPosition;
    private Vector3 offset;
    private Camera mainCamera;
    private Collider2D ingredientCollider;

    private bool isFilling;
    private bool bottleFilled;
    private float fillProgress;
    private Vector3 liquidStartScale;
    private Vector3 liquidStartPosition;

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

        if (bottleLiquid != null)
        {
            liquidStartScale = bottleLiquid.localScale;
            liquidStartPosition = bottleLiquid.localPosition;
        }

        if (bottleCork != null)
        {
            corkStartPosition = bottleCork.position;
            corkCollider = bottleCork.GetComponent<Collider2D>();
        }
    }

    void Update()
    {
        if (Mouse.current == null || mainCamera == null)
            return;

        HandleIngredientDragging();
        HandleBottleFilling();
        HandleCorkDragging();
    }

    void HandleIngredientDragging()
    {
        if (isInMortar || ingredientCollider == null)
            return;

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
                bool accepted =
                    ingredientManager != null &&
                    ingredientManager.TrySelectIngredient(name);

                if (accepted)
                {
                    Vector3 target = ingredientManager.GetIngredientPosition(
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

    void HandleBottleFilling()
    {
        if (grindIngredient == null ||
            !grindIngredient.IsPotionReady ||
            bottleCollider == null ||
            bottleLiquid == null ||
            bottleFilled)
            return;

        Vector3 mousePosition = GetMouseWorldPosition();

        bool insideBottle =
            bottleCollider.OverlapPoint(mousePosition);

        if (Mouse.current.leftButton.wasPressedThisFrame)
            isFilling = insideBottle;

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

            float newHeight = Mathf.Lerp(
                liquidStartScale.y,
                0.62f,
                fillProgress
            );

            bottleLiquid.localScale = new Vector3(
                0.55f,
                newHeight,
                liquidStartScale.z
            );

            float bottomY = -0.32f;

            bottleLiquid.localPosition = new Vector3(
                liquidStartPosition.x,
                bottomY + newHeight * 0.5f,
                liquidStartPosition.z
            );

            if (fillProgress >= 1f)
            {
                bottleFilled = true;
                isFilling = false;

                Debug.Log("Potion bottled!");
            }
        }
    }

    void HandleCorkDragging()
    {
        if (!bottleFilled ||
            corkPlaced ||
            bottleCork == null ||
            corkPoint == null ||
            corkCollider == null)
            return;

        Vector3 mousePosition = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            corkCollider.OverlapPoint(mousePosition))
        {
            isDraggingCork = true;
            corkOffset = bottleCork.position - mousePosition;
        }

        if (isDraggingCork &&
            Mouse.current.leftButton.isPressed)
        {
            Vector3 newPosition = mousePosition + corkOffset;

            bottleCork.position = new Vector3(
                newPosition.x,
                newPosition.y,
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

                Debug.Log("Potion complete! Bottle sealed!");
            }
            else
            {
                bottleCork.position = corkStartPosition;
            }
        }
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
