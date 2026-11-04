using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BlahBlah3000.Whisper;

namespace BlahBlah3000.Api;

/// <summary>
/// REST API for other applications:
/// GET /health and POST /transcribe (raw PCM s16le 16 kHz mono, optional ?prompt= and ?language=).
/// </summary>
internal sealed class ApiServer
{
    /// <summary>Limit of 2 minutes of audio (32,000 bytes per second).</summary>
    private const int MaxBody = 32_000 * 120;

    private WebApplication? _app;

    public int Port { get; private set; }
    public bool ListenAll { get; private set; }
    public bool Running => _app != null;

    /// <summary>Starts the server; on failure (e.g. the port is taken) returns a message, otherwise null.</summary>
    public async Task<string?> StartAsync(int port, bool listenAll, LocalTranscriber transcriber, Func<string> defaultLanguage)
    {
        await StopAsync();
        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls($"http://{(listenAll ? "0.0.0.0" : "127.0.0.1")}:{port}");
            var app = builder.Build();

            app.MapGet("/health", () =>
            {
                var ok = transcriber.State is TranscriberState.Ready or TranscriberState.Idle;
                return Results.Json(new { ok, model = ModelStore.FileName, state = transcriber.State.ToString() },
                    statusCode: ok ? 200 : 503);
            });

            app.MapPost("/transcribe", async (HttpRequest request, CancellationToken ct) =>
            {
                if (request.ContentLength > MaxBody) return Results.Json(new { error = "The recording is longer than 2 minutes" }, statusCode: 413);
                using var buffer = new MemoryStream();
                var chunk = new byte[64 * 1024];
                int read;
                while ((read = await request.Body.ReadAsync(chunk, ct)) > 0)
                {
                    if (buffer.Length + read > MaxBody) return Results.Json(new { error = "The recording is longer than 2 minutes" }, statusCode: 413);
                    buffer.Write(chunk, 0, read);
                }
                var pcm = buffer.ToArray();
                if (pcm.Length == 0 || pcm.Length % 2 != 0)
                {
                    return Results.Json(new { error = "The body must be non-empty PCM s16le 16 kHz mono (an even number of bytes)" }, statusCode: 400);
                }
                var prompt = request.Query["prompt"].ToString();
                var language = request.Query["language"].ToString();
                if (language.Length == 0) language = defaultLanguage();
                var started = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var text = await transcriber.TranscribeAsync(pcm, prompt, language, ct);
                    Log.Write($"API transcription ({started.ElapsedMilliseconds} ms): \"{text}\"");
                    return Results.Json(new { text, ms = started.ElapsedMilliseconds });
                }
                catch (InvalidOperationException ex) when (transcriber.State is TranscriberState.ModelMissing or TranscriberState.Downloading)
                {
                    return Results.Json(new { error = ex.Message }, statusCode: 503);
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(499);
                }
                catch (Exception ex)
                {
                    Log.Write($"API transcription failed: {ex}");
                    return Results.Json(new { error = ex.Message }, statusCode: 500);
                }
            });

            await app.StartAsync();
            _app = app;
            Port = port;
            ListenAll = listenAll;
            Log.Write($"API listening on {(listenAll ? "0.0.0.0" : "127.0.0.1")}:{port}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot start the API: {ex.Message}");
            return ex.InnerException?.Message ?? ex.Message;
        }
    }

    public async Task StopAsync()
    {
        var app = _app;
        _app = null;
        if (app == null) return;
        await app.StopAsync();
        await app.DisposeAsync();
        Log.Write("API stopped");
    }
}
