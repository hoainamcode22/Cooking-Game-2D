using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Pool splash — plain C# (không MonoBehaviour), kiểu MillFxPool: giữ GameObject đã tạo, SetActive thay Instantiate/Destroy.
    /// Object con nằm dưới root "FishingSplashPool" trong scene; đổi scene root chết → phần tử thành fake-null nên mọi chỗ
    /// so `== null` TƯỜNG MINH (?. / ?? không hiểu fake-null của Unity).
    /// </summary>
    public sealed class FishingSplashPool
    {
        private const int MaxPooled = 8;   // đủ cho quăng + thu + bot; vượt thì tái dùng cái cũ nhất

        private static FishingSplashPool _shared;

        public static FishingSplashPool Shared
        {
            get
            {
                if (_shared == null) { _shared = new FishingSplashPool(); }
                return _shared;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _shared = null; }

        private readonly List<FishingSplashFX> _pool = new List<FishingSplashFX>();
        private Transform _root;

        /// <summary>
        /// Phát splash tại worldPos. sortingRef = SpriteRenderer của phao → vẽ cùng layer, order +100 (luôn TRƯỚC phao/người).
        /// sortingRef null → layer theo TouristSortingLayers.Visitor, order 100.
        /// </summary>
        public void Play(Vector2 worldPos, SpriteRenderer sortingRef, float scale = 1f)
        {
            if (!Application.isPlaying) { return; }
            string layer;
            int order;
            if (sortingRef != null)
            {
                layer = sortingRef.sortingLayerName;
                order = sortingRef.sortingOrder + 100;
            }
            else
            {
                layer = TouristSortingLayers.Resolve(TouristSortingLayers.Visitor);
                order = 100;
            }

            FishingSplashFX fx = Acquire();
            if (fx == null) { return; }
            fx.Play(worldPos, layer, order, scale);
        }

        /// <summary>Tắt hết splash đang chạy (đổi scene / huỷ controller).</summary>
        public void StopAll()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] == null) { continue; }
                _pool[i].Stop();
            }
        }

        private FishingSplashFX Acquire()
        {
            // Dọn phần tử đã bị huỷ (fake-null) trước.
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                if (_pool[i] == null) { _pool.RemoveAt(i); }
            }
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].IsPlaying) { return _pool[i]; }
            }
            if (_pool.Count >= MaxPooled)
            {
                // Đầy: lấy cái đầu (cũ nhất) chạy lại, đưa về cuối danh sách.
                FishingSplashFX oldest = _pool[0];
                _pool.RemoveAt(0);
                _pool.Add(oldest);
                return oldest;
            }

            Transform root = Root;
            if (root == null) { return null; }
            var go = new GameObject("Splash_" + _pool.Count);
            go.transform.SetParent(root, false);
            go.SetActive(false);
            FishingSplashFX fx = go.AddComponent<FishingSplashFX>();
            _pool.Add(fx);
            return fx;
        }

        private Transform Root
        {
            get
            {
                if (_root == null)
                {
                    // Root là object scene thường (không DontDestroyOnLoad) để về farm là dọn sạch theo scene.
                    var go = new GameObject("FishingSplashPool");
                    _root = go.transform;
                    _pool.Clear();
                }
                return _root;
            }
        }
    }
}
