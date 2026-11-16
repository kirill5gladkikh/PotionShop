
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class GrindIngredient : MonoBehaviour
{
    [Header("Grinding")]
    public float requiredMovement = 8f;
    public float movementThreshold = 1f;

    [Header("Cauldron")]
    public Collider2D cauldronCollider;
    public SpriteRenderer potionLiquid;

    [Header("Stirring")]
    public Transform stirringSpoon;
    public float requiredRotations = 2f;
    public AudioSource stirringAudio;

    [Header("Bubbles")]
    public float bubbleInterval = 0.12f;
    public float bubbleLifetime = 0.7f;

    private float grindProgress;
    private Vector2 lastMousePosition;
    private bool isGrinding;
    private bool isFinished;
    private bool isTransferring;
    private bool isAddedToCauldron;

    private bool isStirring;
    private bool potionReady;
    private float totalRotation;
    private float lastAngle;
    private float bubbleTimer;
    private Vector2 lastStirMousePosition;

    private SpriteRenderer spoonRenderer;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private DragIngredient drag;
    private Camera mainCamera;
    private AudioSource grindingAudio;

    private List<GameObject> herbPieces =
        new List<GameObject>();

    private Vector3 transferStartPosition;
    private Vector3 transferOffset;

    private Color startingLiquidColor =
        new Color(0.25f, 0.65f, 0.35f);

    private Color finishedLiquidColor =
        new Color(0.65f, 0.95f, 0.45f);

    public bool IsFinished => isFinished;
    public bool IsPotionReady => potionReady;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        drag = GetComponent<DragIngredient>();
        mainCamera = Camera.main;
        grindingAudio = GetComponent<AudioSource>();

        grindingAudio.playOnAwake = false;
        grindingAudio.loop = true;
        grindingAudio.spatialBlend = 0f;

        if (stirringAudio != null)
        {
            stirringAudio.playOnAwake = false;
            stirringAudio.loop = true;
            stirringAudio.spatialBlend = 0f;
        }

        if (stirringSpoon != null)
        {
            spoonRenderer =
                stirringSpoon.GetComponent<SpriteRenderer>();

            HideSpoon();
        }
    }

    void Update()
    {
        if (Mouse.current == null || mainCamera == null)
        {
            StopGrindingSound();
            StopStirringSound();
            HideSpoon();
            return;
        }

        if (isAddedToCauldron)
        {
            HandleStirring();
            return;
        }

        if (isFinished)
        {
            HandleTransfer();
            return;
        }

        HandleGrinding();
    }

    void HandleGrinding()
    {
        if (drag == null ||
            !drag.IsInMortar ||
            drag.mortarCollider == null)
        {
            StopGrindingSound();
            return;
        }

        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        Vector3 worldPosition = GetMouseWorldPosition();

        bool insideMortar =
            drag.mortarCollider.OverlapPoint(worldPosition);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isGrinding = insideMortar;
            lastMousePosition = screenPosition;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            isGrinding = false;

        if (isGrinding && Mouse.current.leftButton.isPressed)
        {
            float movement = Vector2.Distance(
                screenPosition,
                lastMousePosition
            );

            if (insideMortar && movement > movementThreshold)
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
                    new Color(0.44f, 0.65f, 0.35f),
                    new Color(0.15f, 0.35f, 0.12f),
                    progress
                );

                if (!grindingAudio.isPlaying &&
                    grindingAudio.clip != null)
                {
                    grindingAudio.UnPause();

                    if (!grindingAudio.isPlaying)
                        grindingAudio.Play();
                }

                if (progress >= 1f)
                {
                    isFinished = true;
                    StopGrindingSound();
                    CreateHerbPieces();
                    spriteRenderer.enabled = false;

                    Debug.Log("Ingredient fully ground!");
                }
            }
            else
            {
                StopGrindingSound();
            }

            lastMousePosition = screenPosition;
        }
        else
        {
            StopGrindingSound();
        }
    }

    void HandleTransfer()
    {
        if (herbPieces.Count == 0)
            return;

        Vector3 mousePosition = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsMouseOverPieces(mousePosition))
            {
                isTransferring = true;

                transferStartPosition =
                    herbPieces[0].transform.position;

                transferOffset =
                    transferStartPosition - mousePosition;
            }
        }

        if (isTransferring &&
            Mouse.current.leftButton.isPressed)
        {
            Vector3 newPosition =
                mousePosition + transferOffset;

            Vector3 movement =
                newPosition - herbPieces[0].transform.position;

            foreach (GameObject piece in herbPieces)
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
                ResetPiecesPosition();
            }
        }
    }

    void HandleStirring()
    {
        if (potionReady ||
            cauldronCollider == null ||
            stirringSpoon == null)
        {
            HideSpoon();
            StopStirringSound();
            return;
        }

        Vector3 mousePosition = GetMouseWorldPosition();

        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        bool insideCauldron =
            cauldronCollider.OverlapPoint(mousePosition);

        Vector2 center = cauldronCollider.bounds.center;
        Vector2 direction = (Vector2)mousePosition - center;

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isStirring = insideCauldron;
            lastAngle = angle;
            lastStirMousePosition = screenPosition;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            isStirring = false;

        bool active =
            isStirring &&
            Mouse.current.leftButton.isPressed &&
            insideCauldron;

        if (!active)
        {
            HideSpoon();
            StopStirringSound();
            lastAngle = angle;
            lastStirMousePosition = screenPosition;
            return;
        }

        if (spoonRenderer != null)
            spoonRenderer.enabled = true;

        stirringSpoon.position = new Vector3(
            mousePosition.x,
            mousePosition.y,
            stirringSpoon.position.z
        );

        float movement = Vector2.Distance(
            screenPosition,
            lastStirMousePosition
        );

        float angleDifference =
            Mathf.DeltaAngle(lastAngle, angle);

        bool moving =
            movement > movementThreshold &&
            direction.magnitude > 0.15f &&
            Mathf.Abs(angleDifference) < 45f;

        if (moving)
        {
            totalRotation += Mathf.Abs(angleDifference);

            PlayStirringSound();

            bubbleTimer += Time.deltaTime;

            if (bubbleTimer >= bubbleInterval)
            {
                CreateBubble();
                bubbleTimer = 0f;
            }
        }
        else
        {
            StopStirringSound();
        }

        lastAngle = angle;
        lastStirMousePosition = screenPosition;

        float progress = Mathf.Clamp01(
            totalRotation /
            (360f * Mathf.Max(0.1f, requiredRotations))
        );

        if (potionLiquid != null)
        {
            potionLiquid.color = Color.Lerp(
                startingLiquidColor,
                finishedLiquidColor,
                progress
            );
        }

        if (progress >= 1f)
        {
            potionReady = true;
            isStirring = false;

            StopStirringSound();
            HideSpoon();

            Debug.Log("Potion is ready!");
        }
    }

    void CreateBubble()
    {
        if (potionLiquid == null ||
            potionLiquid.sprite == null)
            return;

        GameObject bubble = new GameObject("PotionBubble");

        SpriteRenderer bubbleRenderer =
            bubble.AddComponent<SpriteRenderer>();

        bubbleRenderer.sprite = potionLiquid.sprite;

        bubbleRenderer.color =
            new Color(0.8f, 1f, 0.9f, 0.55f);

        bubbleRenderer.sortingLayerID =
            potionLiquid.sortingLayerID;

        bubbleRenderer.sortingOrder =
            potionLiquid.sortingOrder + 1;

        Vector2 offset =
            Random.insideUnitCircle * 0.65f;

        Bounds liquidBounds = potionLiquid.bounds;

        bubble.transform.position = new Vector3(
            liquidBounds.center.x +
                offset.x * liquidBounds.extents.x,
            liquidBounds.center.y +
                offset.y * liquidBounds.extents.y,
            potionLiquid.transform.position.z - 0.1f
        );

        float size = Random.Range(0.08f, 0.20f);

        bubble.transform.localScale =
            new Vector3(size, size, 1f);

        Destroy(bubble, bubbleLifetime);
    }

    void PlayStirringSound()
    {
        if (stirringAudio == null ||
            stirringAudio.clip == null)
            return;

        if (!stirringAudio.isPlaying)
        {
            stirringAudio.UnPause();

            if (!stirringAudio.isPlaying)
                stirringAudio.Play();
        }
    }

    void StopStirringSound()
    {
        if (stirringAudio != null &&
            stirringAudio.isPlaying)
        {
            stirringAudio.Pause();
        }
    }

    void StopGrindingSound()
    {
        if (grindingAudio != null &&
            grindingAudio.isPlaying)
        {
            grindingAudio.Pause();
        }
    }

    void HideSpoon()
    {
        if (spoonRenderer != null)
            spoonRenderer.enabled = false;
    }

    bool IsMouseOverPieces(Vector3 mousePosition)
    {
        foreach (GameObject piece in herbPieces)
        {
            if (piece == null)
                continue;

            if (Vector2.Distance(
                mousePosition,
                piece.transform.position
            ) < 0.5f)
                return true;
        }

        return false;
    }

    void AddToCauldron()
    {
        isAddedToCauldron = true;

        foreach (GameObject piece in herbPieces)
        {
            if (piece != null)
                Destroy(piece);
        }

        herbPieces.Clear();

        if (potionLiquid != null)
            potionLiquid.color = startingLiquidColor;

        Debug.Log("Herbs added to cauldron!");
    }

    void ResetPiecesPosition()
    {
        Vector3 movement =
            transferStartPosition -
            herbPieces[0].transform.position;

        foreach (GameObject piece in herbPieces)
        {
            if (piece != null)
                piece.transform.position += movement;
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

    void CreateHerbPieces()
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject piece = new GameObject("HerbPiece");

            SpriteRenderer pieceRenderer =
                piece.AddComponent<SpriteRenderer>();

            pieceRenderer.sprite = spriteRenderer.sprite;

            pieceRenderer.color = new Color(
                Random.Range(0.12f, 0.28f),
                Random.Range(0.25f, 0.45f),
                0.10f
            );

            pieceRenderer.sortingLayerID =
                spriteRenderer.sortingLayerID;

            pieceRenderer.sortingOrder =
                spriteRenderer.sortingOrder + 1;

            Vector2 offset =
                Random.insideUnitCircle * 0.25f;

            piece.transform.position =
                transform.position + new Vector3(
                    offset.x,
                    offset.y,
                    0
                );

            float size = Random.Range(0.08f, 0.18f);

            piece.transform.localScale =
                new Vector3(size, size, 1f);

            herbPieces.Add(piece);
        }
    }
}
