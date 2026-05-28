using UnityEngine;
using TMPro;

/// <summary>
/// 距离计算器（数据中心版）
/// 专门负责精准计算 VR 头显的水平移动距离。带有一个可选的 UI 屏幕接口。
/// </summary>
public class DistanceCalculator : MonoBehaviour
{
    [Header("数据源")]
    [Tooltip("VR 头显相机，不填会自动找 Main Camera")]
    public Camera headsetCamera;

    [Header("UI 显示（选填）")]
    [Tooltip("如果需要把距离显示在面板上，把 Text 拖进来。不显示就空着。")]
    public TextMeshProUGUI distanceText;
    
    [Tooltip("显示格式，{0} 会被替换为整数米数")]
    public string displayFormat = "距离: {0} 米";

    [Header("后台数据（供触发器和 CSV 读取）")]
    [Tooltip("当前累计跑动的真实距离（米）")]
    [HideInInspector]
    public float currentDistanceMeters = 0f;

    [Tooltip("开关：是否正在记录数据？（打勾才会计算距离）")]
    [HideInInspector]
    public bool isRecording = false;

    // 内部计算变量
    private Vector3 _lastPosition;
    private bool _hasLastPosition = false;

    void Start()
    {
        if (headsetCamera == null) headsetCamera = Camera.main;
    }

    void Update()
    {
        // 如果没在测试中，就直接跳过，不计算距离
        if (!isRecording) return;

        if (headsetCamera == null)
        {
            headsetCamera = Camera.main;
            if (headsetCamera == null) return;
        }

        Vector3 currentPos = headsetCamera.transform.position;

        if (_hasLastPosition)
        {
            // 【核心优化】：只计算 X 和 Z 轴的距离，忽略 Y 轴（头部上下晃动）
            Vector2 lastXZ = new Vector2(_lastPosition.x, _lastPosition.z);
            Vector2 currentXZ = new Vector2(currentPos.x, currentPos.z);
            
            float delta = Vector2.Distance(lastXZ, currentXZ);
            currentDistanceMeters += delta;
        }

        // 更新上一帧的位置
        _lastPosition = currentPos;
        _hasLastPosition = true;

        // 顺手更新 UI（如果你在面板里拖了 Text 组件的话）
        if (distanceText != null)
        {
            int meters = Mathf.RoundToInt(currentDistanceMeters);
            distanceText.text = string.Format(displayFormat, meters);
        }
    }

    /// <summary>
    /// 开始测试时调用：清零并开始记录
    /// </summary>
    public void StartRecording()
    {
        currentDistanceMeters = 0f;
        _hasLastPosition = false; // 强迫下一帧重新获取起点
        isRecording = true;
    }

    /// <summary>
    /// 结束测试时调用：停止记录
    /// </summary>
    public void StopRecording()
    {
        isRecording = false;
    }
}