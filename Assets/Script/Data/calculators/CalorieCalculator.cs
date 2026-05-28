using UnityEngine;
using TMPro;

/// <summary>
/// 卡路里计算器：自动寻找心率读取器，结合运动医学公式，每秒累加燃烧的卡路里。
/// </summary>
public class CalorieCalculator : MonoBehaviour
{
    [Header("UI 显示（选填）")]
    public TextMeshProUGUI calorieText;
    public string displayFormat = "消耗: {0} kcal";

    [Header("身体参数")]
    public float weightKg = 65f;
    public int age = 22; 

    [Header("后台数据（供 CSV 读取）")]
    [HideInInspector]
    public float totalCalories = 0f;
    [HideInInspector]
    public bool isRecording = false;

    private HeartRateBleReader _hrReader;

    void Start()
    {
        // 雷达启动：全场景自动寻找蓝牙心率管家！
        _hrReader = FindObjectOfType<HeartRateBleReader>();
    }

    void Update()
    {
        if (!isRecording) return;

        int currentHR = 80; // 默认静息心率打底
        if (_hrReader != null && _hrReader.isConnected && _hrReader.LastHeartRateBpm > 0)
        {
            currentHR = _hrReader.LastHeartRateBpm;
        }

        // 运动医学经典公式（男性版简化）：每分钟卡路里 = (年龄*0.2017 - 体重*0.09036 + 心率*0.6309 - 55.0969) / 4.184
        // 我们把它分摊到每一秒去累加，这样数字会平滑上涨，视觉效果极佳
        float kcalPerMinute = (age * 0.2017f) - (weightKg * 0.09036f) + (currentHR * 0.6309f) - 55.0969f;
        kcalPerMinute = kcalPerMinute / 4.184f;

        // 如果站着不动心率太低，给一个保底的静息消耗，防止出现负数
        if (kcalPerMinute < 1.0f) kcalPerMinute = 1.0f; 

        // 累加这一帧消耗的卡路里
        totalCalories += kcalPerMinute * (Time.deltaTime / 60f);

        if (calorieText != null)
        {
            // 四舍五入取整显示
            int displayKcal = Mathf.RoundToInt(totalCalories);
            calorieText.text = string.Format(displayFormat, displayKcal);
        }
    }

    public void StartRecording()
    {
        totalCalories = 0f;
        isRecording = true;
    }

    public void StopRecording() { isRecording = false; }
}