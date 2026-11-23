using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class NastyaCameraCue : MonoBehaviour
{
    [Header("Анимация")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;

    [Header("Появление в камере")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float cameraMargin = 0.05f;

    [Header("Движение к лампе")]
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
        SetIdle();
    }

    private void Update()
    {
        if (hasArrived) return;
        if (!hasStarted && IsInsideCamera()) hasStarted = true;
        if (!hasStarted) return;

        MoveToLamp();
        if (!hasArrived) Animate();
    }

    private bool IsInsideCamera()
    {
        if (targetCamera == null) return false;
        Vector3 p = targetCamera.WorldToViewportPoint(transform.position);
        return p.z > 0f && p.x >= -cameraMargin && p.x <= 1f + cameraMargin
            && p.y >= -cameraMargin && p.y <= 1f + cameraMargin;
    }

    private void Animate()
    {
        if (walkFrames == null || walkFrames.Length == 0) return;
        animationClock += Time.deltaTime;
        int index = Mathf.FloorToInt(animationClock * framesPerSecond) % walkFrames.Length;
        spriteRenderer.sprite = walkFrames[index];
        if (lampStopPoint != null)
            spriteRenderer.flipX = lampStopPoint.position.x < transform.position.x;
    }

    private void MoveToLamp()
    {
        if (lampStopPoint == null) return;
        Vector3 target = lampStopPoint.position;
        target.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, target) <= stopDistance)
        {
            hasArrived = true;
            SetIdle();
        }
    }

    private void SetIdle()
    {
        if (spriteRenderer == null) return;
        if (idleSprite != null) spriteRenderer.sprite = idleSprite;
        else if (walkFrames != null && walkFrames.Length > 0) spriteRenderer.sprite = walkFrames[0];
    }
}
