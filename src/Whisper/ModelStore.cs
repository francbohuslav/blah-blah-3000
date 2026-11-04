using System.IO;
using System.Net.Http;

namespace BlahBlah3000.Whisper;

/// <summary>The Whisper model in %LOCALAPPDATA%\Bogan\BlahBlah3000\models; a missing model is downloaded from HuggingFace, but only when the user asks for it.</summary>
internal static class ModelStore
{
    public const string FileName = "ggml-large-v3-turbo-q8_0.bin";
    public const long ExpectedSize = 874_188_075;
    public const string DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/" + FileName;

    public static string Directory_ { get; } = Path.Combine(AppPaths.Data, "models");

    public static string ModelPath => Path.Combine(Directory_, FileName);

    public static bool Exists() => File.Exists(ModelPath) && new FileInfo(ModelPath).Length == ExpectedSize;

    /// <summary>Downloads the model to a .part file and renames it after checking the size; progress is reported from 0 to 1. Cancelling deletes the .part file.</summary>
    public static async Task DownloadAsync(IProgress<double> progress, CancellationToken ct)
    {
        System.IO.Directory.CreateDirectory(Directory_);
        var part = ModelPath + ".part";
        try
        {
            using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            using var response = await http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? ExpectedSize;
            await using (var source = await response.Content.ReadAsStreamAsync(ct))
            await using (var target = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true))
            {
                var buffer = new byte[1 << 20];
                long done = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, ct)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), ct);
                    done += read;
                    progress.Report((double)done / total);
                }
            }
            if (new FileInfo(part).Length != ExpectedSize)
            {
                throw new InvalidOperationException(Loc.T("LT.BadSize"));
            }
            File.Move(part, ModelPath, overwrite: true);
            Log.Write($"Model downloaded: {ModelPath}");
        }
        catch
        {
            try { File.Delete(part); } catch (IOException) { }
            throw;
        }
    }
}
