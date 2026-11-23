using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class NastyaCameraCue : MonoBehaviour
{
    [Header("Анимация")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;

    [Header("Движение к лампе")]
    [SerializeField] private Transform lampStopPoint;
    [SerializeField, Min(0.01f)] private float moveSpeed = 1.5f;
    [SerializeField] private float stopDistance = 0.08f;

    private SpriteRenderer spriteRenderer;
    private bool hasArrived;
    private float animationClock;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = true;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 51;

        if (lampStopPoint == null)
        {
            GameObject lamp = GameObject.Find("NastyaLampStopPoint");
            if (lamp != null) lampStopPoint = lamp.transform;
        }

        SetIdle();
    }

    private void Update()
    {
        if (hasArrived) return;

        // Настя начинает идти сразу, не ожидая попадания в кадр камеры.
        if (lampStopPoint == null)
        {
            SetIdle();
            return;
        }

        MoveToLamp();
        if (!hasArrived) Animate();
    }

    private void Animate()
    {
        if (walkFrames == null || walkFrames.Length == 0) return;

        animationClock += Time.deltaTime;
        int index = Mathf.FloorToInt(animationClock * framesPerSecond) % walkFrames.Length;
        if (walkFrames[index] != null)
            spriteRenderer.sprite = walkFrames[index];

        spriteRenderer.flipX = lampStopPoint.position.x < transform.position.x;
    }

    private void MoveToLamp()
    {
        Vector3 target = lampStopPoint.position;
        target.z = transform.position.z;

        transform.position = Vector3.MoveTowards(
            transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) <= stopDistance)
        {
            hasArrived = true;
            SetIdle();
        }
    }

    private void SetIdle()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.enabled = true;
        spriteRenderer.color = Color.white;

        if (idleSprite != null)
            spriteRenderer.sprite = idleSprite;
        else if (walkFrames != null)
        {
            foreach (Sprite frame in walkFrames)
            {
                if (frame == null) continue;
                spriteRenderer.sprite = frame;
                break;
            }
        }
    }
}
