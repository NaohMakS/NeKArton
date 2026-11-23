using UnityEngine;

/// <summary>
/// Starts Nastya's walk cycle the first time she enters the camera view,
/// then moves her to the configured lamp-side destination.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NastyaCameraCue : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 9f;

    [Header("Camera trigger")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float cameraMargin = 0.05f;

    [Header("Movement target")]
    [SerializeField] private Transform lampStopPoint;
    [SerializeField, Min(0.01f)] private float moveSpeed = 1.5f;
    [SerializeField] private float stopDistance = 0.08f;

    private SpriteRenderer spriteRenderer;
    private bool hasStarted;
    private bool hasArrived;
    private float animationClock;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;
        if (walkFrames != null && walkFrames.Length > 0)
            spriteRenderer.sprite = walkFrames[0];
    }

    private void Update()
    {
        if (hasArrived) return;

        if (!hasStarted && IsInsideCamera())
            hasStarted = true;

        if (!hasStarted) return;

        Animate();
        MoveToLamp();
    }

    private bool IsInsideCamera()
    {
        if (targetCamera == null) return false;
        Vector3 p = targetCamera.WorldToViewportPoint(transform.position);
        return p.z > 0f &&
               p.x >= -cameraMargin && p.x <= 1f + cameraMargin &&
               p.y >= -cameraMargin && p.y <= 1f + cameraMargin;
    }

    private void Animate()
    {
        if (walkFrames == null || walkFrames.Length == 0) return;
        animationClock += Time.deltaTime;
        int index = Mathf.FloorToInt(animationClock * framesPerSecond) % walkFrames.Length;
        spriteRenderer.sprite = walkFrames[index];
    }

    private void MoveToLamp()
    {
        if (lampStopPoint == null) return;

        Vector3 target = lampStopPoint.position;
        target.z = transform.position.z;
        transform.position = Vector3.MoveTowards(
            transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) <= stopDistance)
        {
            hasArrived = true;
            if (walkFrames != null && walkFrames.Length > 0)
                spriteRenderer.sprite = walkFrames[0];
        }
    }
}
