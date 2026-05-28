using UnityEngine;
using System.Reflection;

/// <summary>
/// 触发器：时间提醒
/// 监听 TestSceneController 的剩余时间，当低于阈值时只触发一次提醒。
/// 优先级建议：1（关键节点）
/// </summary>
public class Trigger_Time : MonoBehaviour
{
    [Header("引用（手动拖拽，数据解耦）")]
    [Tooltip("对话管理器（中心化审核：优先级）")]
    public DogDialogueManager dogDialogueManager;

    [Tooltip("测试流程控制器（用于读取剩余时间/是否在跑步）")]
    public TestSceneController testSceneController;

    [Header("触发条件")]
    [Tooltip("当剩余时间 <= 该阈值（秒）时触发一次")]
    public float thresholdSeconds = 60f;

    [Header("播报内容")]
    [TextArea(2, 4)]
    public string message = "最后冲刺！还有一分钟！";

    [Tooltip("优先级：0最高（紧急），3最低（闲聊）")]
    public int priority = 1;

    // 防刷：只触发一次
    private bool _hasTriggered = false;

    // 反射缓存：读取 TestSceneController 私有字段 currentTime / isRunning
    private static FieldInfo _fiCurrentTime;
    private static FieldInfo _fiIsRunning;

    void Update()
    {
        if (_hasTriggered) return;
        if (dogDialogueManager == null || testSceneController == null) return;

        // 必须在跑步中才允许触发
        if (!GetIsRunning(testSceneController)) return;

        float remaining = GetRemainingTimeSeconds(testSceneController);
        if (remaining <= 0f) return; // 0 或负数代表已结束/无效

        if (remaining <= thresholdSeconds)
        {
            dogDialogueManager.Speak(message, priority);
            _hasTriggered = true;
        }
    }

    /// <summary>
    /// 读取 TestSceneController 私有字段 currentTime（秒）
    /// </summary>
    private static float GetRemainingTimeSeconds(TestSceneController controller)
    {
        if (controller == null) return -1f;

        if (_fiCurrentTime == null)
        {
            _fiCurrentTime = controller.GetType().GetField("currentTime", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (_fiCurrentTime != null && _fiCurrentTime.FieldType == typeof(float))
        {
            return (float)_fiCurrentTime.GetValue(controller);
        }

        // 兜底：如果未来你给 TestSceneController 增加了公开 RemainingTime 属性/方法，可在这里改成直接读公开接口
        return -1f;
    }

    /// <summary>
    /// 读取 TestSceneController 私有字段 isRunning
    /// </summary>
    private static bool GetIsRunning(TestSceneController controller)
    {
        if (controller == null) return false;

        if (_fiIsRunning == null)
        {
            _fiIsRunning = controller.GetType().GetField("isRunning", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (_fiIsRunning != null && _fiIsRunning.FieldType == typeof(bool))
        {
            return (bool)_fiIsRunning.GetValue(controller);
        }

        // 兜底：用 Data HUD 是否显示来粗略判断"是否在跑步"
        if (controller.dataHUDPanel != null) return controller.dataHUDPanel.activeSelf;
        return false;
    }
}
