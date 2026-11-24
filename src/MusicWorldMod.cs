using System;
using System.IO;
using Eleon.Modding;

namespace MusicWorld
{
    public sealed class MusicWorldMod : IMod
    {
        IModApi api;
        Player player;
        bool inGame, disabled;

        public void Init(IModApi modApi)
        {
            api = modApi;
            api.Application.GameEntered += Enter;
            api.Application.Update += Update;
            api.Log("MusicWorld 0.1.0 loaded.");
        }

        void Enter(bool entered)
        {
            Stop();
            inGame = entered;
            disabled = false;
        }

        void Update()
        {
            if (!inGame || disabled) return;
            try
            {
                if (api.Application.Mode != ApplicationMode.SinglePlayer ||
                    api.Application.State != GameState.Running || api.Application.LocalPlayer == null)
                {
                    Stop();
                    return;
                }
                if (player == null)
                {
                    string root = Path.Combine(api.Application.GetPathFor(AppFolder.Content), "Mods", "MusicWorld");
                    player = new Player(root, api.Log, api.LogWarning);
                    api.Log("MusicWorld playlists active.");
                }
                player.Tick();
            }
            catch (Exception e)
            {
                disabled = true;
                Stop();
                api.LogError("MusicWorld paused for this session: " + e);
            }
        }

        void Stop()
        {
            if (player != null) { player.Dispose(); player = null; }
        }

        public void Shutdown()
        {
            api.Application.Update -= Update;
            api.Application.GameEntered -= Enter;
            Stop();
        }
    }
}
