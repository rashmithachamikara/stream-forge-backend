$body = @{
  correlationId = "test-001"
  videoId = "video-001"
  sourceReference = @{
    type = "local_path"
    value = "<absolute-file-path>"
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