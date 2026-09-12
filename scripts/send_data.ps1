param(
    [string]$BaseUrl = $(if ($env:BASE_URL) { $env:BASE_URL } else { 'http://localhost:5080' }),
    [string]$Title = 'تست',
    [string]$Payload = 'این یک محموله نمونه است'
)

$token = $env:TOKEN
if (-not $token -and (Test-Path .token)) { $token = Get-Content .token -Raw }
if (-not $token) { Write-Error 'No token found. Run .\login.ps1 or set $env:TOKEN'; exit 1 }

$body = @{ title = $Title; payload = $Payload } | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$BaseUrl/api/data/send" -Headers @{ Authorization = "Bearer $token" } -Method Post -Body $body -ContentType 'application/json' -ErrorAction Stop

$response | ConvertTo-Json -Depth 5
