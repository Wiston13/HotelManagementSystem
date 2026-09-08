param(
    [Parameter(Mandatory = $true)]
    [string]$WebhookUrl,

    [Parameter(Mandatory = $true)]
    [string]$WebhookSecret,

    [Parameter(Mandatory = $true)]
    [string]$RecipientEmail
)

$headers = @{
    "X-Hotel-Webhook-Key" = $WebhookSecret
}

$bodyObject = [ordered]@{
    bookingNumber = "TEST-HANDOFF-001"
    bookerName = "王小明"
    email = $RecipientEmail
    roomTypeName = "豪華雙人房"
    checkInDate = "2026-09-10"
    checkOutDate = "2026-09-12"
    totalAmount = 6000
    branchName = "台北分館"
    branchAddress = "台北市中正區測試路100號"
    branchPhone = "02-1234-5678"
}

$body = $bodyObject | ConvertTo-Json

$request = @{
    Uri = $WebhookUrl
    Method = "Post"
    Headers = $headers
    ContentType = "application/json; charset=utf-8"
    Body = [System.Text.Encoding]::UTF8.GetBytes($body)
    TimeoutSec = 15
}
try
{
    $response = Invoke-RestMethod @request

    Write-Host "已收到 n8n 回應：" -ForegroundColor Green
    $response | Format-List
}
catch
{
    Write-Error "呼叫 n8n Webhook 失敗：$($_.Exception.Message)"
    exit 1
}