#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Положите в Assets/Editor. Меню: NeKArton -> 5. Attach Canvases To Camera
public static class CanvasToCameraSetup
{
    [MenuItem("NeKArton/5. Attach Canvases To Camera")]
    static void Attach()
    {
        Camera cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError("[NeKArton] На сцене нет камеры."); return; }

        int count = 0;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas) continue;   // вложенные Canvas подхватят настройки корневого

            Undo.RecordObject(c, "Attach canvas to camera");
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = Mathf.Max(1f, cam.nearClipPlane + 0.5f);   // перед камерой, перед спрайтами

            var scaler = c.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
            {
                Undo.RecordObject(scaler, "Canvas scaler");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
            count++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[NeKArton] Canvas привязано к камере \"" + cam.name + "\": " + count
                  + " шт. Теперь они всегда лежат точно в кадре камеры.");
    }
}
#endif
