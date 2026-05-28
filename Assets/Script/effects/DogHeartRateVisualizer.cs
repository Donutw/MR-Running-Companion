using UnityEngine;

public class DogHeartRateVisualizer : MonoBehaviour
{
    [Header("材质与渲染器")]
    [Tooltip("把小狗身上带 SkinnedMeshRenderer 的子物体拖进来")]
    public Renderer dogRenderer;
    [Tooltip("如果小狗有多个材质（比如眼球和身体），填入身体材质的序号，通常是0")]
    public int materialIndex = 0; 

    [Header("颜色设置")]
    public Color normalColor = Color.white; // 正常颜色（通常设为白色，不影响贴图原本颜色）
    public Color alertColor = Color.red;    // 危险颜色（变红）

    [Header("心率阈值")]
    [Tooltip("心率低于这个值时，完全是正常颜色")]
    public float normalHR = 80f;
    [Tooltip("心率高于这个值时，完全变成纯红色")]
    public float dangerHR = 130f;

    [Header("平滑过渡速度")]
    public float colorChangeSpeed = 2f;

    private Material targetMaterial;

    void Start()
    {
        if (dogRenderer != null)
        {
            // 注意：这里用 materials 会自动实例化一个材质，
            // 这样你变红的时候，不会把整个 Unity 工程里的原始材质也给永久染红了
            targetMaterial = dogRenderer.materials[materialIndex];
        }
    }

    void Update()
    {
        // 如果没找到材质，或者大管家还没出生，就不执行
        if (targetMaterial == null || DataManager.Instance == null) return;

        // 1. 获取当前心率
        int currentHR = DataManager.Instance.GetCurrentBpm();

        // 如果心率机还没连上（数值为0或负数），就当作正常心率处理
        if (currentHR <= 0) currentHR = (int)normalHR;

        // 2. 计算危险程度百分比 (Mathf.InverseLerp 会自动把结果限制在 0 到 1 之间)
        // 比如：心率80就是0%，心率105就是50%，心率130及以上就是100%
        float dangerPercentage = Mathf.InverseLerp(normalHR, dangerHR, currentHR);

        // 3. 计算目标颜色
        Color targetColor = Color.Lerp(normalColor, alertColor, dangerPercentage);

        // 4. 平滑过渡：让当前颜色慢慢变成目标颜色，视觉上更自然
        targetMaterial.color = Color.Lerp(targetMaterial.color, targetColor, Time.deltaTime * colorChangeSpeed);
    }
}