using UnityEngine;
using UnityEngine.UI;

namespace KitchenUIv3
{
    /// <summary>
    /// Sao chep sprite avatar khach du lich tu the don (Order_Card/Img_Avatar — V2 dang ghi vao)
    /// sang mot Image khac (vd bang TODAY'S SPECIAL) de 2 cho luon dong bo. Khong co khach thi an.
    /// </summary>
    public class KitchenV3AvatarMirror : MonoBehaviour
    {
        public Image nguon;    // Order_Card/Img_Avatar
        public Image dich;     // Chalkboard/Img_Avatar_Board (mac dinh = Image tren object nay)

        private void Awake() { if (dich == null) dich = GetComponent<Image>(); }

        private void LateUpdate()
        {
            if (nguon == null || dich == null) return;
            bool co = nguon.sprite != null && nguon.enabled;
            if (dich.enabled != co) dich.enabled = co;
            if (co && !ReferenceEquals(dich.sprite, nguon.sprite)) dich.sprite = nguon.sprite;
        }
    }
}
