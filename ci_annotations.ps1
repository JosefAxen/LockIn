$r = Invoke-RestMethod 'https://api.github.com/repos/JosefAxen/LockIn/actions/runs/27366159058/jobs'
$jobId = $r.jobs[0].id
$ann = Invoke-RestMethod "https://api.github.com/repos/JosefAxen/LockIn/check-runs/$jobId/annotations" -Headers @{'Accept'='application/vnd.github+json'}
$ann | ForEach-Object { Write-Host "[$($_.annotation_level)] $($_.path):$($_.start_line) - $($_.message)" }
