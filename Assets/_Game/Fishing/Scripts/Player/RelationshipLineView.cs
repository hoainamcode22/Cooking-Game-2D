using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Trên object RemotePlayers: vẽ LineRenderer từ HeadAnchor của mình tới HeadAnchor của người có kết nối
    /// (Bạn bè xanh · Chị em vàng · Hẹn hò đỏ, màu từ FishingConfig.LineColorFor). Pool theo playerId, LateUpdate vì đọc vị trí người khác.
    /// </summary>
    public class RelationshipLineView : MonoBehaviour
    {
        private const string LineShaderName = "Sprites/Default";

        [Tooltip("Sorting layer mong muốn; rỗng/không tồn tại thì rơi về TouristSortingLayers.Overlay (Foreground).")]
        [SerializeField] private string sortingLayerName = "Foreground";
        [SerializeField] private int sortingOrder = 300;

        private readonly Dictionary<string, LineRenderer> _lines = new Dictionary<string, LineRenderer>();
        private readonly HashSet<string> _usedThisFrame = new HashSet<string>();
        private readonly List<string> _toRemove = new List<string>();
        private static Material _lineMaterial;
        private FishingConfig _cfg;
        private string _resolvedLayer;

        private void Awake()
        {
            _cfg = FishingDatabase.ConfigOrDefault;
            _resolvedLayer = TouristSortingLayers.ResolveOrOverride(sortingLayerName, TouristSortingLayers.Overlay);
        }

        private void LateUpdate()
        {
            var mgr = RemotePlayersManager.Instance;
            var local = FishingPlayerController.Local;
            var friends = FishingNetHub.Friends;
            _usedThisFrame.Clear();

            if (mgr != null && local != null && local.HeadAnchor != null && friends != null)
            {
                IReadOnlyList<RemotePlayerView> views = mgr.All;
                for (int i = 0; i < views.Count; i++)
                {
                    var v = views[i];
                    if (v == null || v.HeadAnchor == null || string.IsNullOrEmpty(v.PlayerId)) { continue; }
                    RelationshipKind kind = friends.GetRelationship(v.PlayerId);
                    if (kind == RelationshipKind.None) { continue; }

                    LineRenderer lr = GetOrCreate(v.PlayerId);
                    lr.enabled = true;
                    lr.SetPosition(0, local.HeadAnchor.position);
                    lr.SetPosition(1, v.HeadAnchor.position);
                    Color c = _cfg.LineColorFor(kind);
                    lr.startColor = c; lr.endColor = c;
                    lr.startWidth = _cfg.relationshipLineWidth; lr.endWidth = _cfg.relationshipLineWidth;
                    _usedThisFrame.Add(v.PlayerId);
                }
            }

            // Tắt line không còn quan hệ; dọn entry của view đã bị huỷ.
            _toRemove.Clear();
            foreach (var kv in _lines)
            {
                if (kv.Value == null) { _toRemove.Add(kv.Key); continue; }
                if (!_usedThisFrame.Contains(kv.Key)) { kv.Value.enabled = false; }
            }
            for (int i = 0; i < _toRemove.Count; i++) { _lines.Remove(_toRemove[i]); }
        }

        private LineRenderer GetOrCreate(string playerId)
        {
            LineRenderer lr;
            if (_lines.TryGetValue(playerId, out lr) && lr != null) { return lr; }

            var go = new GameObject("Line_" + playerId);
            go.transform.SetParent(transform, false);
            lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.numCapVertices = 4;
            lr.textureMode = LineTextureMode.Stretch;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingLayerName = _resolvedLayer;
            lr.sortingOrder = sortingOrder;
            Material m = GetLineMaterial();
            if (m != null) { lr.sharedMaterial = m; }
            _lines[playerId] = lr;
            return lr;
        }

        /// <summary>Sprites/Default nhận màu đỉnh (startColor/endColor) — cache 1 material cho mọi line.</summary>
        private static Material GetLineMaterial()
        {
            if (_lineMaterial != null) { return _lineMaterial; }
            Shader sh = Shader.Find(LineShaderName);
            if (sh == null) { Debug.LogWarning(FishingIds.LogTag + " Không tìm thấy shader " + LineShaderName + " — line dùng material mặc định."); return null; }
            _lineMaterial = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            return _lineMaterial;
        }
    }
}
