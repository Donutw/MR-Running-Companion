using UnityEngine;

/// <summary>
/// 触发器：心率报警（最高优先级）
/// 监听 HeartRateBleReader.LastHeartRateBpm，当心率超过阈值时触发报警。
/// 特殊要求：必须带冷却机制，避免心率波动导致连续报警。
/// 优先级建议：0（紧急/安全，可打断一切）
/// </summary>
public class Trigger_HeartRate : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("对话管理器（需要手动拖拽）")]
    public DogDialogueManager dogDialogueManager;

    [Header("触发条件")]
    public int heartRateWarningThreshold = 150;

    [Tooltip("冷却时间（秒），例如 15 秒内最多报警一次")]
    public float heartRateCooldown = 15f;

    [Header("播报内容")]
    [TextArea(2, 4)]
    public string message = "心率偏高，慢一点哦";

    public int priority = 0;

    // 防刷：冷却时间
    private float _nextAllowedTime = 0f;

    // 心率读取器（自动从场景中查找，包括从 Menu 场景 DontDestroyOnLoad 跟过来的）
    private HeartRateBleReader _heartRateBleReader;

    void Start()
    {
        // 自动查找心率读取器（会在当前场景或 DontDestroyOnLoad 对象中找到）
        _heartRateBleReader = FindObjectOfType<HeartRateBleReader>();
        
        if (_heartRateBleReader == null)
        {
            Debug.LogWarning("[Trigger_HeartRate] 未找到 HeartRateBleReader，心率触发功能将无法工作。");
        }
    }

    void Update()
    {
        if (dogDialogueManager == null || _heartRateBleReader == null) return;
        if (!_heartRateBleReader.isConnected) return;

        int bpm = _heartRateBleReader.LastHeartRateBpm;
        if (bpm <= 0) return;

        if (bpm > heartRateWarningThreshold && Time.time >= _nextAllowedTime)
        {
            dogDialogueManager.Speak(message, priority);
            _nextAllowedTime = Time.time + Mathf.Max(0.1f, heartRateCooldown);
        }
    }
}
