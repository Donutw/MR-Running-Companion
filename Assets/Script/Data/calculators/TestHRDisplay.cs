using UnityEngine;
using TMPro;

public class TestHRDisplay : MonoBehaviour
{
    [Header("UI 设置")]
    [Tooltip("把你想显示心率的文字物体拖到这里")]
    public TextMeshProUGUI hrText;
    
    [Tooltip("设置文字格式，{0} 会被替换成具体的心率数字")]
    public string displayFormat = "HR: {0} bpm";

    [Header("调试信息 (自动获取，无需拖拽)")]
    [Tooltip("脚本运行时会自动找到从 Menu 跟过来的大管家")]
    // 用 public 让你可以看到它找没找到，但不需要你手动拖拽
    private HeartRateBleReader bleReader; 

    void Start()
    {
        // 游戏开始时，自动在场景里寻找那个从 Menu 带来的后台脚本
        bleReader = FindObjectOfType<HeartRateBleReader>();

        if (hrText != null)
        {
            hrText.text = "Waiting for HR data...";
        }
    }

    void Update()
    {
        if (bleReader == null || hrText == null) return;

        // 如果连接正常，并且已经收到了心率数据
        if (bleReader.isConnected && bleReader.LastHeartRateBpm > 0)
        {
            hrText.color = Color.white; // 正常颜色
            
            // 使用你自定义的格式来显示数字！
            hrText.text = string.Format(displayFormat, bleReader.LastHeartRateBpm);
        }
        // 如果断开了
        else
        {
            hrText.color = Color.red; // 变红警示
            
            // 依然保持最后一次读取的数字，防止格式突变，也可以选择在后面加提示
            // 比如这里我加了一个极简的 ( ! ) 表示连接断开
            if (bleReader.LastHeartRateBpm > 0)
            {
                hrText.text = string.Format(displayFormat, bleReader.LastHeartRateBpm) + " (!)";
            }
            else
            {
                hrText.text = "HR: Disconnected";
            }
        }
    }
}