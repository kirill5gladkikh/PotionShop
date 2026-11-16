
using UnityEngine;
using UnityEngine.InputSystem;

public class PestleController : MonoBehaviour
{
    [Header("Mortar")]
    public Collider2D mortarCollider;

    [Header("Ingredients")]
    public DragIngredient herb;
    public IngredientSelector[] otherIngredients;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private GrindIngredient herbGrind;
    private Vector3 startPosition;

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        if (herb != null)
            herbGrind = herb.GetComponent<GrindIngredient>();

        spriteRenderer.enabled = false;
    }

    void Update()
    {
        if (Mouse.current == null ||
            mainCamera == null ||
            mortarCollider == null)
        {
            HidePestle();
            return;
        }

        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    -mainCamera.transform.position.z
                )
            );

        bool insideMortar =
            mortarCollider.OverlapPoint(worldPosition);

        bool canGrind = false;

        // Проверяем обычную траву
        if (herb != null &&
            herbGrind != null &&
            herb.IsInMortar &&
            !herbGrind.IsFinished)
        {
            canGrind = true;
        }

        // Проверяем остальные ингредиенты
        if (otherIngredients != null)
        {
            foreach (IngredientSelector ingredient
                in otherIngredients)
            {
                if (ingredient != null &&
                    ingredient.IsSelected &&
                    !ingredient.IsGround)
                {
                    canGrind = true;
                    break;
                }
            }
        }

        bool showPestle =
            canGrind &&
            Mouse.current.leftButton.isPressed &&
            insideMortar;

        spriteRenderer.enabled = showPestle;

        if (showPestle)
        {
            transform.position = new Vector3(
                worldPosition.x,
                worldPosition.y,
                startPosition.z
            );
        }
    }

    void HidePestle()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
}
