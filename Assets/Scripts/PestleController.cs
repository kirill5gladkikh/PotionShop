
using UnityEngine;
using UnityEngine.InputSystem;

public class PestleController : MonoBehaviour
{
    public Collider2D mortarCollider;
    public DragIngredient herb;
    public IngredientSelector[] otherIngredients;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private GrindIngredient herbGrind;
    private float originalZ;

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalZ = transform.position.z;

        if (herb != null)
            herbGrind = herb.GetComponent<GrindIngredient>();

        spriteRenderer.enabled = false;
    }

    void Update()
    {
        spriteRenderer.enabled = false;

        if (Mouse.current == null ||
            mainCamera == null ||
            mortarCollider == null)
            return;

        Vector2 screen =
            Mouse.current.position.ReadValue();

        Vector3 world =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    screen.x,
                    screen.y,
                    -mainCamera.transform.position.z
                )
            );

        if (!Mouse.current.leftButton.isPressed ||
            !mortarCollider.OverlapPoint(world))
            return;

        bool canGrind =
            herb != null &&
            herbGrind != null &&
            herb.IsInMortar &&
            !herbGrind.IsFinished;

        if (otherIngredients != null)
        {
            foreach (IngredientSelector item in otherIngredients)
            {
                if (item != null &&
                    item.processingType ==
                        IngredientSelector.ProcessingType.Grinding &&
                    item.IsSelected &&
                    !item.IsGround)
                {
                    canGrind = true;
                    break;
                }
            }
        }

        if (!canGrind)
            return;

        spriteRenderer.enabled = true;

        transform.position = new Vector3(
            world.x,
            world.y,
            originalZ
        );
    }
}
