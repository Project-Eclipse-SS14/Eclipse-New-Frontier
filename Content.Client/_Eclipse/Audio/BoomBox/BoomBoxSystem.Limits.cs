using Content.Shared._Eclipse.CCVar;

namespace Content.Client._Eclipse.Audio.BoomBox;

public sealed partial class BoomBoxSystem
{
    /*
     * Handles limiting concurrent sounds for boombox audio.
     */

    private int _playingCount = 0;

    private int _maxConcurrent;

    private void InitializeLimit()
    {
        _cfg.OnValueChanged(EclipseCCVars.BoomBoxAudioLimitConcurrent, (obj) => _maxConcurrent = obj, true);
    }

    private bool TryAudioLimit()
    {
        if (_playingCount >= _maxConcurrent)
            return false;

        _playingCount++;
        return true;
    }

    private void RemoveAudioLimit()
    {
        _playingCount = Math.Max(_playingCount - 1, 0);
    }
}
