using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ExportTrainUIPackage
{
    public static class TrainSpriteLoader
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite GetSprite(string relativeOrFullPath)
        {
            if (string.IsNullOrEmpty(relativeOrFullPath)) return null;

            string normalizedPath = relativeOrFullPath.Replace("\\", "/");

            if (_cache.TryGetValue(normalizedPath, out Sprite cached) && cached != null && cached.texture != null)
            {
                return cached;
            }

#if UNITY_EDITOR
            Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(normalizedPath);
            if (sp != null)
            {
                _cache[normalizedPath] = sp;
                return sp;
            }
#endif

            // Fallback: Read raw bytes directly from disk so it NEVER fails even if Unity importer is pending
            string diskPath = normalizedPath;
            if (!File.Exists(diskPath) && !Path.IsPathRooted(diskPath))
            {
                diskPath = Path.Combine(Application.dataPath, normalizedPath.StartsWith("Assets/") ? normalizedPath.Substring(7) : normalizedPath);
            }

            if (File.Exists(diskPath))
            {
                byte[] data = File.ReadAllBytes(diskPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(data))
                {
                    tex.name = Path.GetFileNameWithoutExtension(diskPath);
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;

                    // Calculate border if known
                    Vector4 border = Vector4.zero;
                    if (diskPath.Contains("popup_frame_wood") || diskPath.Contains("shop_panel")) border = new Vector4(36, 36, 36, 36);
                    else if (diskPath.Contains("popup_panel_paper") || diskPath.Contains("shop_card_inner") || diskPath.Contains("shop_card_outer")) border = new Vector4(20, 20, 20, 20);
                    else if (diskPath.Contains("ribbon_banner_gold") || diskPath.Contains("shop_banner_ribbon")) border = new Vector4(28, 14, 28, 14);
                    else if (diskPath.Contains("btn_")) border = new Vector4(16, 16, 16, 16);
                    else if (diskPath.Contains("progress_track_bar")) border = new Vector4(14, 10, 14, 10);
                    else if (diskPath.Contains("bubble_cargo_req")) border = new Vector4(20, 20, 20, 20);
                    else if (diskPath.Contains("timer_box_dark")) border = new Vector4(16, 16, 16, 16);
                    else if (diskPath.Contains("mini_train_track_bg")) border = new Vector4(16, 16, 16, 16);

                    Sprite newSp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                    _cache[normalizedPath] = newSp;
                    return newSp;
                }
            }

            return null;
        }
    }
}
