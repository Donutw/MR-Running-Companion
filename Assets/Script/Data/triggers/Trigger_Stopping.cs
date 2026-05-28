using UnityEngine;
using System.Reflection;

/// <summary>
/// 触发器：停下来了？
/// 监听 dogAnimator 的 Speed 参数：
/// - 当 TestSceneController 正在跑步(isRunning==true)，但 Speed 持续低于阈值超过 X 秒，
///   触发一句"怎么停下啦？"。
/// 优先级建议：3（闲聊）
/// </summary>
public class Trigger_Stopping : MonoBehaviour
{
    [Header("引用（手动拖拽，数据解耦）")]
    public DogDialogueManager dogDialogueManager;

    [Tooltip("测试流程控制器（用于判断是否在跑步）")]
    public TestSceneController testSceneController;

    [Tooltip("狗的 Animator（用于读取 Speed 参数）")]
    public Animator dogAnimator;

    [Header("判定参数")]
    [Tooltip("Animator 的速度参数名")]
    public string speedParameterName = "Speed";

    [Tooltip("Speed 低于该值视为'几乎没动'】【注意：Speed 是动画用速度，非真实米/秒】")]
    public float speedThreshold = 0.1f;

    [Tooltip("Speed 连续低于阈值超过该时间（秒）才触发")]
    public float stoppedSecondsThreshold = 3f;

    [Tooltip("触发后冷却时间（秒），防止速度抖动导致反复提醒")]
    public float cooldownSeconds = 15f;

    [Header("播报内容")]
    [TextArea(2, 4)]
    public string message = "怎么停下啦？继续加油哦！";

    public int priority = 3;

    // 防刷：一次"停顿事件"只触发一次 + 冷却
    private float _belowSpeedTimer = 0f;
    private bool _hasTriggeredThisStop = false;
    private float _nextAllowedTime = 0f;

    private static FieldInfo _fiIsRunning;

    void Update()
    {
        if (dogDialogueManager == null || testSceneController == null || dogAnimator == null) return;

        bool isRunning = GetIsRunning(testSceneController);
        if (!isRunning)
        {
            // 不在跑步中：重置状态
            _belowSpeedTimer = 0f;
            _hasTriggeredThisStop = false;
            return;
        }

        float speed = dogAnimator.GetFloat(speedParameterName);

        if (speed < speedThreshold)
        {
            _belowSpeedTimer += Time.deltaTime;

            if (!_hasTriggeredThisStop &&
                _belowSpeedTimer >= stoppedSecondsThreshold &&
                Time.time >= _nextAllowedTime)
            {
                dogDialogueManager.Speak(message, priority);
                _hasTriggeredThisStop = true;
                _nextAllowedTime = Time.time + Mathf.Max(0.1f, cooldownSeconds);
            }
        }
        else
        {
            // 一旦速度恢复，认为本次"停顿事件"结束，允许下次再次触发
            _belowSpeedTimer = 0f;
            _hasTriggeredThisStop = false;
        }
    }

    /// <summary>
    /// 读取 TestSceneController 私有字段 isRunning（不改原脚本的前提下实现监听）
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
