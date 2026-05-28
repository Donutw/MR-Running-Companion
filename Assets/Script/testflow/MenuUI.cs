using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [Header("UI 组件")]
    public TextMeshProUGUI logText; 
    public GameObject testButtonsGroup; 
    
    [Header("编号选择 UI (用于控制连接前后的显隐)")]
    public GameObject idSelectionGroup; // 【新增】把包含加减按钮和数字文字的父物体拖进来

    [Header("测试按钮 (用于做完后置灰)")]
    public Button buttonTestA; 
    public Button buttonTestB; 

    [Header("编号选择 文字")]
    public TextMeshProUGUI idDisplayText; 

   private HeartRateBleReader bleReader; 

    void Start()
    {
        // 2. 【核心新增】一出生，立刻去全局管家那里认领心率读取器
        if (DataManager.Instance != null)
        {
            bleReader = DataManager.Instance.GetComponent<HeartRateBleReader>();
        }

        // 3. 初始隐藏
        if (testButtonsGroup != null) testButtonsGroup.SetActive(false);
        if (idSelectionGroup != null) idSelectionGroup.SetActive(false); 

        // 4. 【核心新增】为了消灭那一瞬间的 "New Text"
        // 我们在 Start 的第一帧，就强行根据心率状态刷一次 UI
        if (bleReader != null && bleReader.isConnected)
        {
            // 如果从测试场景回来时心率还连着，直接显示选关和编号，隐藏日志
            if (logText != null) logText.gameObject.SetActive(false);
            if (testButtonsGroup != null) testButtonsGroup.SetActive(true);
            if (idSelectionGroup != null) idSelectionGroup.SetActive(true);
        }
        else
        {
            // 如果断开了，显示日志，准备重新连接
            if (logText != null) 
            {
                logText.gameObject.SetActive(true);
                // 给个默认提示，盖掉 "New Text"
                logText.text = "Waiting for heart rate monitor..."; 
            }
        }
        
        UpdateIDUI();
        CheckTestsStatus(); 
    }
    void Update()
    {
        if (bleReader == null) return;

        if (!bleReader.isConnected)
        {
            // 心率未连接时：显示Log，隐藏测试按钮和编号UI
            if (logText != null)
            {
                logText.gameObject.SetActive(true);
                logText.text = bleReader.currentLog;
            }
            if (testButtonsGroup != null) testButtonsGroup.SetActive(false);
            if (idSelectionGroup != null) idSelectionGroup.SetActive(false); 
        }
        else
        {
            // 心率连接成功后：隐藏Log，显示测试按钮和编号UI
            if (logText != null) logText.gameObject.SetActive(false);
            if (testButtonsGroup != null) testButtonsGroup.SetActive(true);
            if (idSelectionGroup != null) idSelectionGroup.SetActive(true); 
        }
    }

    public void ChangeID(int delta)
    {
        if (DataManager.Instance != null)
        {
            // 1. 改变编号
            DataManager.Instance.participantID += delta;
            
            // 2. 限制不能减到 1 以下
            if (DataManager.Instance.participantID < 1) 
            {
                DataManager.Instance.participantID = 1; 
            }
            
            // 3. 【核心新增】既然换了新编号，说明是新的被试，必须重置测试完成状态！
            // 注意：这代表如果你不小心点错了加减号，状态会被清空。
            // 但在实际实验流程中，做完一个人再切下一个，这是最符合逻辑的。
            DataManager.Instance.isTestAFinished = false;
            DataManager.Instance.isTestBFinished = false;
            
            // 4. 刷新数字显示
            UpdateIDUI();

            // 5. 【核心新增】立刻刷新按钮的置灰状态（让灰掉的按钮重新亮起来）
            CheckTestsStatus();
        }
    }
    

    private void UpdateIDUI()
    {
        if (idDisplayText != null && DataManager.Instance != null)
        {
            idDisplayText.text = DataManager.Instance.participantID.ToString("D2");
        }
    }

    private void CheckTestsStatus()
    {
        if (DataManager.Instance != null)
        {
            if (buttonTestA != null)
            {
                buttonTestA.interactable = !DataManager.Instance.isTestAFinished;
            }

            if (buttonTestB != null)
            {
                buttonTestB.interactable = !DataManager.Instance.isTestBFinished;
            }
        }
    }
}