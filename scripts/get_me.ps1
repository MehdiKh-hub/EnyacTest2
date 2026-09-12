param(
    [string]$BaseUrl = $(if ($env:BASE_URL) { $env:BASE_URL } else { 'http://localhost:5080' })
)

$token = $env:TOKEN
if (-not $token -and (Test-Path .token)) { $token = Get-Content .token -Raw }
if (-not $token) { Write-Error 'No token found. Run .\login.ps1 or set $env:TOKEN'; exit 1 }

$response = Invoke-RestMethod -Uri "$BaseUrl/api/users/me" -Headers @{ Authorization = "Bearer $token" } -Method Get -ErrorAction Stop

$response | ConvertTo-Json -Depth 5
