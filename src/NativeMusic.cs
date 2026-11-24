using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicWorld
{
    internal sealed class NativeMusic
    {
        readonly Dictionary<AudioSource, bool> ownedMute = new Dictionary<AudioSource, bool>();
        readonly List<AudioSource> expired = new List<AudioSource>();
        readonly GameObject host;
        AudioSource[] sources = new AudioSource[0];
        float nextScan;
        UnityEngine.Audio.AudioMixerGroup explorationMixer, combatMixer;

        internal UnityEngine.Audio.AudioMixerGroup MixerFor(Mood mood)
        {
            return mood == Mood.Combat ? combatMixer : explorationMixer;
        }

        internal NativeMusic(GameObject host) { this.host = host; }

        static bool IsCombatClip(string name)
        {
            return name.Equals("Encounter_v003_Low Intensity", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Catalyst_v003_Low Intensity", StringComparison.OrdinalIgnoreCase)
                || name.Equals("MetalDoomed_Planet_Boss", StringComparison.OrdinalIgnoreCase)
                || name.Equals("MetalAndroids_Space_Boss", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Metal Vol2 Scalpels_Soft_Planet", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Metal Vol2 Crater_Soft_Space", StringComparison.OrdinalIgnoreCase);
        }

        internal bool Observe(float now, bool replace)
        {
            if (now >= nextScan)
            {
                sources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
                nextScan = now + 1.5f;
            }
            expired.Clear();
            expired.AddRange(ownedMute.Keys);
            bool combat = false;
            foreach (AudioSource s in sources)
            {
                if (!s || s.gameObject == host || !s.isActiveAndEnabled || !s.isPlaying || !s.clip) continue;
                string name = s.clip.name;
                bool battle = IsCombatClip(name) || s.gameObject.name == "BattleSoundtrack";
                if (!battle && !name.StartsWith("bm_", StringComparison.OrdinalIgnoreCase)) continue;
                combat |= battle;
                if (s.outputAudioMixerGroup)
                {
                    if (battle) combatMixer = s.outputAudioMixerGroup;
                    else explorationMixer = s.outputAudioMixerGroup;
                }
                if (replace)
                {
                    if (!ownedMute.ContainsKey(s)) ownedMute.Add(s, s.mute);
                    if (!s.mute) s.mute = true;
                    expired.Remove(s);
                }
            }
            foreach (AudioSource s in expired) Restore(s);
            return combat;
        }

        void Restore(AudioSource s)
        {
            if (s) s.mute = ownedMute[s];
            ownedMute.Remove(s);
        }

        internal void Release()
        {
            foreach (AudioSource s in new List<AudioSource>(ownedMute.Keys)) Restore(s);
        }
    }
}
