
using UnityEngine;
using UnityEngine.InputSystem;

public class PestleController : MonoBehaviour
{
    public DragIngredient ingredient;
    public Collider2D mortarCollider;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private GrindIngredient grind;
    private Vector3 startPosition;

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        if (ingredient != null)
            grind = ingredient.GetComponent<GrindIngredient>();

        spriteRenderer.enabled = false;
    }

    void Update()
    {
        if (Mouse.current == null ||
            mainCamera == null ||
            ingredient == null ||
            mortarCollider == null ||
            grind == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        // Не показываем пестик после измельчения
        if (!ingredient.IsInMortar || grind.IsFinished)
        {
            spriteRenderer.enabled = false;
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

        bool showPestle =
            Mouse.current.leftButton.isPressed && insideMortar;

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
}
