using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiAudioManager : MonoBehaviour
    {
        public static YokaiAudioManager Instance { get; private set; }

        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Queue<AudioSource> pool = new Queue<AudioSource>();
        AudioSource ambience;
        bool bossMode;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Load("slash_light");
            Load("slash_heavy");
            Load("parry");
            Load("dodge");
            Load("hunter_art");
            Load("boss_telegraph");
            Load("ambient_forest");
            Load("ambient_boss");

            for (int i=0;i<8;i++) pool.Enqueue(CreateSource("SFX_" + i));

            ambience = CreateSource("Ambience");
            ambience.loop = true;
            ambience.spatialBlend = 0f;
            ambience.volume = .28f;
            PlayAmbience(false);
        }

        AudioSource CreateSource(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform,false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f;
            source.maxDistance = 24f;
            return source;
        }

        void Load(string id)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + id);
            if (clip != null) clips[id] = clip;
        }

        public void Play(string id, Vector3 position, float volume, float pitch)
        {
            AudioClip clip;
            if (!clips.TryGetValue(id, out clip) || clip == null) return;

            AudioSource source = GetSource();
            source.transform.position = position;
            source.spatialBlend = .72f;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, .6f, 1.5f);
            source.clip = clip;
            source.loop = false;
            source.Play();
        }

        AudioSource GetSource()
        {
            int count = pool.Count;
            for (int i=0;i<count;i++)
            {
                AudioSource s = pool.Dequeue();
                pool.Enqueue(s);
                if (!s.isPlaying) return s;
            }

            AudioSource extra = CreateSource("SFX_Extra");
            pool.Enqueue(extra);
            return extra;
        }

        public void SetBossMode(bool enabled)
        {
            if (bossMode == enabled) return;
            bossMode = enabled;
            PlayAmbience(enabled);
        }

        void PlayAmbience(bool boss)
        {
            if (ambience == null) return;

            string id = boss ? "ambient_boss" : "ambient_forest";
            AudioClip clip;
            if (!clips.TryGetValue(id, out clip) || clip == null) return;

            ambience.Stop();
            ambience.clip = clip;
            ambience.volume = boss ? .34f : .25f;
            ambience.pitch = 1f;
            ambience.Play();
        }
    }

    public static class YokaiAudio
    {
        public static void Play(string id, Vector3 position, float volume, float pitch)
        {
            if (YokaiAudioManager.Instance != null)
                YokaiAudioManager.Instance.Play(id, position, volume, pitch);
        }

        public static void Play(string id, Vector3 position)
        {
            Play(id, position, 1f, 1f);
        }
    }
}
