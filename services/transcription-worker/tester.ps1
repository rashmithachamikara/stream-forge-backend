$body = @{
  correlationId = "test-001"
  videoId = "video-001"
  sourceReference = @{
    type = "local_path"
    value = "C:\Files\Shared\Software Projects\Stream Forge\stream-forge-backend\src\StreamForge.Api\uploads\videos\8ccfe49a383443698d41df27aa133959\original\Red Dead Redemption 2 2024.02.09 - 23.45.52.01.mp4"
  }
  language = "en"
  outputFormats = @("vtt", "srt")
  callback = @{
    url = "http://127.0.0.1:9999/fake-callback"
  }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod `
  -Method Post `
  -Uri "http://127.0.0.1:8091/jobs/transcriptions" `
  -ContentType "application/json" `
  -Body $body