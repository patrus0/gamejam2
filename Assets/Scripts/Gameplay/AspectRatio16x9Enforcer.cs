using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class AspectRatio16x9Enforcer : MonoBehaviour
{
    [Header("Target Aspect")]
    [SerializeField] private float targetWidth = 16f;
    [SerializeField] private float targetHeight = 9f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool forceBlackBackground = true;

    [Header("Canvas Sync")]
    [SerializeField] private bool syncRootCanvasesToCamera = true;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Range(0f, 1f)]
    [SerializeField] private float matchWidthOrHeight = 0.5f;

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        ApplyAll();
    }

    private void OnEnable()
    {
        ApplyAll();
    }

    private void Update()
    {
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            return;

        ApplyAll();
    }

    private void OnValidate()
    {
        targetWidth = Mathf.Max(1f, targetWidth);
        targetHeight = Mathf.Max(1f, targetHeight);
        referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
        referenceResolution.y = Mathf.Max(1f, referenceResolution.y);

        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (!Application.isPlaying)
            ApplyAll();
    }

    private void ApplyAll()
    {
        ApplyCameraViewport();

        if (syncRootCanvasesToCamera)
            ApplyCanvasSettings();

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void ApplyCameraViewport()
    {
        if (targetCamera == null)
            return;

        if (forceBlackBackground)
        {
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = Color.black;
        }

        float targetAspect = targetWidth / targetHeight;
        float screenAspect = (float)Screen.width / Screen.height;

        if (screenAspect > targetAspect)
        {
            // Экран слишком широкий: вертикальные черные поля (pillarbox).
            float normalizedWidth = targetAspect / screenAspect;
            float xOffset = (1f - normalizedWidth) * 0.5f;
            targetCamera.rect = new Rect(xOffset, 0f, normalizedWidth, 1f);
        }
        else if (screenAspect < targetAspect)
        {
            // Экран слишком высокий: горизонтальные черные поля (letterbox).
            float normalizedHeight = screenAspect / targetAspect;
            float yOffset = (1f - normalizedHeight) * 0.5f;
            targetCamera.rect = new Rect(0f, yOffset, 1f, normalizedHeight);
        }
        else
        {
            targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    private void ApplyCanvasSettings()
    {
        if (targetCamera == null)
            return;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.isRootCanvas)
                continue;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = targetCamera;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                continue;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }
    }
}
