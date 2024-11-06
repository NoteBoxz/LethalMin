# LaunchTwoInstances.ps1

Add-Type -AssemblyName System.Windows.Forms

# Configuration
$exePath = 'C:\Program Files (x86)\Steam\steamapps\common\Lethal Company\Lethal Company.exe'
$windowSize = 0.50

# Calculate screen resolution and window size
$ScreenWidth = [System.Windows.Forms.Screen]::AllScreens[0].Bounds.Width
$ScreenHeight = [System.Windows.Forms.Screen]::AllScreens[0].Bounds.Height
$WindowWidth = [Math]::Floor($ScreenWidth * $windowSize)
$WindowHeight = [Math]::Floor($ScreenHeight * $windowSize)

# Prepare arguments for game launch
$arguments = @(
    '-monitor', '0',
    '--screen-fullscreen', '0',
    '-screen-height', $WindowHeight,
    '-screen-width', $WindowWidth
)

# Uncomment the following lines if the doorstop argument is needed
# $doorstopPath = "C:\Program Files (x86)\Steam\steamapps\common\Lethal Company\BepInEx\core\BepInEx.Preloader.dll"
# $arguments += @('--doorstop-enable', 'true', "--doorstop-target", "`"$doorstopPath`"")

# Launch two instances
for ($i = 1; $i -le 2; $i++) {
    Write-Host "Starting game instance: $i"
    Start-Process $exePath -ArgumentList $arguments
    if ($i -eq 1) {
        Start-Sleep -Seconds 15  # 5-second pause between launches
    }
}