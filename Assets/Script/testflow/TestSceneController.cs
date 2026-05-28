using UnityEngine;
using TMPro;

/// <summary>
/// 总控制脚本：负责测试流程切换、UI显隐、以及指挥所有数据计算器。
/// </summary>
public class TestSceneController : MonoBehaviour
{
    [Header("测试设置")]
    [Tooltip("勾选代表当前是Test A，不勾选代表是Test B")]
    public bool isTestA = true; // 【新增】用来区分这个场景是A还是B

    [Tooltip("测试时长（秒）")]
    public float testDuration = 180f;

    [Header("UI 面板")]
    public GameObject startPanel;
    public GameObject dataHUDPanel;
    public GameObject resultPanel;

    [Header("显示组件")]
    public TextMeshProUGUI timerText;
    public GameObject[] handVisuals;

    [Header("对话系统引用")]
    public DogDialogueManager dogDialogueManager;
    public Animator dogAnimator;

    [Header("对话内容")]
    [TextArea(2, 4)] // 这样在 Inspector 里会变成一个大文本框
    public string startMessage = "Ready, go go go!";
    public int startPriority = 1; 

    [Space(10)] // 在面板里留一点空隙，好看
    [TextArea(2, 4)]
    public string endMessage = "Good job! Let's take a break.";
    public int endPriority = 1;

    // --- 私有变量：后台引用的数据计算器 ---
    private DistanceCalculator _distCalc;
    private PaceCalculator _paceCalc;
    private CalorieCalculator _calCalc;
    private CSVLogger _csvLogger; // 【插入1】：声明CSV记录器
    
    private float currentTime;
    private bool isRunning = false;

    void Start()
    {
        // 1. 自动寻找场景中的“计算器员工”
        _distCalc = FindObjectOfType<DistanceCalculator>();
        _paceCalc = FindObjectOfType<PaceCalculator>();
        _calCalc = FindObjectOfType<CalorieCalculator>();
        _csvLogger = FindObjectOfType<CSVLogger>(); // 【插入2】：自动寻找记录器

        // 2. 初始化 UI 状态
        if (startPanel != null) startPanel.SetActive(true);
        if (dataHUDPanel != null) dataHUDPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        // 3. 初始计时
        currentTime = testDuration;
        isRunning = false;
        UpdateTimerDisplay();

        // 4. 确保手部模型可见
        SetHandVisuals(true);

        // 5. 开场白（使用常驻台词，不自动消失，直到开始跑步时手动清空）
        if (dogDialogueManager != null)
        {
            dogDialogueManager.SpeakPermanent(startMessage, startPriority);
        }
    }

    /// <summary>
    /// 被开始按钮调用的公开方法
    /// </summary>
    public void StartRunning()
    {
        if (isRunning) return;

        // A. 切换面板
        if (startPanel != null) startPanel.SetActive(false);
        if (dataHUDPanel != null) dataHUDPanel.SetActive(true);

        // B. 隐藏手部模型（减少干扰）
        SetHandVisuals(false);

        // C. 指挥所有计算器：开始录制！
        if (_distCalc != null) _distCalc.StartRecording();
        if (_paceCalc != null) _paceCalc.StartRecording();
        if (_calCalc != null) _calCalc.StartRecording();
        if (_csvLogger != null) _csvLogger.StartLogging(); // 【插入3】：通知记录器开始记账

        // D. 对话系统准备：停止开场白（常驻台词需要手动清空）
        if (dogDialogueManager != null) dogDialogueManager.ForceStopSpeaking();

        // E. 动画与计时
        if (dogAnimator != null) dogAnimator.SetTrigger("Start");
        currentTime = testDuration;
        isRunning = true;
    }

    void Update()
    {
        if (isRunning)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();

            // 注意：时间同步已由 Trigger_Time 脚本通过反射自动读取，无需手动同步

            if (currentTime <= 0)
            {
                EndTest();
            }
        }
    }

    private void EndTest()
    {
        if (!isRunning) return;
        isRunning = false;
        currentTime = 0f;

        // 1. 【物理关停】彻底禁用所有“哨兵”脚本，从源头切断心率报警等噪音
        MonoBehaviour[] allScripts = GetComponents<MonoBehaviour>();
        foreach (var s in allScripts)
        {
            if (s.GetType().Name.StartsWith("Trigger_"))
            {
                s.enabled = false; 
            }
        }

        // 2. 【强行清场】清空当前可能正在排队或打字的所有对话气泡
        if (dogDialogueManager != null)
        {
            dogDialogueManager.ForceStopSpeaking();
        }

        // ================= 【核心修改：顺序提前】 =================
        // 先让所有计算器和记录员“停笔算账”，必须在打开面板之前完成！
        if (_distCalc != null) _distCalc.StopRecording();
        if (_paceCalc != null) _paceCalc.StopRecording();
        if (_calCalc != null) _calCalc.StopRecording();

        if (_csvLogger != null)
        {
            _csvLogger.StopAndExportCSV(isTestA); // 这里面会算出平均心率和配速
            Debug.Log("测试结束：数据已冻结，CSV 文件已生成！");
        }
        // ==========================================================

        // 3. UI 面板切换（此时结算面板再弹出来，去拿到的数据就是刚刚算好的了）
        if (dataHUDPanel != null) dataHUDPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);
        SetHandVisuals(true);

        // 4. 气泡高度补偿
        BubbleFollowHead bubbleFollow = FindObjectOfType<BubbleFollowHead>();
        if (bubbleFollow != null)
        {
            bubbleFollow.SetFinishMode();
        }

        // 5. 动画锁定
        if (dogAnimator != null)
        {
            dogAnimator.SetBool("isFinished", true); 
            dogAnimator.SetTrigger("Finish");
        }

        // 6. 终章陈词
        if (dogDialogueManager != null)
        {
            dogDialogueManager.SpeakPermanent(endMessage, 0);
        }

        // 7. 向 DataManager 汇报完成状态
        if (DataManager.Instance != null)
        {
            if (isTestA)
            {
                DataManager.Instance.isTestAFinished = true;
            }
            else
            {
                DataManager.Instance.isTestBFinished = true;
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    /// <summary>
    /// 控制所有手的显示/隐藏，并且确保不会每隔一段时间留下幽灵手（残影）。
    /// 彻底隐藏时直接 SetActive(false)，同时可以用 Destroy 清理掉残留实例（如果你发现还有漏掉的）。
    /// </summary>
    private void SetHandVisuals(bool visible)
    {
        if (handVisuals == null) return;

        foreach (var hand in handVisuals)
        {
            if (hand == null) continue;

            // 如果要彻底隐身，直接关闭 GameObject（推荐）
            if (!visible)
            {
                hand.SetActive(false); // 最彻底，所有残余组件都不会再显示/渲染
            }
            else
            {
                hand.SetActive(true);

                // 可选：确保所有 Renderer 和 Collider 都被重新开启
                var renderers = hand.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }

                var colliders = hand.GetComponentsInChildren<Collider>(true);
                foreach (var collider in colliders)
                {
                    collider.enabled = true;
                }
            }
        }
    }
}