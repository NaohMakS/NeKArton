using UnityEngine;

/// <summary>
/// Вступительное движение Насти: появляется в кадре, идёт к точке у лампы,
/// а затем остаётся на последнем кадре покоя.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NastyaCameraCue : MonoBehaviour
{
    [Header("Анимация")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 9f;

    [Header("Появление в камере")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float cameraMargin = 0.05f;

    [Header("Движение к лампе")]
    [SerializeField] private Transform lampStopPoint;
    [SerializeField, Min(0.01f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float stopDistance = 0.08f;

    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool hasStarted;
    private bool hasArrived;
    private float animationClock;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;
        FindPlayer();
        SetIdle();
    }

    private void Update()
    {
        if (hasArrived || lampStopPoint == null) return;

        if (!hasStarted && IsInsideCamera())
            hasStarted = true;

        if (!hasStarted) return;

        FacePlayerOrMovement();
        MoveToLamp();

        if (!hasArrived)
            AnimateWalk();
    }

    private bool IsInsideCamera()
    {
        if (targetCamera == null) return false;

        Vector3 viewport = targetCamera.WorldToViewportPoint(transform.position);
        return viewport.z > 0f &&
               viewport.x >= -cameraMargin && viewport.x <= 1f + cameraMargin &&
               viewport.y >= -cameraMargin && viewport.y <= 1f + cameraMargin;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            PlayerMovement movement = FindObjectOfType<PlayerMovement>();
            if (movement != null) playerObject = movement.gameObject;
        }
        if (playerObject != null) player = playerObject.transform;
    }

    private void FacePlayerOrMovement()
    {
        if (player == null) FindPlayer();

        // Настя должна смотреть на главного героя, даже когда идёт к точке остановки.
        float targetX = player != null ? player.position.x : lampStopPoint.position.x;
        float deltaX = targetX - transform.position.x;
        if (Mathf.Abs(deltaX) > 0.01f)
            spriteRenderer.flipX = deltaX < 0f;
    }

    private void AnimateWalk()
    {
        if (walkFrames == null || walkFrames.Length == 0) return;

        animationClock += Time.deltaTime;
        int frameIndex = Mathf.FloorToInt(animationClock * framesPerSecond) % walkFrames.Length;
        if (walkFrames[frameIndex] != null)
            spriteRenderer.sprite = walkFrames[frameIndex];
    }

    private void MoveToLamp()
    {
        Vector3 target = lampStopPoint.position;
        target.z = transform.position.z;

        transform.position = Vector3.MoveTowards(
            transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) <= stopDistance)
        {
            transform.position = target;
            hasArrived = true;
            SetIdle();
        }
    }

    private void SetIdle()
    {
        if (spriteRenderer == null) return;

        Sprite fallback = walkFrames != null && walkFrames.Length > 0
            ? walkFrames[0]
            : null;

        if (idleSprite != null) spriteRenderer.sprite = idleSprite;
        else if (fallback != null) spriteRenderer.sprite = fallback;
    }
}
