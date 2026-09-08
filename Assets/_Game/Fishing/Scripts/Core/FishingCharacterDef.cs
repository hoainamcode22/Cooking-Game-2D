using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>Một nhân vật chọn được ở bước 1 (PlayerF / PlayerM). Tool wire prefab + sprite preview.</summary>
    [System.Serializable]
    public class FishingCharacterDef
    {
        public string characterId = FishingIds.CharacterF;
        public string displayName = "Cô gái";
        [Tooltip("Prefab do FishingPlayerAnimSetupTool tạo: Animator + SpriteRenderer + Rigidbody2D.")]
        public GameObject prefab;
        [Tooltip("Ảnh xem trước ở popup chọn nhân vật (= frame down_1).")]
        public Sprite previewSprite;
    }
}
