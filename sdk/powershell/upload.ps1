$baseUrl = "http://localhost:5186"
$email = "<youremail@email.com>"
$password = "<yourpassword>"

Write-Host "1. Logging in..."
try {
  $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body (@{ email = $email; password = $password } | ConvertTo-Json) `
    -ErrorAction Stop
  
  Write-Host "Response: $($loginResponse | ConvertTo-Json)"
  
  if ($loginResponse.token) {
    $token = $loginResponse.token
    Write-Host "OK - Token: $($token.Substring(0, 20))..."
  } elseif ($loginResponse.accessToken) {
    $token = $loginResponse.accessToken
    Write-Host "OK - Token: $($token.Substring(0, 20))..."
  } else {
    Write-Host "FAILED: No token in response"
    exit 1
  }
} catch {
  Write-Host "FAILED: $($_.Exception.Message)"
  Write-Host "Status: $($_.Exception.Response.StatusCode)"
  exit 1
}

Write-Host ""
Write-Host "2. Creating upload session..."
try {
  $sessionBody = @{
    title = "Test Video"
    description = "Testing"
    totalSize = 1048576
    contentType = "video/mp4"
    categoryId = $null
  } | ConvertTo-Json

  $sessionResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/uploads/sessions" `
    -Method POST `
    -ContentType "application/json" `
    -Headers @{ Authorization = "Bearer $token" } `
    -Body $sessionBody `
    -ErrorAction Stop
  $sessionId = $sessionResponse.sessionId
  Write-Host "OK - Session ID: $sessionId"
} catch {
  Write-Host "FAILED: $($_.Exception.Message)"
  if ($_.ErrorDetails.Message) {
    Write-Host "Error Details: $($_.ErrorDetails.Message)"
  }
  exit 1
}

Write-Host ""
Write-Host "3. Getting upload target..."
try {
  $params = "?partNumber=1&partSize=1048576"
  $targetUrl = "$baseUrl/api/v1/uploads/sessions/$sessionId/target$params"
  $targetResponse = Invoke-RestMethod -Uri $targetUrl `
    -Method GET `
    -Headers @{ Authorization = "Bearer $token" } `
    -ErrorAction Stop
  Write-Host "OK - Target URL: $($targetResponse.url)"
} catch {
  Write-Host "FAILED: $($_.Exception.Message)"
  exit 1
}

Write-Host ""
Write-Host "4. Creating test file..."
$testFile = Join-Path $PSScriptRoot "test-video.mp4"
$bytes = New-Object byte[] 1048576
[System.Random]::new().NextBytes($bytes)
[System.IO.File]::WriteAllBytes($testFile, $bytes)
Write-Host "OK - Created: $testFile"

Write-Host ""
Write-Host "5. Uploading part..."
try {
  $boundary = [System.Guid]::NewGuid().ToString()
  $LF = "`r`n"
  $bodyLines = @(
    "--$boundary",
    'Content-Disposition: form-data; name="file"; filename="test-video.mp4"',
    'Content-Type: application/octet-stream',
    '',
    [System.Text.Encoding]::GetEncoding("ISO-8859-1").GetString($bytes),
    "--$boundary",
    'Content-Disposition: form-data; name="checksum"',
    '',
    'test-checksum-123',
    "--$boundary--"
  )
  $body = [System.Text.Encoding]::UTF8.GetBytes(($bodyLines -join $LF))
  
  $uploadUrl = "$baseUrl/api/v1/uploads/sessions/$sessionId/parts/1"
  $uploadResponse = Invoke-WebRequest -Uri $uploadUrl -UseBasicParsing `
    -Method POST `
    -Headers @{ 
      Authorization = "Bearer $token"
      "Content-Type" = "multipart/form-data; boundary=$boundary"
    } `
    -Body $body `
    -ErrorAction Stop
  $uploadData = $uploadResponse.Content | ConvertFrom-Json
  Write-Host "OK - Part uploaded"
} catch {
  Write-Host "FAILED: $($_.Exception.Message)"
  exit 1
}

Write-Host ""
Write-Host "6. Completing upload..."
try {
  $completeBody = @{ sessionId = $sessionId; fileName = "test-video.mp4"; checksum = "test-checksum-123" } | ConvertTo-Json
  $completeUrl = "$baseUrl/api/v1/uploads/sessions/$sessionId/complete"
  $completeResponse = Invoke-RestMethod -Uri $completeUrl `
    -Method POST `
    -ContentType "application/json" `
    -Headers @{ Authorization = "Bearer $token" } `
    -Body $completeBody `
    -ErrorAction Stop
  $videoId = $completeResponse.videoId
  Write-Host "OK - Upload complete!"
  Write-Host "  Video ID: $videoId"
  Write-Host "  Status: $($completeResponse.status)"
} catch {
  Write-Host "FAILED: $($_.Exception.Message)"
  exit 1
}

Write-Host ""
Write-Host "========== SUCCESS =========="
Write-Host "Video uploaded successfully"
Write-Host "Files: ./uploads/$sessionId/final/"
