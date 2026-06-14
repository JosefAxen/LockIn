$r = Invoke-RestMethod 'https://api.github.com/repos/JosefAxen/LockIn/actions/runs/27366159058/jobs'
$j = $r.jobs[0]
Write-Host "Job: $($j.name) -> $($j.conclusion)"
$j.steps | ForEach-Object { Write-Host "  $($_.number) $($_.name) -> $($_.conclusion)" }
