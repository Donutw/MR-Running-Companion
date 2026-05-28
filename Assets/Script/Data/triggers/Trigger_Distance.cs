using UnityEngine;

/// <summary>
/// 触发器：距离鼓励
/// 监听 DistanceCalculator.currentDistanceMeters，达到里程碑时触发鼓励。
/// 优先级建议：2（常规成就）
/// </summary>
public class Trigger_Distance : MonoBehaviour
{
    [Header("引用（手动拖拽，数据解耦）")]
    public DogDialogueManager dogDialogueManager;
    public DistanceCalculator distanceCalculator;

    [Header("里程碑（米）")]
    [Tooltip("建议按从小到大填写，例如 100 / 500 / 1000。达到每个值只触发一次。")]
    public float[] milestonesMeters = new float[] { 100f, 500f, 1000f };

    [Header("播报内容")]
    [Tooltip("支持 {0}：会替换成里程碑米数（整数）")]
    [TextArea(3, 5)] // 这会让输入框变成 3 到 5 行高
    public string messageFormat = "太棒了！已经跑了 {0} 米！";

    public int priority = 2;

    // 防刷：用"下一个里程碑索引"保证单次触发
    private int _nextIndex = 0;

    void Update()
    {
        if (dogDialogueManager == null || distanceCalculator == null) return;
        if (!distanceCalculator.isRecording) return;
        if (milestonesMeters == null || milestonesMeters.Length == 0) return;

        float dist = distanceCalculator.currentDistanceMeters;

        // 可能一帧跨过多个里程碑：用 while 逐个补触发
        while (_nextIndex < milestonesMeters.Length && dist >= milestonesMeters[_nextIndex])
        {
            int meters = Mathf.RoundToInt(milestonesMeters[_nextIndex]);
            string msg = string.Format(messageFormat, meters);
            dogDialogueManager.Speak(msg, priority);
            _nextIndex++;
        }
    }
}
