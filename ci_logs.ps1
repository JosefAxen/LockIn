$r = Invoke-RestMethod 'https://api.github.com/repos/JosefAxen/LockIn/actions/runs/27366159058/jobs'
$jobId = $r.jobs[0].id
$logUrl = "https://api.github.com/repos/JosefAxen/LockIn/actions/jobs/$jobId/logs"
$log = Invoke-RestMethod $logUrl -Headers @{'Accept'='application/vnd.github+json'}
# Filter to lines around the error
$lines = $log -split "`n"
$errorLines = $lines | Where-Object { $_ -match 'error|Error|FAILED|warning' } | Select-Object -First 40
$errorLines | ForEach-Object { Write-Host $_ }
