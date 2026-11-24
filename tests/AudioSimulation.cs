// Stand-ins for the Unity audio boundary. The production assembly references the game's DLLs.
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public class Object
    {
        internal static readonly List<AudioSource> Sources = new List<AudioSource>();
        public static implicit operator bool(Object o) { return o != null; }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode unused) { return Sources.OfType<T>().ToArray(); }
        public static void DontDestroyOnLoad(Object o) {}
        public static void Destroy(Object o) { if (o is GameObject) Sources.RemoveAll(s => s.gameObject == o); }
    }
    public enum FindObjectsSortMode { None }
    public class GameObject : Object
    {
        public string name;
        public GameObject(string value) { name = value; }
        public T AddComponent<T>() where T : AudioSource, new()
        { T s = new T(); s.gameObject = this; Sources.Add(s); return s; }
    }
    public class AudioClip : Object { public string name; public float length = 10; }
    public class AudioSource : Object
    {
        public GameObject gameObject;
        public AudioClip clip;
        public Audio.AudioMixerGroup outputAudioMixerGroup;
        public bool isActiveAndEnabled = true, isPlaying, playOnAwake, loop, mute;
        public float volume = 1, spatialBlend;
        public void Play() { isPlaying = clip != null; }
        public void Stop() { isPlaying = false; }
        public void Pause() { isPlaying = false; }
        public void UnPause() { isPlaying = clip != null; }
    }
    public static class Time { public static float timeScale = 1, time, deltaTime; }
    public static class AudioListener { public static bool pause = false; }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Sin(float x) { return (float)Math.Sin(x); }
        public static float Clamp(float x,float min,float max) { return Math.Max(min, Math.Min(max,x)); }
        public static int Clamp(int x,int min,int max) { return Math.Max(min, Math.Min(max,x)); }
        public static float Clamp01(float x) { return Clamp(x,0,1); }
        public static float MoveTowards(float x,float y,float d) { return x < y ? Math.Min(y,x+d) : Math.Max(y,x-d); }
    }
    public enum AudioType { WAV, MPEG, OGGVORBIS }
    public class ResourceRequest { public bool isDone = true; public Object asset; }
    public static class Resources
    {
        public static ResourceRequest LoadAsync<T>(string name)
        { return new ResourceRequest { asset = new AudioClip { name = name } }; }
    }
    public static class JsonUtility { public static void FromJsonOverwrite(string json, object value) { throw new NotSupportedException(); } }
}
namespace UnityEngine.Audio { public class AudioMixerGroup : UnityEngine.Object {} }
namespace UnityEngine.Networking
{
    public class UnityWebRequest : IDisposable
    {
        public enum Result { Success, DataProcessingError }
        public bool isDone; public int timeout; public string error = "simulated decode error";
        public Result result; public string url;
        public void SendWebRequest() { isDone=true; result=url.Contains("broken") ? Result.DataProcessingError : Result.Success; }
        public void Abort() {} public void Dispose() {}
    }
    public static class UnityWebRequestMultimedia
    {
        public static UnityWebRequest GetAudioClip(string uri, UnityEngine.AudioType type) { return new UnityWebRequest { url=uri }; }
    }
    public static class DownloadHandlerAudioClip
    {
        public static UnityEngine.AudioClip GetContent(UnityWebRequest req) { return new UnityEngine.AudioClip {name=req.url}; }
    }
}
