$ios = Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/microsoft.net.sdk.ios.manifest-9.0.100/index.json'
Write-Host "=== iOS 18.x versions ==="
$ios.versions | Where-Object { $_ -like '18.*' }

$mac = Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/microsoft.net.sdk.maccatalyst.manifest-9.0.100/index.json'
Write-Host "=== MacCatalyst 18.x versions ==="
$mac.versions | Where-Object { $_ -like '18.*' }

$maui = Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/microsoft.net.sdk.maui.manifest-9.0.100/index.json'
Write-Host "=== MAUI versions (last 10) ==="
$maui.versions | Select-Object -Last 10
