using System;
using System.Collections.Generic;
using System.IO;

namespace MusicWorld
{
    public enum Mood { Exploration, Combat }

    // Each round contains every candidate once. A failed file is skipped for this session.
    internal sealed class Playlist
    {
        internal static readonly string[] ExplorationDefaults = {
            "sounds/biomes/bm_adrift", "sounds/biomes/bm_morninglight", "sounds/biomes/bm_sanctuary",
            "sounds/biomes/bm_distantworlds", "sounds/biomes/bm_nebularclouds", "sounds/biomes/bm_voyage"
        };
        internal static readonly string[] CombatDefaults = {
            "sounds/combat/encounter_v003_low intensity", "sounds/combat/catalyst_v003_low intensity",
            "sounds/biomes/bm_vanguard", "sounds/biomes/bm_reaver"
        };
        readonly Random random;
        readonly List<string> custom = new List<string>();
        readonly List<string> fallback;
        readonly List<string> bag = new List<string>();
        string last;

        internal Playlist(string folder, string[] defaults, Random random)
        {
            this.random = random;
            fallback = new List<string>(defaults);
            if (Directory.Exists(folder))
                foreach (string f in Directory.GetFiles(folder))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".mp3" || ext == ".ogg" || ext == ".wav") custom.Add(Path.GetFullPath(f));
                }
        }

        internal string Next()
        {
            if (bag.Count == 0)
            {
                bag.AddRange(custom.Count > 0 ? custom : fallback);
                if (bag.Count == 0) return null;
                for (int i = bag.Count - 1; i > 0; --i) Swap(i, random.Next(i + 1));
                if (bag.Count > 1 && bag[bag.Count - 1] == last) Swap(bag.Count - 1, 0);
            }
            last = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            return last;
        }

        void Swap(int a, int b) { string t = bag[a]; bag[a] = bag[b]; bag[b] = t; }
        internal void Reject(string item) { custom.Remove(item); fallback.Remove(item); bag.Remove(item); }
    }

    internal sealed class CombatState
    {
        float lastBattle = float.NegativeInfinity;
        internal bool Observe(bool nativeBattlePlaying, float now, float hold)
        {
            if (nativeBattlePlaying) lastBattle = now;
            return nativeBattlePlaying || now - lastBattle < hold;
        }
    }
}
