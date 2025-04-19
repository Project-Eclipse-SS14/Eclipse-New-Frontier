using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Content.Shared._Corvax.CCVar;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server._Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] { "type" },
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache.");

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private readonly Dictionary<string, byte[]> _cache = new();
    private readonly List<string> _cacheKeysSeq = new();
    private int _maxCachedCount = 200;
    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CorvaxCCVars.TTSMaxCache, val =>
        {
            _maxCachedCount = val;
            ResetCache();
        }, true);
        _cfg.OnValueChanged(CorvaxCCVars.TTSApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CorvaxCCVars.TTSApiToken, v => _apiToken = v, true);
    }

    /// <summary>
    /// Generates audio with passed text by API
    /// </summary>
    /// <param name="speaker">Identifier of speaker</param>
    /// <param name="text">SSML formatted text</param>
    /// <returns>OGG audio bytes or null if failed</returns>
    public async Task<byte[]?> ConvertTextToSpeech(string speaker, string text, TTSEffect? effect)
    {
        WantedCount.Inc();
        var cacheKey = GenerateCacheKey(speaker, text);
        if (_cache.TryGetValue(cacheKey, out var data))
        {
            ReusedCount.Inc();
            _sawmill.Verbose($"Use cached sound for '{text}' speech by '{speaker}' speaker");
            return data;
        }

        _sawmill.Verbose($"Generate new audio for '{text}' speech by '{speaker}' speaker");

        var reqTime = DateTime.UtcNow;
        try
        {
            var timeout = _cfg.GetCVar(CorvaxCCVars.TTSApiTimeout);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

            var uriBuilder = new UriBuilder(_apiUrl);

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["text"] = text;
            query["speaker"] = speaker;
            query["ext"] = "ogg";
            if (effect.HasValue)
            {
                switch (effect.Value)
                {
                    case TTSEffect.Radio:
                        query["effect"] = "radio";
                        break;
                    case TTSEffect.Reverse:
                        query["effect"] = "reverse";
                        break;
                    case TTSEffect.Echo:
                        query["effect"] = "echo";
                        break;
                    case TTSEffect.Robotic:
                        query["effect"] = "robotic";
                        break;
                    case TTSEffect.Ghost:
                        query["effect"] = "ghost";
                        break;
                    case TTSEffect.Announce:
                        query["effect"] = "announce";
                        break;
                }
            }
            uriBuilder.Query = query.ToString();

            HttpRequestMessage request = new(HttpMethod.Get, uriBuilder.ToString())
            {
                Version = _httpClient.DefaultRequestVersion,
                VersionPolicy = _httpClient.DefaultVersionPolicy,
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

            var response = await _httpClient.SendAsync(request, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _sawmill.Warning("TTS request was rate limited");
                    return null;
                }

                _sawmill.Error($"TTS request returned bad status code: {response.StatusCode}");
                return null;
            }

            var soundData = await response.Content.ReadAsByteArrayAsync(cancellationToken: cts.Token);

            _cache.Add(cacheKey, soundData);
            _cacheKeysSeq.Add(cacheKey);
            if (_cache.Count > _maxCachedCount)
            {
                var firstKey = _cacheKeysSeq.First();
                _cache.Remove(firstKey);
                _cacheKeysSeq.Remove(firstKey);
            }

            _sawmill.Debug($"Generated new audio for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");
            RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            return soundData;
        }
        catch (TaskCanceledException)
        {
            RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Timeout of request generation new audio for '{text}' speech by '{speaker}' speaker");
            return null;
        }
        catch (Exception e)
        {
            RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
            return null;
        }
    }

    public void ResetCache()
    {
        _cache.Clear();
        _cacheKeysSeq.Clear();
    }

    private string GenerateCacheKey(string speaker, string text)
    {
        var key = $"{speaker}/{text}";
        byte[] keyData = Encoding.UTF8.GetBytes(key);
        var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(keyData);
        return Convert.ToHexString(bytes);
    }

    public enum TTSEffect
    {
        Radio,
        Reverse,
        Echo,
        Robotic,
        Ghost,
        Announce
    }
}
