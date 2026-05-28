using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 气泡打字对话。挂载在「始终保持激活」的根节点上（例如 Bubble），
/// bubbleContainer 填要被显示/隐藏的子物体（例如 Image 或包含背景+尾巴的容器），
/// 这样隐藏气泡时不会把本脚本所在物体关掉，才能反复播放。
/// 显示/隐藏带渐变，打字在气泡完全出现后开始，消失时文字与气泡同步渐变。
/// </summary>
public class TypeBubble : MonoBehaviour
{
    [Header("引用")]
    public TextMeshProUGUI textMesh;
    [Tooltip("要显示/隐藏的物体（背景或背景+尾巴的父物体）。若没有 CanvasGroup 会自动添加用于渐变。")]
    public GameObject bubbleContainer;

    [Header("节奏")]
    public float typeSpeed = 0.05f;
    public float baseStayDuration = 1.5f;
    public float stayPerCharacter = 0.1f;

    [Header("渐变")]
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.25f;

    [Header("常驻模式")]
    [Tooltip("为 true 时，本轮对话在打字完成后不会自动渐隐和隐藏，需要外部手动关闭。")]
    public bool isPermanent = false;

    /// <summary>
    /// 完整执行一轮：气泡渐显 → 打字机效果 → 按字数停留 → 气泡与文字同步渐隐。
    /// </summary>
    public void PlayDialogue(string content)
    {
        StopAllCoroutines();
        StartCoroutine(PlayDialogueRoutine(content));
    }

    /// <summary>
    /// 兼容旧用法，等同于 PlayDialogue(content)。
    /// </summary>
    public void Speak(string content)
    {
        PlayDialogue(content);
    }

    IEnumerator PlayDialogueRoutine(string content)
    {
        bubbleContainer.SetActive(true);
        textMesh.text = "";

        CanvasGroup cg = bubbleContainer.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = bubbleContainer.AddComponent<CanvasGroup>();

        cg.alpha = 0f;

        // 1. 渐显
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeInDuration;
            cg.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        cg.alpha = 1f;

        // 2. 打字机效果
        foreach (char letter in content.ToCharArray())
        {
            textMesh.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        // 如果当前设置为“常驻模式”，打字完成后就保持显示，不再执行自动停留 + 渐隐逻辑
        if (isPermanent)
        {
            yield break;
        }

        // 3. 停留
        float showDuration = baseStayDuration + (content.Length * stayPerCharacter);
        yield return new WaitForSeconds(showDuration);

        // 4. 渐隐（文字与气泡同步）
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / fadeOutDuration;
            cg.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        cg.alpha = 0f;
        bubbleContainer.SetActive(false);
    }
}
