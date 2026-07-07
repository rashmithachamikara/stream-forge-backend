using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

/// <summary>
/// Configures transcription orchestration, worker communication, and default output behavior.
/// </summary>
public sealed class TranscriptionOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Transcription";

    /// <summary>
    /// Gets or sets whether transcription features are enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets whether newly ready videos should be auto-submitted for transcription.
    /// </summary>
    public bool AutoTranscribeOnReady { get; set; }

    /// <summary>
    /// Gets or sets the active transcription provider identifier.
    /// </summary>
    [Required]
    public string Provider { get; set; } = "local-faster-whisper";

    /// <summary>
    /// Gets or sets the default transcription language when one is not explicitly requested.
    /// </summary>
    public string? DefaultLanguage { get; set; }

    /// <summary>
    /// Gets or sets the default transcription artifact formats to generate.
    /// </summary>
    public string[] OutputFormats { get; set; } = ["vtt", "srt"];

    /// <summary>
    /// Gets or sets the base URL for the transcription worker service.
    /// </summary>
    [Required]
    public string WorkerBaseUrl { get; set; } = "http://127.0.0.1:8090";

    /// <summary>
    /// Gets or sets the API callback base URL exposed to the worker.
    /// </summary>
    [Required]
    public string CallbackBaseUrl { get; set; } = "http://127.0.0.1:5000";

    /// <summary>
    /// Gets or sets the optional shared secret sent by the worker callback.
    /// </summary>
    public string? WorkerCallbackSecret { get; set; }

    /// <summary>
    /// Gets or sets the callback authentication header name expected from the worker.
    /// </summary>
    public string WorkerCallbackAuthHeader { get; set; } = "X-StreamForge-Worker-Secret";

    /// <summary>
    /// Gets or sets the worker request timeout in minutes.
    /// </summary>
    public int JobTimeoutMinutes { get; set; } = 120;

    /// <summary>
    /// Gets or sets provider-specific local Faster Whisper options.
    /// </summary>
    public LocalFasterWhisperOptions LocalFasterWhisper { get; set; } = new();
}

/// <summary>
/// Configures the local Faster Whisper transcription worker.
/// </summary>
public sealed class LocalFasterWhisperOptions
{
    /// <summary>
    /// Gets or sets the model name used by the worker.
    /// </summary>
    public string Model { get; set; } = "small";

    /// <summary>
    /// Gets or sets the execution device, such as CPU or CUDA.
    /// </summary>
    public string Device { get; set; } = "cpu";

    /// <summary>
    /// Gets or sets the worker compute type.
    /// </summary>
    public string ComputeType { get; set; } = "int8";

    /// <summary>
    /// Gets or sets the beam size used during transcription decoding.
    /// </summary>
    [Range(1, 20)]
    public int BeamSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether voice activity detection is enabled.
    /// </summary>
    public bool EnableVad { get; set; } = true;

    /// <summary>
    /// Gets or sets whether word-level timestamps are requested from the worker.
    /// </summary>
    public bool EnableWordTimestamps { get; set; }
}
