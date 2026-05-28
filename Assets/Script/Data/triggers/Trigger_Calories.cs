using UnityEngine;

/// <summary>
/// 触发器：卡路里关卡夸奖
/// 监听 CalorieCalculator.totalCalories，达到关卡时触发夸奖。
/// 优先级建议：2（常规成就）
/// </summary>
public class Trigger_Calories : MonoBehaviour
{
    [Header("引用（手动拖拽，数据解耦）")]
    public DogDialogueManager dogDialogueManager;
    public CalorieCalculator calorieCalculator;

    [Header("关卡（kcal）")]
    [Tooltip("建议按从小到大填写，例如 10 / 20 / 50。每个值只触发一次。")]
    public int[] milestonesKcal = new int[] { 10, 20, 50 };

    [Header("播报内容")]
    [Tooltip("支持 {0}：会替换成关卡 kcal 数")]
    [TextArea(3, 5)] // 这会让输入框变成 3 到 5 行高
    public string messageFormat = "厉害！已消耗 {0} kcal！";

    public int priority = 2;

    // 防刷：用"下一个关卡索引"保证单次触发
    private int _nextIndex = 0;

    void Update()
    {
        if (dogDialogueManager == null || calorieCalculator == null) return;
        if (!calorieCalculator.isRecording) return;
        if (milestonesKcal == null || milestonesKcal.Length == 0) return;

        int kcal = Mathf.FloorToInt(calorieCalculator.totalCalories);

        while (_nextIndex < milestonesKcal.Length && kcal >= milestonesKcal[_nextIndex])
        {
            int milestone = milestonesKcal[_nextIndex];
            string msg = string.Format(messageFormat, milestone);
            dogDialogueManager.Speak(msg, priority);
            _nextIndex++;
        }
    }
}
