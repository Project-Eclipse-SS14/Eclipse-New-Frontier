using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Shared._Corvax.CCVar;
using Content.Shared._Corvax.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using static Content.Server._Corvax.TTS.TTSManager;

namespace Content.Server._Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly List<string> _sampleText =
        new()
        {
            "Съешь же ещё этих мягких французских булок, да выпей чаю.",
            "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
            "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
            "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
            "Учёные, тут странная аномалия в баре! Она уже съела мима!",
            "Я надеюсь что инженеры внимательно следят за сингулярностью...",
            "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
            "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
            "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
            "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
            "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
            "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
        };

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled = false;
    private TimeSpan _ttsTimeout;
    private Queue<GenerateTTSData> _requestQueue = new();
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");

        _cfg.OnValueChanged(CorvaxCCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CorvaxCCVars.TTSTimeoutSeconds, v => _ttsTimeout = TimeSpan.FromSeconds(v), true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<GenerateTTSEvent>(OnGenerateTTS);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        RegisterRateLimits();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_requestQueue.Count > 0 && CheckQueueRateLimit() == RateLimitStatus.Allowed)
        {
            var data = DequeueTTSData();
            if (data == null)
            {
                ReturnQueueRateLimitTicket();
                return;
            }


            var ev = new GenerateTTSEvent(data);
            RaiseLocalEvent(ev);
        }
    }

    private GenerateTTSData? DequeueTTSData()
    {
        while (_requestQueue.TryDequeue(out var data))
        {
            var diff = _gameTiming.RealTime - data.TimeRequestCreated;
            if (diff > _ttsTimeout)
            {
                _sawmill.Error($"Timeout of request generation new audio for '{data.TextSanitized}' speech by '{data.Speaker}' speaker");
                continue;
            }

            return data;
        }

        return null;
    }

    private async void OnGenerateTTS(GenerateTTSEvent ev)
    {
        var result = await _ttsManager.ConvertTextToSpeech(ev.Data.Speaker, ev.Data.TextSanitized, ev.Data.Effects);

        if (result == null)
        {
            _sawmill.Error($"Failed to generate new audio for '{ev.Data.TextSanitized}' spoken by '{ev.Data.Speaker}' speaker");
            _requestQueue.Enqueue(ev.Data);
            return;
        }

        ev.Data.Tcs.SetResult(result);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, args.ObfuscatedMessage, protoVoice.Speaker);
            return;
        }

        HandleSay(uid, args.Message, protoVoice.Speaker);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker)
    {
        var soundData = await GenerateTTS(message, speaker);
        if (soundData is null) return;
        RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid)), Filter.Pvs(uid));
    }

    private async void HandleWhisper(EntityUid uid, string message, string obfMessage, string speaker)
    {
        var fullSoundData = await GenerateTTS(message, speaker, true);
        if (fullSoundData is null) return;

        var obfSoundData = await GenerateTTS(obfMessage, speaker, true);
        if (obfSoundData is null) return;

        var fullTtsEvent = new PlayTTSEvent(fullSoundData, GetNetEntity(uid), true);
        var obfTtsEvent = new PlayTTSEvent(obfSoundData, GetNetEntity(uid), true);

        // TODO: Check obstacles
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;
        foreach (var session in receptions)
        {
            if (!session.AttachedEntity.HasValue) continue;
            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();
            if (distance > ChatSystem.VoiceRange * ChatSystem.VoiceRange)
                continue;

            RaiseNetworkEvent(distance > ChatSystem.WhisperClearRange ? obfTtsEvent : fullTtsEvent, session);
        }
    }

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, bool isWhisper = false)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "") return null;
        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        TTSEffect? effects = null;

        //if (isWhisper)
        //    effects = TTSEffect.PitchShift;

        var data = new GenerateTTSData(_gameTiming.RealTime, textSanitized, speaker, effects);
        _requestQueue.Enqueue(data);

        var result = await data.Task;

        return result;
    }

    public sealed class TransformSpeakerVoiceEvent : EntityEventArgs
    {
        public EntityUid Sender;
        public string VoiceId;

        public TransformSpeakerVoiceEvent(EntityUid sender, string voiceId)
        {
            Sender = sender;
            VoiceId = voiceId;
        }
    }

    private sealed class GenerateTTSData
    {
        public Task<byte[]?> Task => Tcs.Task;
        public readonly TaskCompletionSource<byte[]?> Tcs = new TaskCompletionSource<byte[]?>();

        public string TextSanitized;
        public string Speaker;
        public TTSEffect? Effects;
        public TimeSpan TimeRequestCreated;

        public GenerateTTSData(TimeSpan timeRequestCreated, string textSanitized, string speaker, TTSEffect? effects)
        {
            TimeRequestCreated = timeRequestCreated;
            TextSanitized = textSanitized;
            Speaker = speaker;
            Effects = effects;
        }
    }

    private sealed class GenerateTTSEvent
    {
        public readonly GenerateTTSData Data;

        public GenerateTTSEvent(GenerateTTSData data)
        {
            Data = data;
        }
    }
}
