using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class CSVLogger : MonoBehaviour
{
    // 私有引用，不在面板显示，靠代码自己找
    private DistanceCalculator distCalc;
    private PaceCalculator paceCalc;
    private CalorieCalculator calCalc;
    
    void Start()
    {
        // 脚本一出生，自动去全场寻找这三个计算器，不用你手动拖了！
        distCalc = FindObjectOfType<DistanceCalculator>();
        paceCalc = FindObjectOfType<PaceCalculator>();
        calCalc = FindObjectOfType<CalorieCalculator>();
    }

    // 定义每一行数据的结构
    [System.Serializable]
    public class LogRow
    {
        public string timestamp;
        public string distance;
        public string heartRate;
        public string pace;
        public string calories;
    }

    private List<LogRow> logData = new List<LogRow>();
    private bool isLogging = false;
    private float startTime;

    // --- 供 UI 读取的统计数据 ---
    public float averageHR { get; private set; }
    public float averagePace { get; private set; }

    /// <summary>
    /// 开始记录数据（由 TestSceneController 在 StartRunning 时调用）
    /// </summary>
    public void StartLogging()
    {
        logData.Clear();
        startTime = Time.time;
        isLogging = true;
        
        // 开启协程，每 1 秒执行一次记录
        StartCoroutine(LogRoutine());
        Debug.Log("CSVLogger: 开始记录数据...");
    }

    /// <summary>
    /// 停止记录并生成 CSV（由 TestSceneController 在 EndTest 时调用）
    /// </summary>
    public void StopAndExportCSV(bool isTestA)
    {
        isLogging = false;
        StopAllCoroutines();
        
        CalculateAverages(); // 停止时顺便算出平均值，供 Result 面板用
        SaveToCSV(isTestA);
    }

    private IEnumerator LogRoutine()
    {
        while (isLogging)
        {
            RecordSingleRow();
            yield return new WaitForSeconds(1f); // 精确等待 1 秒
        }
    }

    private void RecordSingleRow()
    {
        LogRow newRow = new LogRow();

        // 1. 时间戳（记录相对开始的秒数）
        float elapsedTime = Time.time - startTime;
        newRow.timestamp = elapsedTime.ToString("F1");

        // 2. 距离 【修复了这里，改成了 currentDistanceMeters】
        newRow.distance = (distCalc != null) ? distCalc.currentDistanceMeters.ToString("F2") : "";

        // 3. 心率
        if (DataManager.Instance != null)
        {
            int hr = DataManager.Instance.GetCurrentBpm();
            newRow.heartRate = (hr > 0) ? hr.ToString() : ""; // 缺失则留空
        }
        else
        {
            newRow.heartRate = "";
        }

        // 4. 配速 【修复了这里，改成了 currentPaceFloat】
        if (paceCalc != null && paceCalc.currentPaceFloat > 0)
        {
            newRow.pace = paceCalc.currentPaceFloat.ToString("F2");
        }
        else
        {
            newRow.pace = ""; // 缺失则留空
        }

        // 5. 卡路里
        newRow.calories = (calCalc != null) ? calCalc.totalCalories.ToString("F2") : "";

        logData.Add(newRow);
    }

    private void CalculateAverages()
    {
        int hrCount = 0, paceCount = 0;
        float hrSum = 0, paceSum = 0;

        foreach (var row in logData)
        {
            if (int.TryParse(row.heartRate, out int hr))
            {
                hrSum += hr;
                hrCount++;
            }
            if (float.TryParse(row.pace, out float pace))
            {
                paceSum += pace;
                paceCount++;
            }
        }

        averageHR = (hrCount > 0) ? (hrSum / hrCount) : 0;
        averagePace = (paceCount > 0) ? (paceSum / paceCount) : 0;
    }

    private void SaveToCSV(bool isTestA)
    {
        // 获取被试编号
        string participantID = (DataManager.Instance != null) ? DataManager.Instance.participantID.ToString("D3") : "000";
        string condition = isTestA ? "TestA" : "TestB";
        
        // 生成文件名，包含当前真实日期时间防止覆盖
        string dateTimeStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"P{participantID}_{condition}_{dateTimeStr}.csv";

        // Application.persistentDataPath 是 Unity 官方推荐的本地持久化路径，支持 Quest 端读写
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // 写入表头
                writer.WriteLine("Timestamp(s),Distance(m),HeartRate(bpm),Pace(min/km),Calories(kcal)");

                // 写入所有数据行
                foreach (var row in logData)
                {
                    writer.WriteLine($"{row.timestamp},{row.distance},{row.heartRate},{row.pace},{row.calories}");
                }
            }
            Debug.Log($"<color=green>CSV 导出成功！路径: {filePath}</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV 导出失败: {e.Message}");
        }
    }
}