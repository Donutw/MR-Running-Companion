# GPU 温度监控脚本（需 NVIDIA 驱动自带 nvidia-smi）
# 用法：在项目根目录执行 .\GPU_TemperatureMonitor.ps1
# 温度超过阈值会弹窗提醒，可配合 Unity 里的 GPUThermalGuard 使用

Add-Type -AssemblyName System.Windows.Forms -ErrorAction SilentlyContinue

$WarningTemp = 82   # 警告温度（摄氏度）
$CriticalTemp = 88  # 危险温度（摄氏度）
$CheckIntervalSec = 10  # 检查间隔（秒）

function Get-GpuTemperature {
    $out = & nvidia-smi --query-gpu=temperature.gpu --format=csv,noheader,nounits 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }
    $temps = $out -replace ' ', '' | ForEach-Object { [int]$_ }
    return $temps
}

function Show-TempAlert {
    param([string]$Message, [string]$Title = "GPU 温度提醒")
    [System.Windows.Forms.MessageBox]::Show($Message, $Title, [System.Windows.Forms.MessageBoxButtons]::OK)
}

Write-Host "GPU 温度监控已启动（警告: ${WarningTemp}°C，危险: ${CriticalTemp}°C）。按 Ctrl+C 退出。"
Write-Host ""

$lastWarn = $null
$lastCritical = $null

while ($true) {
    $temps = Get-GpuTemperature
    if ($null -eq $temps) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 未检测到 nvidia-smi（仅支持 NVIDIA 显卡）"
    } else {
        $idx = 0
        foreach ($t in $temps) {
            $tag = if ($temps.Count -gt 1) { " GPU$idx" } else { "" }
            if ($t -ge $CriticalTemp) {
                Write-Host "[$(Get-Date -Format 'HH:mm:ss')]${tag} $t°C [危险]" -ForegroundColor Red
                if ($lastCritical -ne $t) {
                    Show-TempAlert "显卡温度过高：${t}°C。建议降低画质或限制帧率，并检查散热。" "GPU 温度过高"
                    $lastCritical = $t
                }
            } elseif ($t -ge $WarningTemp) {
                Write-Host "[$(Get-Date -Format 'HH:mm:ss')]${tag} $t°C [警告]" -ForegroundColor Yellow
                $lastCritical = $null
            } else {
                Write-Host "[$(Get-Date -Format 'HH:mm:ss')]${tag} $t°C" -ForegroundColor Green
                $lastWarn = $null
                $lastCritical = $null
            }
            $idx++
        }
    }
    Start-Sleep -Seconds $CheckIntervalSec
}
