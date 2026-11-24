using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MusicWorld
{
    [Serializable]
    internal sealed class Settings
    {
        public float volume = 0.4f;
        public float fadeSeconds = 2f;
        public int exploreGapMin = 25, exploreGapMax = 75;
        public float afterCombatQuiet = 12f, combatHold = 3f;
        public bool debugLog = false;

        internal static Settings Read(string root)
        {
            Settings s = new Settings();
            string path = Path.Combine(root, "musicworld.json");
            if (File.Exists(path)) JsonUtility.FromJsonOverwrite(File.ReadAllText(path), s);
            s.volume = Mathf.Clamp01(s.volume);
            s.fadeSeconds = Mathf.Clamp(s.fadeSeconds, 0.1f, 12f);
            s.exploreGapMin = Mathf.Clamp(s.exploreGapMin, 0, 3600);
            s.exploreGapMax = Mathf.Clamp(s.exploreGapMax, s.exploreGapMin, 3600);
            s.afterCombatQuiet = Mathf.Clamp(s.afterCombatQuiet, 0, 3600);
            s.combatHold = Mathf.Clamp(s.combatHold, 0, 60);
            return s;
        }
    }

    internal sealed class Deck
    {
        internal readonly AudioSource Audio;
        internal float Gain;
        internal bool OwnsClip;

        internal Deck(GameObject host)
        {
            Audio = host.AddComponent<AudioSource>();
            Audio.playOnAwake = false;
            Audio.loop = false;
            Audio.spatialBlend = 0f;
            Audio.volume = 0;
        }

        internal void Clear()
        {
            Audio.Stop();
            AudioClip clip = Audio.clip;
            Audio.clip = null;
            if (OwnsClip && clip) UnityEngine.Object.Destroy(clip);
            OwnsClip = false;
            Gain = 0f;
            Audio.volume = 0f;
        }
    }

    internal sealed class Player : IDisposable
    {
        readonly GameObject host;
        readonly NativeMusic native;
        readonly Deck[] decks;
        readonly Playlist[] lists;
        readonly Settings settings;
        readonly System.Random random = new System.Random();
        readonly CombatState combat = new CombatState();
        readonly Action<string> log;
        readonly Action<string> warn;
        int current = -1, lastDeck = 1;
        float nextObserve, nextTrack;
        bool nativeBattle, replacing, exhausted, paused;
        Mood mood;
        string candidate;
        UnityWebRequest download;
        ResourceRequest resource;

        internal Player(string root, Action<string> log, Action<string> warn)
        {
            this.log = log;
            this.warn = warn;
            settings = Settings.Read(root);
            lists = new [] {
                new Playlist(Path.Combine(root, "Music/Exploration"), Playlist.ExplorationDefaults, random),
                new Playlist(Path.Combine(root, "Music/Combat"), Playlist.CombatDefaults, random)
            };
            host = new GameObject("EGS-MUSICWORLD");
            UnityEngine.Object.DontDestroyOnLoad(host);
            native = new NativeMusic(host);
            decks = new [] { new Deck(host), new Deck(host) };
        }

        internal void Tick()
        {
            bool pauseNow = Time.timeScale <= 0.001f || AudioListener.pause;
            if (pauseNow != paused)
            {
                foreach (Deck d in decks)
                    if (d.Audio.clip) { if (pauseNow) d.Audio.Pause(); else d.Audio.UnPause(); }
                paused = pauseNow;
            }
            if (paused) return;
            float now = Time.time;
            if (now >= nextObserve)
            {
                nativeBattle = native.Observe(now, replacing);
                nextObserve = now + 0.18f;
            }
            Mood desired = combat.Observe(nativeBattle, now, settings.combatHold) ? Mood.Combat : Mood.Exploration;
            if (desired != mood)
            {
                mood = desired;
                CancelRequest();
                exhausted = false;
                if (mood == Mood.Exploration) current = -1;
                nextTrack = now + (mood == Mood.Combat ? 0 : settings.afterCombatQuiet);
                if (settings.debugLog) log("Mode: " + mood);
                // Keep the old cue audible until the incoming combat clip has loaded.
                if (mood == Mood.Combat) Begin();
            }

            if (candidate != null) Poll();
            if (current >= 0 && !decks[current].Audio.isPlaying)
            {
                decks[current].Clear();
                current = -1;
                nextTrack = now + (mood == Mood.Combat ? 0 : random.Next(settings.exploreGapMin, settings.exploreGapMax + 1));
            }
            if (current < 0 && candidate == null && !exhausted && now >= nextTrack) Begin();

            float step = Time.deltaTime / settings.fadeSeconds;
            for (int i = 0; i < decks.Length; ++i)
            {
                Deck d = decks[i];
                if (!d.Audio.clip) continue;
                d.Gain = Mathf.MoveTowards(d.Gain, i == current ? 1 : 0, step);
                d.Audio.volume = Mathf.Sin(d.Gain * Mathf.PI * 0.5f) * settings.volume;
                if (i != current && d.Gain <= 0f) d.Clear();
            }
        }

        void Begin()
        {
            candidate = lists[(int)mood].Next();
            if (candidate == null)
            {
                exhausted = true;
                current = -1;
                replacing = false;
                native.Release();
                warn("No playable tracks for " + mood + ". Restoring game music.");
                return;
            }
            try
            {
                if (!Path.IsPathRooted(candidate)) resource = Resources.LoadAsync<AudioClip>(candidate);
                else
                {
                    string ext = Path.GetExtension(candidate).ToLowerInvariant();
                    AudioType type = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".wav" ? AudioType.WAV : AudioType.MPEG;
                    download = UnityWebRequestMultimedia.GetAudioClip(new Uri(candidate).AbsoluteUri, type);
                    download.timeout = 30;
                    download.SendWebRequest();
                }
            }
            catch (Exception e) { Failed(e.Message); }
        }

        void Poll()
        {
            try
            {
                AudioClip clip;
                bool custom = download != null;
                if (custom)
                {
                    if (!download.isDone) return;
                    if (download.result != UnityWebRequest.Result.Success) { Failed(download.error); return; }
                    clip = DownloadHandlerAudioClip.GetContent(download);
                }
                else
                {
                    if (resource == null) { Failed("resource request unavailable"); return; }
                    if (!resource.isDone) return;
                    clip = resource.asset as AudioClip;
                }
                if (!clip || clip.length <= 0) { Failed("empty or unsupported audio"); return; }
                int i = 1 - lastDeck;
                Deck d = decks[i];
                d.Clear();
                d.Audio.clip = clip;
                d.OwnsClip = custom;
                d.Audio.outputAudioMixerGroup = native.MixerFor(mood);
                d.Audio.Play();
                lastDeck = current = i;
                replacing = true;
                if (settings.debugLog) log(mood + ": " + Path.GetFileName(candidate));
                CancelRequest();
            }
            catch (Exception e) { Failed(e.Message); }
        }

        void Failed(string reason)
        {
            warn("Skipping " + Path.GetFileName(candidate) + ": " + reason);
            lists[(int)mood].Reject(candidate);
            CancelRequest();
            current = -1;
            nextTrack = Time.time + 0.15f;
        }

        void CancelRequest()
        {
            if (download != null)
            {
                if (!download.isDone) download.Abort();
                download.Dispose();
                download = null;
            }
            resource = null;
            candidate = null;
        }

        public void Dispose()
        {
            CancelRequest();
            native.Release();
            foreach (Deck d in decks) d.Clear();
            UnityEngine.Object.Destroy(host);
        }
    }
}
