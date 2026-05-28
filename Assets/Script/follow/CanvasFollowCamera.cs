using UnityEngine;
using System.Collections; // 引入协程功能

/// <summary>
/// 让 Canvas 带延迟感地保持在玩家视野正前方，并支持每次激活时的瞬间对齐与渐显效果。
/// 挂到 Canvas_UI 根物体上，Canvas 需为 World Space 模式。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))] // 自动强制挂载 CanvasGroup 组件用于渐显
public class CanvasFollowCamera : MonoBehaviour
{
    [Header("相机")]
    [Tooltip("玩家视角相机，不填则使用 Main Camera")]
    public Camera viewCamera;

    [Header("位置")]
    [Tooltip("与相机前方的距离（米）")]
    public float distance = 0.8f;
    [Tooltip("相对相机视线的高度偏移（米），0 为视线高度")]
    public float heightOffset = 0f;
    [Tooltip("锁定上下跟随。勾选后，低头/抬头时画布只会保持在水平正前方")]
    public bool lockVerticalMovement = true;

    [Header("延迟感")]
    public float positionSmoothTime = 0.25f;
    public float rotationSmooth = 8f;
    public float maxSpeed = 3f;

    [Header("视觉表现 (Fade In)")]
    [Tooltip("每次激活面板时是否播放渐显动画")]
    public bool useFadeIn = true;
    [Tooltip("渐显时长（秒）")]
    public float fadeDuration = 0.4f;

    private Transform _transform;
    private Transform _cameraTransform;
    private Vector3 _velocity = Vector3.zero;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _transform = transform;
        _canvasGroup = GetComponent<CanvasGroup>();
        
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera != null) _cameraTransform = viewCamera.transform;
    }

    // 每次物体被 SetActive(true) 时都会自动调用
    void OnEnable()
    {
        // 1. 瞬间把位置和旋转拉到玩家面前，消除“飞来”的残影
        SnapToTarget();

        // 2. 播放渐显动画
        if (useFadeIn && _canvasGroup != null)
        {
            StartCoroutine(FadeInRoutine());
        }
    }

    /// <summary>
    /// 瞬间计算并应用目标位置与旋转，不带任何平滑延迟
    /// </summary>
    private void SnapToTarget()
    {
        if (_cameraTransform == null) return;

        // --- 瞬间位置 ---
        Vector3 forward = _cameraTransform.forward;
        if (lockVerticalMovement)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = _cameraTransform.up;
                forward.y = 0f;
            }
        }
        forward.Normalize();
        Vector3 offset = forward * distance + Vector3.up * heightOffset;
        _transform.position = _cameraTransform.position + offset;
        
        // 必须清空平滑阻尼的惯性速度
        _velocity = Vector3.zero; 

        // --- 瞬间旋转 ---
        Vector3 dirToCamera = _cameraTransform.position - _transform.position;
        if (lockVerticalMovement) dirToCamera.y = 0f;
        if (dirToCamera.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToCamera.normalized) * Quaternion.Euler(0f, 180f, 0f);
            _transform.rotation = targetRot;
        }
    }

    /// <summary>
    /// 控制 CanvasGroup 透明度的渐显协程
    /// </summary>
    private IEnumerator FadeInRoutine()
    {
        _canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            // 使用 Lerp 平滑插值
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null; // 等待下一帧
        }
        
        _canvasGroup.alpha = 1f; // 确保最终完全不透明
    }

    void LateUpdate()
    {
        if (_cameraTransform == null) return;
        float dt = Time.deltaTime;

        // 获取相机前方的向量
        Vector3 forward = _cameraTransform.forward;
        if (lockVerticalMovement)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = _cameraTransform.up;
                forward.y = 0f;
            }
        }
        forward.Normalize();
        Vector3 offset = forward * distance + Vector3.up * heightOffset;
        Vector3 targetPos = _cameraTransform.position + offset;

        // 位置平滑
        _transform.position = Vector3.SmoothDamp(_transform.position, targetPos, ref _velocity, positionSmoothTime, maxSpeed, dt);

        // 目标旋转：面向相机
        Vector3 dirToCamera = _cameraTransform.position - _transform.position;
        if (lockVerticalMovement) dirToCamera.y = 0f;
        
        if (dirToCamera.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToCamera.normalized) * Quaternion.Euler(0f, 180f, 0f);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRot, rotationSmooth * dt);
        }
    }
}