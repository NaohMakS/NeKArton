using UnityEngine;

// Вешается на Main Camera. Плавно следует за целью и не выходит за границы фона.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Цель")]
    public Transform target;

    [Header("Движение")]
    public float smoothTime = 0.15f;
    public Vector2 offset = Vector2.zero;
    public float cameraZ = -10f;

    [Header("Границы (необязательно)")]
    public bool useBounds = true;
    public SpriteRenderer boundsSource;   // обычно фон локации

    Camera cam;
    Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        // сразу встать на героя, без «пролёта» камеры при старте
        if (target != null) transform.position = Desired();
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
    }

    Vector3 Desired()
    {
        Vector3 pos = new Vector3(target.position.x + offset.x, target.position.y + offset.y, cameraZ);

        if (useBounds && boundsSource != null && cam.orthographic)
        {
            Bounds b = boundsSource.bounds;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = b.min.x + halfW, maxX = b.max.x - halfW;
            float minY = b.min.y + halfH, maxY = b.max.y - halfH;

            // если фон меньше экрана, просто центрируем по нему
            pos.x = minX > maxX ? b.center.x : Mathf.Clamp(pos.x, minX, maxX);
            pos.y = minY > maxY ? b.center.y : Mathf.Clamp(pos.y, minY, maxY);
        }
        return pos;
    }
}
