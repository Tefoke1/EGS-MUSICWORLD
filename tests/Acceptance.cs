using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MusicWorld
{
    internal static class Acceptance
    {
        static Player player;
        static readonly List<string> warnings = new List<string>();
        static void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); }
        static AudioSource Source(string obj, string clip)
        {
            AudioSource s = new GameObject(obj).AddComponent<AudioSource>();
            s.clip = new AudioClip { name = clip }; s.Play(); return s;
        }
        static void Advance(float seconds)
        {
            for (int i=0;i<(int)Math.Ceiling(seconds*10);i++)
            {
                Time.deltaTime=0.1f; if (Time.timeScale>0) Time.time+=0.1f;
                player.Tick();
            }
        }
        static AudioSource[] Playing()
        { return UnityEngine.Object.Sources.Where(s=>s.gameObject.name=="EGS-MUSICWORLD" && s.isPlaying).ToArray(); }
        static void Main(string[] args)
        {
            string root=args[0];
            var playlist=new Playlist(root,new[]{"a","b","c"},new System.Random(5));
            string last=null;
            for(int round=0;round<6;round++)
            {
                var tracks=new HashSet<string>();
                for(int i=0;i<3;i++)
                {
                    string n=playlist.Next();
                    if(n==last || !tracks.Add(n)) throw new Exception("repeat");
                    last=n;
                }
            }
            Check(true,"shuffle rounds and round boundaries");
            var ambience=Source("Landscape","bm_Tempest");
            var wind=Source("Environment","am_Water");
            var fight=Source("BattleSoundtrack","Encounter_v003_Low Intensity"); fight.Stop();
            player=new Player(root,Console.WriteLine,warnings.Add);
            Advance(3); Check(Playing().Length==1 && ambience.mute && !wind.mute,"exploration takes only native music");
            Playing()[0].Stop(); Advance(2);
            Check(Playing().Length==0 && ambience.mute && !wind.mute,"natural silence between exploration cues");
            fight.Play(); Advance(0.9f);
            Check(Playing().Length==1 && fight.mute && Playing()[0].volume>0 && Playing()[0].volume<0.4f,"fight interrupts silence with fade");
            Advance(2); fight.Stop(); Advance(2);
            Check(Playing().Length==1,"short gap keeps combat mode");
            Advance(4); Check(Playing().Length==0,"battle ends in silence");
            Advance(12); Check(Playing().Length==1 && Playing()[0].clip.name.StartsWith("sounds/biomes/"),"exploration returns after cooldown");
            fight.Play(); Advance(0.8f); Check(Playing().Length==2,"combat crossfades active exploration");
            Advance(3); Check(Playing().Length==1,"old deck released");
            Time.timeScale=0; Advance(1); Check(Playing().Length==0,"pause playback");
            Time.timeScale=1; Advance(0.5f); Check(Playing().Length==1,"resume playback");
            player.Dispose(); Check(!ambience.mute && !fight.mute && !wind.mute && Playing().Length==0,"restore sources on exit");

            fight.Stop();
            string explore=Path.Combine(root,"Music/Exploration"), combat=Path.Combine(root,"Music/Combat");
            Directory.CreateDirectory(explore); Directory.CreateDirectory(combat);
            File.WriteAllText(Path.Combine(explore,"broken.ogg"),"test");
            File.WriteAllText(Path.Combine(combat,"custom.wav"),"test");
            player=new Player(root,Console.WriteLine,warnings.Add);
            Advance(3);
            Check(warnings.Count==1 && Playing().Single().clip.name.StartsWith("sounds/biomes/"),"invalid custom track falls back");
            fight.Play(); Advance(4); Check(Playing().Single().clip.name.EndsWith("custom.wav"),"custom combat folder overrides built-ins");
            ambience.clip=new AudioClip {name="am_Wind"}; Advance(1);
            Check(!ambience.mute,"pooled source with a new sound is restored");
            player.Dispose();
        }
    }
}
