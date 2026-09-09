using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Điểm điều phối DUY NHẤT cho phím Escape / nút Back Android của mọi popup-panel module Hồ Câu (CHỦ: Dev C).
    /// Popup gọi Push(this) khi mở, Remove(this) khi đóng (kể cả OnDisable). Trong Update của mình, popup hỏi
    /// ConsumeEscape(this): chỉ trả true cho popup ở ĐỈNH stack, và mỗi frame chỉ 1 lần → 1 lần bấm chỉ đóng 1 popup.
    /// Không cần MonoBehaviour riêng: popup nào đang mở thì Update của nó đang chạy, đủ để hỏi.
    /// Popup KHÔNG tham gia: FishingResultToastUI, FishingInviteHintUI (thẻ báo, tự ẩn), CastPowerMeterUI (không phải popup).
    /// </summary>
    public static class FishingPopupStack
    {
        private static readonly List<Component> _stack = new List<Component>(8);
        private static int _consumedFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _stack.Clear(); _consumedFrame = -1; }

        /// <summary>Số popup đang mở (đã bỏ object bị huỷ).</summary>
        public static int Count
        {
            get { Prune(); return _stack.Count; }
        }

        /// <summary>Popup trên cùng, null nếu không có.</summary>
        public static Component Top
        {
            get { Prune(); return _stack.Count > 0 ? _stack[_stack.Count - 1] : null; }
        }

        /// <summary>Đưa popup lên đỉnh (gọi khi mở). Gọi lại khi đã có → chỉ đưa lên đỉnh, không trùng.</summary>
        public static void Push(Component owner)
        {
            if (owner == null) { return; }
            Prune();
            _stack.Remove(owner);
            _stack.Add(owner);
        }

        /// <summary>Bỏ popup khỏi stack (gọi khi đóng / OnDisable). Không có trong stack → bỏ qua.</summary>
        public static void Remove(Component owner)
        {
            Prune();
            if (owner == null) { return; }
            _stack.Remove(owner);
        }

        public static bool Contains(Component owner)
        {
            if (owner == null) { return false; }
            Prune();
            return _stack.Contains(owner);
        }

        public static bool IsTop(Component owner)
        {
            if (owner == null) { return false; }
            Component top = Top;
            return top != null && top == owner;
        }

        /// <summary>Escape (PC) / Back (Android) vừa bấm frame này. Hỏi cả Input System lẫn Input cũ (project để activeInputHandler = Both).</summary>
        public static bool EscapePressedThisFrame()
        {
            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { return true; }
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) { return true; }
#endif
            return false;
        }

        /// <summary>
        /// True đúng 1 lần mỗi frame, chỉ cho popup ở đỉnh stack, khi Escape vừa bấm. Popup nhận true thì tự đóng.
        /// </summary>
        public static bool ConsumeEscape(Component owner)
        {
            if (owner == null) { return false; }
            if (_consumedFrame == Time.frameCount) { return false; }
            if (!IsTop(owner)) { return false; }
            if (!EscapePressedThisFrame()) { return false; }
            _consumedFrame = Time.frameCount;
            return true;
        }

        /// <summary>Đánh dấu Escape frame này đã được xử lý (vd. ChatPanel dùng Escape để bỏ focus ô gõ, không đóng).</summary>
        public static void MarkEscapeConsumed() { _consumedFrame = Time.frameCount; }

        /// <summary>Bỏ các entry đã bị Destroy (Unity fake-null: so == null tường minh).</summary>
        private static void Prune()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i] == null) { _stack.RemoveAt(i); }
            }
        }
    }
}
