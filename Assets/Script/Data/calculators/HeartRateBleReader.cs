// 依赖：Velorexe Unity-Android-Bluetooth-Low-Energy 插件
// 安装插件后，在 Player Settings > Scripting Define Symbols 中添加 HAS_ANDROID_BLE，即可启用心率功能。
// 未添加时仅为占位，可正常 Build 到 Quest。

using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

#if HAS_ANDROID_BLE
using Android.BLE;
using Android.BLE.Commands;
#endif

/// <summary>
/// 纯后台版：从支持 180D/2A37 的手表接收心率广播。
/// 仅向外提供状态变量，不再直接操作任何 UI。
/// </summary>
public class HeartRateBleReader : MonoBehaviour
{
    [Header("公开状态 (供前台 UI 读取)")]
    [HideInInspector]
    public string currentLog = "Waiting to start..."; // 替代原来的 text.text
    [HideInInspector]
    public bool isConnected = false;                  // 标志是否成功收到心率数据

    [Header("【测试模式】模拟心率（串流测试用）")]
    [Tooltip("勾选后：跳过蓝牙连接，生成模拟心率数据。取消勾选：恢复真实蓝牙连接。")]
    public bool useSimulatedHeartRate = false;

    [Tooltip("模拟心率的起始值（静息心率，例如 70）")]
    public int simulatedStartBpm = 70;

    [Tooltip("模拟心率的最大值（运动峰值，例如 160）")]
    public int simulatedMaxBpm = 160;

    [Tooltip("模拟心率变化速度（每秒增加多少，例如 2）")]
    public float simulatedIncreaseRate = 2f;

    [Tooltip("模拟心率的随机波动范围（±波动值，例如 5）")]
    public int simulatedRandomVariation = 5;

    [Header("Scan Filter")]
    [Tooltip("Only connect if device name contains this (empty = first device)")]
    public string deviceNameFilter = "HUAWEI Band HR-101";

    [Header("Retry & Debug")]
    [Tooltip("断开后是否自动重试（扫描或直连上次设备）")]
    public bool autoRetryOnDisconnect = true;
    [Tooltip("断开后等几秒再重试（给手表重新广播、BLE 释放时间，建议 6～10）")]
    public float retryWaitSeconds = 5f;
    [Tooltip("连上后延迟几秒再订阅心率（可减轻 Status 19，建议 1～2）")]
    public float subscribeDelaySeconds = 1.5f;
    [Tooltip("在 Console 输出连接/断开/扫描结果")]
    public bool logToConsole = true;

#if HAS_ANDROID_BLE
    const string HeartRateServiceUuid = "0000180d-0000-1000-8000-00805f9b34fb";
    const string HeartRateMeasurementUuid = "00002a37-0000-1000-8000-00805f9b34fb";

    string _connectedDeviceAddress;
    string _lastConnectedAddress; // 上次成功连上的设备，重试时优先直连
    ConnectToDevice _connectCommand;
    SubscribeToCharacteristic _subscribeCommand;
    int _lastBpm = -1;
    bool _isRetrying;
    int _scanDiscoverCount;
    float _connectedAtTime = -1f;

    // 模拟模式相关
    float _simulatedBpm = 120f;

    void Log(string msg, bool isError = false)
    {
        if (!logToConsole) return;
        if (isError) Debug.LogWarning("[HeartRateBle] " + msg);
        else Debug.Log("[HeartRateBle] " + msg);
        
        // 将原有的报错信息也存入 log 供前台读取
        currentLog = msg; 
    }

    void Start()
    {
        _isRetrying = false;
        isConnected = false;

        // 如果启用模拟模式，直接设置连接状态，跳过蓝牙初始化
        if (useSimulatedHeartRate)
        {
            isConnected = true;
            _lastBpm = simulatedStartBpm;
            _simulatedBpm = simulatedStartBpm;
            currentLog = "模拟模式：生成测试心率数据";
            if (logToConsole)
                Debug.Log("[HeartRateBle] 模拟模式已启用，生成测试心率数据（起始值：" + simulatedStartBpm + " bpm）");
            return; // 跳过所有蓝牙连接逻辑
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        const string scanPerm = "android.permission.BLUETOOTH_SCAN";
        const string connectPerm = "android.permission.BLUETOOTH_CONNECT";
        if (!Permission.HasUserAuthorizedPermission(scanPerm) || !Permission.HasUserAuthorizedPermission(connectPerm))
        {
            currentLog = "Requesting Bluetooth permission...";
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => TryStartAfterPermission();
            callbacks.PermissionDenied += name => Log("Permission denied: " + name, true);
            callbacks.PermissionDeniedAndDontAskAgain += name => Log("Permission denied (don't ask again): " + name, true);
            if (!Permission.HasUserAuthorizedPermission(scanPerm))
                Permission.RequestUserPermission(scanPerm, callbacks);
            else
                Permission.RequestUserPermission(connectPerm, callbacks);
            return;
        }
#endif
        StartConnectFlow();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void TryStartAfterPermission()
    {
        const string scanPerm = "android.permission.BLUETOOTH_SCAN";
        const string connectPerm = "android.permission.BLUETOOTH_CONNECT";
        if (Permission.HasUserAuthorizedPermission(scanPerm) && Permission.HasUserAuthorizedPermission(connectPerm))
            StartConnectFlow();
        else if (!Permission.HasUserAuthorizedPermission(connectPerm))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => StartConnectFlow();
            Permission.RequestUserPermission(connectPerm, callbacks);
        }
    }
#endif

    void StartConnectFlow()
    {
        _connectedDeviceAddress = null;
        _scanDiscoverCount = 0;
        currentLog = "Scanning for devices...";
        Log("Scanning for BLE devices...");

        var discover = new DiscoverDevices(OnDeviceDiscovered, OnScanFinished, 12000);
        BleManager.Instance.QueueCommand(discover);
    }

    void OnDeviceDiscovered(string deviceAddress, string deviceName)
    {
        _scanDiscoverCount++;
        bool match = string.IsNullOrEmpty(deviceNameFilter) ||
            (!string.IsNullOrEmpty(deviceName) && deviceName.ToLowerInvariant().Contains(deviceNameFilter.ToLowerInvariant()));
        if (logToConsole)
            Debug.Log("[HeartRateBle] Discovered: " + (string.IsNullOrEmpty(deviceName) ? "(no name)" : deviceName) + " " + deviceAddress + (match ? " [match]" : ""));

        if (!string.IsNullOrEmpty(_connectedDeviceAddress))
            return;
        if (!match)
            return;

        _connectedDeviceAddress = deviceAddress;
    }

    void OnScanFinished()
    {
        if (string.IsNullOrEmpty(_connectedDeviceAddress))
        {
            currentLog = "No device found. Retrying...";
            Log("No device found. Scan saw " + _scanDiscoverCount + " device(s). Check filter or watch broadcast.", true);
            if (autoRetryOnDisconnect) StartCoroutine(RetryAfterDelay());
            return;
        }

        currentLog = "Connecting to device...";
        Log("Connecting to " + _connectedDeviceAddress);

        _connectCommand = new ConnectToDevice(
            _connectedDeviceAddress,
            OnConnected,
            OnDisconnected
        );
        BleManager.Instance.QueueCommand(_connectCommand);
    }

    void DoDirectConnect(string address)
    {
        _connectedDeviceAddress = address;
        currentLog = "Reconnecting...";
        Log("Direct connect to " + address + " (no scan).");

        _connectCommand = new ConnectToDevice(address, OnConnected, OnDisconnected);
        BleManager.Instance.QueueCommand(_connectCommand);
    }

    void OnConnected(string deviceAddress)
    {
        _lastConnectedAddress = deviceAddress;
        _connectedAtTime = Time.realtimeSinceStartup;
        currentLog = "Connected, preparing...";
        Log("Connected, will subscribe in " + subscribeDelaySeconds + "s...");

        if (subscribeDelaySeconds > 0f)
            Invoke(nameof(DoSubscribeDelayed), subscribeDelaySeconds);
        else
            DoSubscribeFor(_lastConnectedAddress);
    }

    void DoSubscribeDelayed()
    {
        DoSubscribeFor(_lastConnectedAddress);
    }

    void DoSubscribeFor(string deviceAddress)
    {
        if (string.IsNullOrEmpty(deviceAddress)) return;
        currentLog = "Subscribing to heart rate...";
        Log("Subscribing to heart rate...");

        _subscribeCommand = new SubscribeToCharacteristic(
            deviceAddress,
            HeartRateServiceUuid,
            HeartRateMeasurementUuid,
            OnHeartRateData,
            customGatt: true
        );
        BleManager.Instance.QueueCommand(_subscribeCommand);
    }

    void OnDisconnected(string deviceAddress)
    {
        CancelInvoke(nameof(DoSubscribeDelayed)); 
        
        // 核心：一旦断开，立刻把状态置为 false，这样前台 UI 就能立刻变红
        isConnected = false; 
        
        float connectedDuration = _connectedAtTime >= 0f ? Time.realtimeSinceStartup - _connectedAtTime : -1f;
        _connectedAtTime = -1f;
        
        currentLog = "Disconnected. Retrying...";
        
        if (connectedDuration >= 0f)
            Log("Disconnected from " + deviceAddress + " (was connected " + connectedDuration.ToString("F1") + "s).", true);
        else
            Log("Disconnected from " + deviceAddress, true);

        _subscribeCommand = null;
        _connectCommand = null;

        if (autoRetryOnDisconnect && !_isRetrying)
            StartCoroutine(RetryAfterDelay());
    }

    System.Collections.IEnumerator RetryAfterDelay()
    {
        _isRetrying = true;
        float t = retryWaitSeconds;
        while (t > 0f)
        {
            currentLog = string.Format("Disconnected. Retry in {0:F0}s", t);
            t -= Time.deltaTime;
            yield return null;
        }
        
        if (!string.IsNullOrEmpty(_lastConnectedAddress))
        {
            Log("Retry: direct connect to last device.");
            string addressToTry = _lastConnectedAddress;
            
            // 【核心修复】：把上次的地址“用掉”并立刻清空。
            // 这样如果这次直连瞬间失败，下次重试时就会走 else 分支，强制重新扫描。
            _lastConnectedAddress = null; 
            
            DoDirectConnect(addressToTry);
        }
        else
        {
            Log("Retrying scan...");
            StartConnectFlow();
        }
        
        _isRetrying = false;
    }
    

    public void Retry()
    {
        if (_isRetrying) return;
        _subscribeCommand?.Unsubscribe();
        _connectCommand?.Disconnect();
        _subscribeCommand = null;
        _connectCommand = null;
        if (!string.IsNullOrEmpty(_lastConnectedAddress))
            DoDirectConnect(_lastConnectedAddress);
        else
            StartConnectFlow();
    }

    public void ForceRescan()
    {
        if (_isRetrying) return;
        _lastConnectedAddress = null;
        _subscribeCommand?.Unsubscribe();
        _connectCommand?.Disconnect();
        _subscribeCommand = null;
        _connectCommand = null;
        StartConnectFlow();
    }

    void OnHeartRateData(byte[] value)
    {
        if (value == null || value.Length < 2) return;

        bool is16Bit = (value[0] & 0x01) != 0;
        int bpm;
        if (is16Bit && value.Length >= 3)
            bpm = value[1] | (value[2] << 8);
        else
            bpm = value[1];

        _lastBpm = Mathf.Clamp(bpm, 0, 255);
        
        // 核心：只有成功解包收到了心率数值，才算真正的 isConnected
        isConnected = true; 
    }

    void Update()
    {
        // 模拟模式：在 Update 中生成动态心率数据
        if (useSimulatedHeartRate)
        {
            // 模拟心率逐渐上升（从起始值向最大值靠近，模拟运动过程）
            if (_simulatedBpm < simulatedMaxBpm)
            {
                // 逐渐增加心率，模拟运动强度提升
                _simulatedBpm += simulatedIncreaseRate * Time.deltaTime;
                _simulatedBpm = Mathf.Clamp(_simulatedBpm, simulatedStartBpm, simulatedMaxBpm);
            }

            // 添加随机波动（模拟真实心率的小幅波动）
            int randomVariation = Random.Range(-simulatedRandomVariation, simulatedRandomVariation + 1);
            int finalBpm = Mathf.RoundToInt(_simulatedBpm) + randomVariation;
            _lastBpm = Mathf.Clamp(finalBpm, 60, 200); // 确保在合理范围内
        }
    }

    public int LastHeartRateBpm => _lastBpm;

    void OnDestroy()
    {
        _subscribeCommand?.Unsubscribe();
        _connectCommand?.Disconnect();
    }
#else
    void Start()
    {
        currentLog = "HR: Install BLE plugin";
    }

    public int LastHeartRateBpm => -1;
#endif
}