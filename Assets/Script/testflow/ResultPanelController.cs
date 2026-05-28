using UnityEngine;
using TMPro;

public class ResultPanelController : MonoBehaviour
{
    [Header("结算数据展示文本（仅显示数值）")]
    public TextMeshProUGUI timeText;     
    public TextMeshProUGUI distanceText; 
    public TextMeshProUGUI paceText;     
    public TextMeshProUGUI hrText;       
    public TextMeshProUGUI calText;      

    void OnEnable()
    {
        // 1. 自动寻找数据源
        CSVLogger logger = FindObjectOfType<CSVLogger>();
        DistanceCalculator distCalc = FindObjectOfType<DistanceCalculator>();
        CalorieCalculator calCalc = FindObjectOfType<CalorieCalculator>();
        TestSceneController testScene = FindObjectOfType<TestSceneController>();

        // 2. 填入时间 (格式：03:00)
        if (timeText != null && testScene != null)
        {
            int totalMin = Mathf.FloorToInt(testScene.testDuration / 60);
            int totalSec = Mathf.FloorToInt(testScene.testDuration % 60);
            timeText.text = $"{totalMin:00}:{totalSec:00}";
        }

        // 3. 填入距离 (仅数值)
        if (distanceText != null && distCalc != null)
        {
            distanceText.text = $"{distCalc.currentDistanceMeters:F0}";
        }

        // 4. 填入卡路里 (仅数值)
        if (calText != null && calCalc != null)
        {
            calText.text = $"{calCalc.totalCalories:F0}";
        }

        // 5. 填入平均心率和配速 (仅数值)
        if (logger != null)
        {
            if (hrText != null) 
            {
                hrText.text = $"{logger.averageHR:F0}";
            }

            if (paceText != null) 
            {
                int pMin = Mathf.FloorToInt(logger.averagePace);
                int pSec = Mathf.FloorToInt((logger.averagePace - pMin) * 60f);
                paceText.text = $"{pMin:00}'{pSec:00}\"";
            }
        }
    }
}