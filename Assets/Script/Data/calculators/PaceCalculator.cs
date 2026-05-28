using UnityEngine;
using TMPro;

/// <summary>
/// 配速计算器：计算全局平均配速 (分'秒"/公里)
/// 优化版：降低了UI刷新频率，防止数字狂跳
/// </summary>
public class PaceCalculator : MonoBehaviour
{
    [Header("UI 显示（选填）")]
    public TextMeshProUGUI paceText;
    public string displayFormat = "配速: {0}"; // {0} 会被替换为类似 05'30"

    [Header("刷新设置")]
    [Tooltip("每隔几秒刷新一次面板数字（防止狂跳）")]
    public float updateInterval = 1f;

    [Header("后台数据（供 CSV 读取）")]
    [HideInInspector]
    public string currentPaceString = "--'--\"";
    [HideInInspector]
    public float currentPaceFloat = 0f; 
    [HideInInspector]
    public bool isRecording = false;

    private DistanceCalculator _distanceCalc;
    private float _runningTimeSeconds = 0f;
    private float _uiTimer = 0f; // 专门用来控制 UI 刷新的计时器

    void Start()
    {
        _distanceCalc = FindObjectOfType<DistanceCalculator>();
    }

    void Update()
    {
        if (!isRecording || _distanceCalc == null) return;

        // 后台时间依然每帧精准累加
        _runningTimeSeconds += Time.deltaTime;
        _uiTimer += Time.deltaTime;

        // 【核心优化】：只有攒够了 1 秒，才去计算和改变 UI
        if (_uiTimer >= updateInterval)
        {
            _uiTimer = 0f; // 重新攒时间

            // 跑满 10 米才开始算配速
            if (_distanceCalc.currentDistanceMeters > 10f)
            {
                float distanceKm = _distanceCalc.currentDistanceMeters / 1000f;
                float timeMinutes = _runningTimeSeconds / 60f;
                
                currentPaceFloat = timeMinutes / distanceKm;

                int minutes = Mathf.FloorToInt(currentPaceFloat);
                int seconds = Mathf.FloorToInt((currentPaceFloat - minutes) * 60f);

                // 极端情况保护：如果玩家走得超级慢，算出几百分钟，面板会撑爆。封顶 99'59"
                if (minutes > 99) { minutes = 99; seconds = 59; }

                currentPaceString = string.Format("{0:00}'{1:00}\"", minutes, seconds);
            }

            // 更新文字
            if (paceText != null)
            {
                paceText.text = string.Format(displayFormat, currentPaceString);
            }
        }
    }

    public void StartRecording()
    {
        _runningTimeSeconds = 0f;
        _uiTimer = 0f;
        currentPaceString = "--'--\"";
        currentPaceFloat = 0f;
        isRecording = true;
        
        if (paceText != null) paceText.text = string.Format(displayFormat, currentPaceString);
    }

    public void StopRecording() { isRecording = false; }
}