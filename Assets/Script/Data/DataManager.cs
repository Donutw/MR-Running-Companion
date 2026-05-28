using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    [Header("Data Storage")]
    public int participantID = 1;
    public bool isTestAFinished = false;
    public bool isTestBFinished = false;

    private HeartRateBleReader _bleReader;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 只在这里写一次
            _bleReader = GetComponent<HeartRateBleReader>();
        }
        else { Destroy(gameObject); }
    }

    public int GetCurrentBpm() => _bleReader != null ? _bleReader.LastHeartRateBpm : -1;

    // 以后我们要在这里写 SaveToCSV() 函数
}