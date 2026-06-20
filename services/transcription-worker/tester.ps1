$body = @{
  correlationId = "test-001"
  videoId = "video-001"
  sourceReference = @{
    type = "local_path"
    value = "D:\Play\The Boys\The.Boys.2019.S03E01.1080p.10bit.WEBRip.6CH.x265.HEVC-PS.mkv"
  }
  language = "en"
  outputFormats = @("vtt", "srt")
  callback = @{
    url = "http://127.0.0.1:9999/fake-callback"
  }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod `
  -Method Post `
  -Uri "http://127.0.0.1:8090/jobs/transcriptions" `
  -ContentType "application/json" `
  -Body $body