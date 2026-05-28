using UnityEngine;

/// <summary>
/// 虚拟狗对话中间人：
/// - 负责“交通管制”：根据优先级决定哪句台词能播。
/// - 优先级定义：数字越小级别越高（0: 紧急/安全, 1: 节点提醒, 2: 鼓励/成就, 3: 闲聊）
/// </summary>
public class DogDialogueManager : MonoBehaviour
{
    [Header("气泡与动画引用")]
    public TypeBubble typeBubble;
    public Animator dogAnimator;

    // 内部状态
    private int _currentPriority = 99; // 当前正在播报的优先级，初始设为最低

    void Awake()
    {
        if (typeBubble == null) typeBubble = GetComponentInChildren<TypeBubble>();
        if (dogAnimator == null) dogAnimator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// 带优先级的「普通台词」接口（会按照 TypeBubble 的节奏自动消失）
    /// </summary>
    /// <param name="message">台词内容</param>
    /// <param name="priority">优先级（0最高，3最低）</param>
    public void Speak(string message, int priority)
    {
        if (typeBubble == null) return;

        // 普通台词：确保不是常驻模式
        typeBubble.isPermanent = false;

        // 如果狗正在说话
        if (dogAnimator.GetBool("isSpeaking"))
        {
            // 只有当新消息的优先级 [小于] 当前优先级时，才允许打断或覆盖
            if (priority < _currentPriority)
            {
                // 允许打断：先强行停止当前的
                typeBubble.StopAllCoroutines();
                Debug.Log($"[Manager] 高优先级({priority})打断了低优先级({_currentPriority})");
            }
            else
            {
                // 拒绝播报：当前正在说更重要的事
                Debug.Log($"[Manager] 拒绝低优先级请求({priority})，当前正处于级别({_currentPriority})");
                return;
            }
        }

        // 执行播报
        _currentPriority = priority;
        typeBubble.PlayDialogue(message);
        SetSpeaking(true);
    }

    /// <summary>
    /// 带优先级的「常驻台词」接口：
    /// - 使用 TypeBubble 的打字 + 渐入效果；
    /// - 但在打字完成后不会自动渐隐/隐藏，由外部（如 ForceStopSpeaking）来手动关闭；
    /// - 仍然遵守优先级规则，可被更高或相同优先级的台词打断。
    /// </summary>
    public void SpeakPermanent(string message, int priority)
    {
        if (typeBubble == null) return;

        // 如果狗正在说话
        if (dogAnimator.GetBool("isSpeaking"))
        {
            // 只有当新消息的优先级 [小于] 当前优先级时，才允许打断或覆盖
            if (priority < _currentPriority)
            {
                typeBubble.StopAllCoroutines();
                Debug.Log($"[Manager] 高优先级常驻({priority})打断了低优先级({_currentPriority})");
            }
            else
            {
                Debug.Log($"[Manager] 拒绝低优先级常驻请求({priority})，当前正处于级别({_currentPriority})");
                return;
            }
        }

        // 执行常驻播报：开启常驻模式
        _currentPriority = priority;
        typeBubble.isPermanent = true;
        typeBubble.PlayDialogue(message);
        SetSpeaking(true);
    }

    public void ForceStopSpeaking()
    {
        if (typeBubble != null)
        {
            typeBubble.StopAllCoroutines();
            if (typeBubble.bubbleContainer != null) typeBubble.bubbleContainer.SetActive(false);
        }
        SetSpeaking(false);
        _currentPriority = 99; // 重置优先级
    }

    void Update()
    {
        if (typeBubble != null && typeBubble.bubbleContainer != null)
        {
            // 如果气泡自动消失了，重置优先级并让狗闭嘴
            if (!typeBubble.bubbleContainer.activeSelf)
            {
                SetSpeaking(false);
                _currentPriority = 99; 
            }
        }
    }

    private void SetSpeaking(bool isSpeaking)
    {
        if (dogAnimator != null) dogAnimator.SetBool("isSpeaking", isSpeaking);
    }
}