param(
    [string]$BaseUrl = $(if ($env:BASE_URL) { $env:BASE_URL } else { 'http://localhost:5080' }),
    [string]$Username = 'admin',
    [string]$Password = 'P@ssw0rd!'
)

$body = @{ username = $Username; password = $Password } | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $body -ContentType 'application/json' -ErrorAction Stop

if ($null -eq $response.token) {
    Write-Error "Login failed: $($response | ConvertTo-Json -Depth 3)"
    exit 1
}

$response.token | Out-File -FilePath .token -Encoding ascii
Write-Host "Token saved to .token"
if ($response.expiresAtUtc) { Write-Host "expiresAtUtc: $($response.expiresAtUtc)" }
