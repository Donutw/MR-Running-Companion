using UnityEngine;

/// <summary>
/// 触发器：配速提示（闲聊级别）
/// 监听 PaceCalculator.currentPaceFloat，当配速过慢（数字过大）时提示提速。
/// 优先级建议：3（闲聊/轻提示）
/// </summary>
public class Trigger_Pace : MonoBehaviour
{
    [Header("引用（手动拖拽，数据解耦）")]
    public DogDialogueManager dogDialogueManager;
    public PaceCalculator paceCalculator;

    [Header("触发条件")]
    [Tooltip("当 currentPaceFloat > 该阈值（分/公里）时，认为配速偏慢。例：8 代表 8 分/公里。")]
    public float slowPaceThreshold = 8f;

    [Tooltip("冷却时间（秒），避免一直唠叨")]
    public float paceCooldown = 20f;

    [Header("播报内容")]
    [TextArea(2, 4)]
    public string message = "稍微提提速吧，我们可以的！";

    public int priority = 3;

    // 防刷：冷却时间
    private float _nextAllowedTime = 0f;

    void Update()
    {
        if (dogDialogueManager == null || paceCalculator == null) return;
        if (!paceCalculator.isRecording) return;

        float pace = paceCalculator.currentPaceFloat;
        if (pace <= 0f) return; // 距离未满 10m 时 PaceCalculator 会保持 0

        if (pace > slowPaceThreshold && Time.time >= _nextAllowedTime)
        {
            dogDialogueManager.Speak(message, priority);
            _nextAllowedTime = Time.time + Mathf.Max(0.1f, paceCooldown);
        }
    }
}
