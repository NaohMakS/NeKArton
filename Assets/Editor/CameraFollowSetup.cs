#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Положите в Assets/Editor. CameraFollow.cs должен лежать вне папки Editor.
public static class CameraFollowSetup
{
    [MenuItem("NeKArton/4. Setup Camera Follow")]
    static void Setup()
    {
        Camera cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError("[NeKArton] На сцене нет камеры."); return; }

        // --- цель: выделенный объект, иначе тег Player, иначе имя содержит "Player" ---
        Transform target = null;
        GameObject sel = Selection.activeGameObject;
        if (sel != null && sel != cam.gameObject && sel.GetComponent<Canvas>() == null)
            target = sel.transform;

        if (target == null)
        {
            try { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) target = p.transform; }
            catch { /* тега может не быть */ }
        }
        if (target == null)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name.StartsWith("Player")) { target = t; break; }
        }
        if (target == null)
        {
            Debug.LogWarning("[NeKArton] Герой не найден. Выделите героя в Hierarchy и повторите.");
            return;
        }

        // --- фон: объект с "Background" в имени, иначе спрайт с самым низким Order in Layer ---
        SpriteRenderer bg = null;
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr.name.ToLower().Contains("background")) { bg = sr; break; }
            if (bg == null || sr.sortingOrder < bg.sortingOrder) bg = sr;
        }

        // --- камера ---
        Undo.RecordObject(cam, "Setup camera");
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = Undo.AddComponent<CameraFollow>(cam.gameObject);

        Undo.RecordObject(follow, "Setup camera follow");
        follow.target = target;
        follow.boundsSource = bg;
        follow.useBounds = bg != null;

        Undo.RecordObject(cam.transform, "Move camera");
        cam.transform.position = new Vector3(target.position.x, target.position.y, follow.cameraZ);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[NeKArton] Камера следует за: " + target.name + (bg != null ? ", границы: " + bg.name : ", границы не заданы")
                  + (cam.orthographic ? "" : ". ВНИМАНИЕ: камера не Orthographic, для 2D лучше включить."));
    }
}
#endif
