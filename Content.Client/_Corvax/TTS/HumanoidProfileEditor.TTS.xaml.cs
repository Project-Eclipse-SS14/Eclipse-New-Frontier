namespace Content.Client.Lobby.UI
{
    public sealed partial class HumanoidProfileEditor
    {
        private void SetVoice(string newVoice)
        {
            Profile = Profile?.WithVoice(newVoice);
            IsDirty = true;
        }
    }
}