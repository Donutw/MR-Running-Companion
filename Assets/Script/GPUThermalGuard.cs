using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 显卡过热防护：限制帧率、可选降低画质，减轻 GPU 负载。
/// 挂到场景里任意常驻物体（如 GameManager）上即可生效。
/// </summary>
public class GPUThermalGuard : MonoBehaviour
{
    [Header("降温模式")]
    [Tooltip("勾选后启用：限制帧率并可选降低画质，减轻显卡负载")]
    public bool coolingMode = true;

    [Header("帧率限制")]
    [Tooltip("降温模式下允许的最高帧率（0 = 不限制）")]
    [Range(0, 120)]
    public int targetFrameRate = 60;

    [Header("画质")]
    [Tooltip("降温模式下使用的画质等级名（留空则不自动改画质）")]
    public string coolingQualityName = "Performant";

    [Header("运行时切换")]
    [Tooltip("按此键在「正常/降温」模式间切换")]
    public KeyCode toggleKey = KeyCode.F11;

    int _savedFrameRate;
    int _savedQualityIndex;

    void Start()
    {
        _savedFrameRate = Application.targetFrameRate;
        _savedQualityIndex = QualitySettings.GetQualityLevel();
        ApplyCoolingState();
    }

    void Update()
    {
        // 检查 F11 键是否按下 (切换降温模式)
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
        {
            coolingMode = !coolingMode;
            ApplyCoolingState();
        }
    }

    void ApplyCoolingState()
    {
        if (coolingMode)
        {
            Application.targetFrameRate = targetFrameRate > 0 ? targetFrameRate : 0;
            if (!string.IsNullOrEmpty(coolingQualityName))
            {
                int idx = GetQualityIndexByName(coolingQualityName);
                if (idx >= 0)
                    QualitySettings.SetQualityLevel(idx);
            }
        }
        else
        {
            Application.targetFrameRate = _savedFrameRate;
            QualitySettings.SetQualityLevel(_savedQualityIndex);
        }
    }

    static int GetQualityIndexByName(string name)
    {
        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
            if (names[i] == name) return i;
        return -1;
    }

    void OnValidate()
    {
        if (Application.isPlaying && coolingMode)
            Application.targetFrameRate = targetFrameRate > 0 ? targetFrameRate : 0;
    }
}
