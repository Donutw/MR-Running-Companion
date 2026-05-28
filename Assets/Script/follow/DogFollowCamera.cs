using UnityEngine;

/// <summary>
/// 终极修正版小狗跟随逻辑：
/// 1. 修复 Vector2.Distance 语法错误。
/// 2. 增加移动门槛 (Move Threshold)，防止频繁微调。
/// 3. 区分移动(面向前方)与静止(面向用户+180度修正)。
/// </summary>
[RequireComponent(typeof(Animator))]
public class DogFollowCamera : MonoBehaviour
{
    [Header("VR / 视角")]
    public Camera viewCamera;

    [Header("防抖设置")]
    [Tooltip("视线方向平滑度，越小防抖越强（建议值: 2~4）")]
    public float headTurnSmoothSpeed = 3f;
    // 内部用来记录平滑后的方向
    private Vector3 _smoothedCamForward = Vector3.zero;
    
    [Header("目标点")]
    public float targetBearingDegrees = 30f;
    public float targetDistance = 1.2f;
    public float groundY = 0f;

    [Header("移动门槛")]
    [Tooltip("偏离超过此距离才开始移动（米）")]
    public float moveThreshold = 0.5f;
    [Tooltip("接近到此距离内就停止（米）")]
    public float stopThreshold = 0.1f;

    [Header("移动平滑")]
    public float runSmoothTime = 0.3f;
    public float idleSmoothTime = 0.8f;
    public float maxSpeed = 2.5f;

    [Header("动画状态")]
    public string speedParameterName = "Speed";
    public string runGroupName = "Run_group"; 
    public float speedScale = 2.5f;

    [Header("转向")]
    public float lookAtUserDistance = 0.4f;
    public float rotationSmooth = 6f;
    public bool reverseLookAtUser = true;

    Transform _transform;
    Animator _animator;
    int _speedParamId;
    int _runGroupHash;
    Camera _camera;
    Vector3 _velocityXZ = Vector3.zero;
    bool _isMoving = false; 

    void Awake()
    {
        _transform = transform;
        _animator = GetComponent<Animator>();
        _speedParamId = Animator.StringToHash(speedParameterName);
        _runGroupHash = Animator.StringToHash(runGroupName);
    }

    void Start() { ResolveCamera(); }

    void Update()
    {
        if (_camera == null) { ResolveCamera(); if (_camera == null) return; }

        float dt = Time.deltaTime;
        Vector3 pos = _transform.position;

        // 1. 计算理想目标点 (加入头部防抖机制)
        Vector3 rawCamForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
        if (rawCamForward.sqrMagnitude < 0.001f) rawCamForward = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
        // 如果是第一帧，直接对齐
        if (_smoothedCamForward == Vector3.zero) _smoothedCamForward = rawCamForward;
        // 核心防抖计算：让平滑方向慢慢追赶真实的头部方向
        _smoothedCamForward = Vector3.Slerp(_smoothedCamForward, rawCamForward, headTurnSmoothSpeed * dt);
        // 使用平滑后的方向计算最终目标点
        Vector3 targetWorld = _camera.transform.position + (Quaternion.Euler(0, targetBearingDegrees, 0) * _smoothedCamForward) * targetDistance;
        targetWorld.y = groundY;

        // 2. 移动门槛逻辑 (修正了此处的 Vector2.Distance 语法)
        float distToTarget = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(targetWorld.x, targetWorld.z));
        
        if (!_isMoving && distToTarget > moveThreshold) {
            _isMoving = true; 
        } else if (_isMoving && distToTarget < stopThreshold) {
            _isMoving = false; 
        }

        // 3. 状态判定
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        bool isInRunGroup = stateInfo.shortNameHash == _runGroupHash;

        // 4. 执行移动
        Vector3 newPos = pos;
        if (_isMoving)
        {
            float currentSmooth = isInRunGroup ? runSmoothTime : idleSmoothTime;
            
            // 【橡皮筋逻辑核心】
            // 基础限速由 Inspector 面板里的 maxSpeed 决定（建议在 Unity 里填 2.0 或 2.5）。
            // 乘数 1.5f 可以调整橡皮筋的“弹力”。距离越远，允许的临时极速就越高。
            float dynamicMaxSpeed = Mathf.Max(maxSpeed, distToTarget * 1.5f);

            // 使用 dynamicMaxSpeed 替代原来的 maxSpeed
            Vector3 nextXZ = Vector3.SmoothDamp(
                new Vector3(pos.x, 0, pos.z), 
                new Vector3(targetWorld.x, 0, targetWorld.z), 
                ref _velocityXZ, 
                currentSmooth, 
                dynamicMaxSpeed, 
                dt
            );
            newPos = new Vector3(nextXZ.x, groundY, nextXZ.z);
        }

        float moveSpeed = (newPos - pos).magnitude / Mathf.Max(dt, 0.001f);
        _transform.position = newPos;
        _animator.SetFloat(_speedParamId, Mathf.Clamp(moveSpeed * speedScale, 0f, 3f));

        // 5. 转向逻辑
        Vector3 desiredForwardXZ;
        bool shouldFix180 = false;

        if (moveSpeed > 0.1f && isInRunGroup) 
        {
            desiredForwardXZ = _velocityXZ.normalized;
            shouldFix180 = false; 
        }
        else 
        {
            Vector3 toUser = _camera.transform.position - _transform.position;
            toUser.y = 0;
            desiredForwardXZ = toUser.normalized;
            shouldFix180 = reverseLookAtUser;
        }

        if (desiredForwardXZ.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredForwardXZ);
            if (shouldFix180) targetRot *= Quaternion.Euler(0, 180, 0);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRot, rotationSmooth * dt);
        }
    }

    void ResolveCamera() { _camera = viewCamera != null ? viewCamera : Camera.main; }
}