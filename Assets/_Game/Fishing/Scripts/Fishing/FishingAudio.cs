using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Âm thanh Hồ Câu — AudioSource riêng trên GameObject ẩn DontDestroyOnLoad (tạo lazy), clip nạp từ
    /// Resources/Audio/Fishing/{cast,splash,bite,reel,catch,fail}. Chưa có clip → fallback sang tiếng có sẵn của AudioManager.
    /// Âm lượng nhân AudioManager.SfxGain để thanh trượt SFX của người chơi có hiệu lực. KHÔNG sửa AudioManager.
    /// </summary>
    public static class FishingAudio
    {
        private const string ResourceFolder = "Audio/Fishing/";
        private const string SourceObjectName = "FishingAudio";

        private static AudioSource _source;
        private static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> _missing = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _source = null;
            _clips.Clear();
            _missing.Clear();
        }

        public static void PlayCast()
        {
            if (!TryPlay("cast", 0.8f)) { AudioManager.Instance?.PlayUIClick(); }
        }

        public static void PlaySplash()
        {
            if (!TryPlay("splash", 0.9f)) { AudioManager.Instance?.PlayBubblePop(); }
        }

        public static void PlayBite()
        {
            if (!TryPlay("bite", 1f)) { AudioManager.Instance?.PlayCoinTing(); }
        }

        public static void PlayReel()
        {
            if (!TryPlay("reel", 0.8f)) { AudioManager.Instance?.PlayUIClick(); }
        }

        public static void PlayCatch()
        {
            if (!TryPlay("catch", 1f)) { AudioManager.Instance?.PlaySuccess(); }
        }

        public static void PlayFail()
        {
            if (!TryPlay("fail", 0.7f)) { AudioManager.Instance?.PlayUIClick(); }
        }

        /// <summary>Phát clip riêng nếu có; false = không có clip (caller fallback). Không phát khi ngoài Play Mode.</summary>
        private static bool TryPlay(string name, float volume)
        {
            if (!Application.isPlaying) { return true; }
            AudioClip clip = LoadClip(name);
            if (clip == null) { return false; }
            AudioSource src = Source;
            if (src == null) { return false; }
            float gain = AudioManager.SfxGain;
            if (gain <= 0f) { return true; }   // SFX đang tắt: coi như đã "phát", không fallback (fallback cũng im)
            src.PlayOneShot(clip, Mathf.Clamp01(volume) * gain);
            return true;
        }

        private static AudioClip LoadClip(string name)
        {
            AudioClip clip;
            if (_clips.TryGetValue(name, out clip) && clip != null) { return clip; }
            if (_missing.Contains(name)) { return null; }
            clip = Resources.Load<AudioClip>(ResourceFolder + name);
            if (clip == null) { _missing.Add(name); return null; }   // nhớ là thiếu để không Resources.Load lại mỗi lần bấm
            _clips[name] = clip;
            return clip;
        }

        private static AudioSource Source
        {
            get
            {
                // So == null tường minh: object có thể bị huỷ (fake-null) khi Sếp xoá tay trong Editor.
                if (_source == null)
                {
                    var go = new GameObject(SourceObjectName);
                    go.hideFlags = HideFlags.HideInHierarchy;
                    Object.DontDestroyOnLoad(go);
                    _source = go.AddComponent<AudioSource>();
                    _source.playOnAwake = false;
                    _source.loop = false;
                    _source.spatialBlend = 0f;
                    _source.volume = 1f;   // âm lượng thật nhân SfxGain lúc phát (cùng cách AudioManager)
                }
                return _source;
            }
        }
    }
}
