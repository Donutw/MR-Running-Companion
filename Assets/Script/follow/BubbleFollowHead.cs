using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class BubbleFollowHead : MonoBehaviour
{
    [Header("高度设置")]
    [Tooltip("跑步中小狗的高度偏移")]
    public float heightOffset = 0.35f;
    [Tooltip("结束时（小狗站起）的高度偏移")]
    public float finishHeightOffset = 0.7f; // 建议设高一点，避开站起的模型
    
    [Header("平滑过渡")]
    public float smoothSpeed = 5f; // 气泡升高的速度

    [Header("引用")]
    public Transform headAnchor;
    public Camera viewCamera;

    private bool _isFinished = false;
    private float _targetOffset;
    private Transform _transform;

    void Awake()
    {
        _transform = transform;
        if (viewCamera == null) viewCamera = Camera.main;
        _targetOffset = heightOffset; // 初始为跑步高度
    }

    /// <summary>
    /// 公开接口：由 Controller 在 EndTest 时调用
    /// </summary>
    public void SetFinishMode()
    {
        _isFinished = true;
        _targetOffset = finishHeightOffset;
    }

    void LateUpdate()
    {
        if (headAnchor == null || viewCamera == null) return;

        // 1. 位置计算：使用 Lerp 实现高度平滑上升
        float currentOffset = Mathf.Lerp(heightOffset, _targetOffset, _isFinished ? 1f : 0f); 
        // 实际上如果你想更丝滑，可以用时间累加来实现真正的平滑，这里为了逻辑简单：
        Vector3 targetPos = headAnchor.position + Vector3.up * _targetOffset;
        
        // 气泡平滑移动到目标点
        _transform.position = Vector3.Lerp(_transform.position, targetPos, Time.deltaTime * smoothSpeed);

        // 2. 旋转修正（保持原来的逻辑）
        Vector3 directionToCamera = viewCamera.transform.position - _transform.position;
        directionToCamera.y = 0f; 
        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera.normalized);
            _transform.rotation = targetRotation * Quaternion.Euler(0, 180, 0); 
        }
    }
}