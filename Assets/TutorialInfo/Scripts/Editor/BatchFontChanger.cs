using UnityEngine;
using UnityEditor;
using TMPro;

public class BatchFontChanger : EditorWindow
{
    public TMP_FontAsset newFont;

    [MenuItem("Tools/批量更换字体 (Font Changer)")]
    public static void ShowWindow()
    {
        GetWindow<BatchFontChanger>("字体更换器");
    }

    void OnGUI()
    {
        GUILayout.Label("选择你新生成的字体资源 (SDF):", EditorStyles.boldLabel);
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField(newFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("一键替换当前场景所有字体"))
        {
            if (newFont == null)
            {
                Debug.LogError("请先拖入一个字体资源！");
                return;
            }

            // 寻找场景中所有的 TMP 组件（包括禁用的物体）
            TextMeshProUGUI[] texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            int count = 0;

            foreach (var text in texts)
            {
                // 确保只修改当前场景中的物体，排除 Prefab 原始资源
                if (text.gameObject.scene.name != null)
                {
                    Undo.RecordObject(text, "Batch Font Change"); // 允许撤销 (Ctrl+Z)
                    text.font = newFont;
                    EditorUtility.SetDirty(text); // 标记已修改
                    count++;
                }
            }

            // 同时也查找非 UI 的 TextMeshPro (World Space 里的)
            TextMeshPro[] worldTexts = Resources.FindObjectsOfTypeAll<TextMeshPro>();
            foreach (var wText in worldTexts)
            {
                if (wText.gameObject.scene.name != null)
                {
                    Undo.RecordObject(wText, "Batch Font Change");
                    wText.font = newFont;
                    EditorUtility.SetDirty(wText);
                    count++;
                }
            }

            Debug.Log($"成功替换了 {count} 个物体的字体！");
        }
    }
}