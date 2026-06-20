using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

public sealed class TranscriptionOptions
{
    public const string SectionName = "Transcription";

    public bool Enabled { get; set; }

    public bool AutoTranscribeOnReady { get; set; }

    [Required]
    public string Provider { get; set; } = "local-faster-whisper";

    public string? DefaultLanguage { get; set; }

    public string[] OutputFormats { get; set; } = ["vtt", "srt"];

    [Required]
    public string WorkerBaseUrl { get; set; } = "http://127.0.0.1:8090";

    [Required]
    public string CallbackBaseUrl { get; set; } = "http://127.0.0.1:5000";

    public string? WorkerCallbackSecret { get; set; }

    public string WorkerCallbackAuthHeader { get; set; } = "X-StreamForge-Worker-Secret";

    public int JobTimeoutMinutes { get; set; } = 120;

    public LocalFasterWhisperOptions LocalFasterWhisper { get; set; } = new();
}

public sealed class LocalFasterWhisperOptions
{
    public string Model { get; set; } = "small";

    public string Device { get; set; } = "cpu";

    public string ComputeType { get; set; } = "int8";

    [Range(1, 20)]
    public int BeamSize { get; set; } = 5;

    public bool EnableVad { get; set; } = true;

    public bool EnableWordTimestamps { get; set; }
}
