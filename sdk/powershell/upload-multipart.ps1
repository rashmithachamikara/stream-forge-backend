param(
  [string]$BaseUrl = "http://localhost:5186",
  [string]$Email = "<youremail@email.com>",
  [string]$Password = "<yourpassword>",
  [string]$FilePath = "D:\Play\Modern family\Modern.Family.S01E01.720p.BluRay.x264.150MB-Pahe.in.mkv",
  [string]$Title = "Multipart Test Video",
  [string]$Description = "Testing multipart upload",
  [Nullable[Guid]]$CategoryId = $null,
  [string]$ContentType = "video/x-matroska",
  [int]$ChunkSizeBytes = 5242880
)

$ErrorActionPreference = "Stop"

function Write-Step($message) {
  Write-Host $message
}

function Read-ErrorBody {
  param([Parameter(Mandatory = $true)]$Exception)

  try {
    $response = $Exception.Response
    if ($null -eq $response) {
      return $null
    }

    $stream = $response.GetResponseStream()
    if ($null -eq $stream) {
      return $null
    }

    $reader = New-Object System.IO.StreamReader($stream)
    try {
      return $reader.ReadToEnd()
    } finally {
      $reader.Dispose()
    }
  } catch {
    return $null
  }
}

function Get-Token {
  param(
    [Parameter(Mandatory = $true)][string]$BaseUrl,
    [Parameter(Mandatory = $true)][string]$Email,
    [Parameter(Mandatory = $true)][string]$Password
  )

  $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body (@{ email = $Email; password = $Password } | ConvertTo-Json) `
    -ErrorAction Stop

  if ($loginResponse.accessToken) {
    return $loginResponse.accessToken
  }

  if ($loginResponse.token) {
    return $loginResponse.token
  }

  throw "Login response did not contain an access token."
}

function Get-OrCreateTestFile {
  param([string]$FilePath)

  if ([string]::IsNullOrWhiteSpace($FilePath)) {
    $FilePath = Join-Path $PSScriptRoot "multipart-test-video.mp4"
  }

  if (-not (Test-Path $FilePath)) {
    $bytes = New-Object byte[] 1048576
    [System.Random]::new().NextBytes($bytes)
    [System.IO.File]::WriteAllBytes($FilePath, $bytes)
  }

  return (Resolve-Path $FilePath).Path
}

function Get-MD5Hex {
  param([Parameter(Mandatory = $true)][byte[]]$Bytes)

  $md5 = [System.Security.Cryptography.MD5]::Create()
  try {
    return ([System.BitConverter]::ToString($md5.ComputeHash($Bytes))).Replace('-', '').ToLowerInvariant()
  } finally {
    $md5.Dispose()
  }
}

function Split-IntoChunks {
  param(
    [Parameter(Mandatory = $true)][byte[]]$Bytes,
    [Parameter(Mandatory = $true)][int]$ChunkSizeBytes
  )

  $chunks = New-Object System.Collections.Generic.List[object]
  $offset = 0
  $partNumber = 1

  while ($offset -lt $Bytes.Length) {
    $length = [Math]::Min($ChunkSizeBytes, $Bytes.Length - $offset)
    $chunk = New-Object byte[] $length
    [Array]::Copy($Bytes, $offset, $chunk, 0, $length)
    $chunks.Add([pscustomobject]@{ PartNumber = $partNumber; Bytes = $chunk })
    $offset += $length
    $partNumber++
  }

  return $chunks
}

try {
  Write-Step "1. Logging in..."
  $token = Get-Token -BaseUrl $BaseUrl -Email $Email -Password $Password
  Write-Step "OK - Token acquired"

  Write-Step ""
  Write-Step "2. Creating upload session..."
  $sessionBody = @{
    title = $Title
    description = $Description
    totalSize = 1048576
    contentType = $ContentType
    categoryId = $CategoryId
  } | ConvertTo-Json

  $sessionResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/uploads/sessions" `
    -Method POST `
    -ContentType "application/json" `
    -Headers @{ Authorization = "Bearer $token" } `
    -Body $sessionBody `
    -ErrorAction Stop

  $sessionId = $sessionResponse.sessionId
  Write-Step "OK - Session ID: $sessionId"

  Write-Step ""
  Write-Step "3. Getting upload target..."
  Write-Step "OK - Session target is backend multipart upload"

  Write-Step ""
  Write-Step "4. Preparing test file..."
  $resolvedFilePath = Get-OrCreateTestFile -FilePath $FilePath
  Write-Step "OK - File: $resolvedFilePath"

  Write-Step ""
  Write-Step "5. Uploading multipart chunks..."
  Add-Type -AssemblyName System.Net.Http

  $fileBytes = [System.IO.File]::ReadAllBytes($resolvedFilePath)
  $chunks = Split-IntoChunks -Bytes $fileBytes -ChunkSizeBytes $ChunkSizeBytes
  Write-Step "OK - Split into $($chunks.Count) chunk(s) of up to $ChunkSizeBytes bytes"

  $fileName = [System.IO.Path]::GetFileName($resolvedFilePath)
  $chunkCount = 0
  foreach ($chunk in $chunks) {
    $partNumber = [int]$chunk.PartNumber
    $checksum = Get-MD5Hex -Bytes $chunk.Bytes
    $partSize = $chunk.Bytes.Length

    $targetUrl = "$BaseUrl/api/v1/uploads/sessions/$sessionId/target?partNumber=$partNumber&partSize=$partSize"
    $targetResponse = Invoke-RestMethod -Uri $targetUrl `
      -Method GET `
      -Headers @{ Authorization = "Bearer $token" } `
      -ErrorAction Stop

    $chunkStream = New-Object System.IO.MemoryStream(,$chunk.Bytes)
    try {
      $streamContent = New-Object System.Net.Http.StreamContent($chunkStream)
      $streamContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("application/octet-stream")

      $multipart = New-Object System.Net.Http.MultipartFormDataContent
      $multipart.Add($streamContent, "file", $fileName)
      $multipart.Add((New-Object System.Net.Http.StringContent($checksum)), "checksum")

      $httpClient = New-Object System.Net.Http.HttpClient
      try {
        $httpClient.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
        $uploadUri = New-Object System.Uri("$BaseUrl/api/v1/uploads/sessions/$sessionId/parts/$partNumber")
        $uploadResponseMessage = $httpClient.PostAsync($uploadUri, $multipart).GetAwaiter().GetResult()
        $uploadBody = $uploadResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        if (-not $uploadResponseMessage.IsSuccessStatusCode) {
          throw "Chunk $partNumber failed with status $($uploadResponseMessage.StatusCode): $uploadBody"
        }

        $chunkCount++
        Write-Step "OK - Uploaded chunk $partNumber / $($chunks.Count)"
      } finally {
        $httpClient.Dispose()
        $multipart.Dispose()
      }
    } finally {
      $chunkStream.Dispose()
    }
  }

  Write-Step ""
  Write-Step "6. Completing upload..."
  $completeBody = @{ sessionId = $sessionId; fileName = $fileName } | ConvertTo-Json
  $completeUrl = "$BaseUrl/api/v1/uploads/sessions/$sessionId/complete"
  $completeResponse = Invoke-RestMethod -Uri $completeUrl `
    -Method POST `
    -ContentType "application/json" `
    -Headers @{ Authorization = "Bearer $token" } `
    -Body $completeBody `
    -ErrorAction Stop

  Write-Step "OK - Upload complete!"
  Write-Step "  Video ID: $($completeResponse.videoId)"
  Write-Step "  Status: $($completeResponse.status)"

  Write-Host ""
  Write-Host "========== SUCCESS =========="
  Write-Host "Video uploaded successfully"
  Write-Host "Stored under: $BaseUrl/uploads/$sessionId/final/"
} catch {
  Write-Step "FAILED: $($_.Exception.Message)"
  $body = Read-ErrorBody -Exception $_.Exception
  if ($body) {
    Write-Step "Response Body: $body"
  }
  exit 1
}
