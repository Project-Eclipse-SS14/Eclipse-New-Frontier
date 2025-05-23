using Content.Shared._Eclipse.Audio.BoomBox;
using Robust.Client.UserInterface;

namespace Content.Client._Eclipse.Audio.BoomBox;

public sealed class BoomBoxBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BoomBoxMenu? _menu;

    public BoomBoxBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BoomBoxMenu>();

        _menu.OnPlayPressed += args =>
        {
            if (args)
            {
                SendMessage(new BoomBoxPlayingMessage());
            }
            else
            {
                SendMessage(new BoomBoxPauseMessage());
            }
        };

        _menu.OnStopPressed += () =>
        {
            SendMessage(new BoomBoxStopMessage());
        };

        _menu.OnSongSelected += SelectSong;

        _menu.SetTime += SetTime;
        Reload();
    }

    /// <summary>
    /// Reloads the attached menu if it exists.
    /// </summary>
    public void Reload()
    {
        if (_menu == null || !EntMan.TryGetComponent(Owner, out BoomBoxComponent? boombox))
            return;

        _menu.SetAudioStream(boombox.AudioStream);

        if (boombox.SongBytes is not null)
        {
            var songData = EntMan.System<BoomBoxSystem>().GetAudioMetadata(boombox.SongBytes);
            var length = songData.Length;
            _menu.SetSelectedSong(songData.Title!, (float)length.TotalSeconds);
        }
        else
        {
            _menu.SetSelectedSong(string.Empty, 0f);
        }

        _menu.Entity = Owner;
    }

    public void SelectSong(byte[] bytes)
    {
        if (_menu is not null)
            EntMan.System<BoomBoxSystem>().SendSongData(_menu.Entity, bytes);
    }

    public void SetTime(float time)
    {
        var sentTime = time;

        // You may be wondering, what the fuck is this
        // Well we want to be able to predict the playback slider change, of which there are many ways to do it
        // We can't just use SendPredictedMessage because it will reset every tick and audio updates every frame
        // so it will go BRRRRT
        // Using ping gets us close enough that it SHOULD, MOST OF THE TIME, fall within the 0.1 second tolerance
        // that's still on engine so our playback position never gets corrected.
        if (EntMan.TryGetComponent(Owner, out BoomBoxComponent? boombox) &&
            EntMan.TryGetComponent(boombox.AudioStream, out BoomBoxAudioComponent? audioComp))
        {
            audioComp.PlaybackPosition = time;
        }

        SendMessage(new BoomBoxSetTimeMessage(sentTime));
    }
}

