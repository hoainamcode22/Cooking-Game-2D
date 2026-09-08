using System;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Máy trạng thái một lượt câu — POCO thuần C#: KHÔNG UnityEngine.Time / Random / PlayerPrefs,
    /// mọi số ngẫu nhiên và dt đều truyền từ ngoài vào để test được không cần Unity.
    ///
    /// Idle → (TryCast) → Casting → Waiting → Bite → (TryReel) → Reeling → Result → Idle
    ///                                 │          └─ hết cửa sổ không THU → Result (Escaped)
    ///                                 └─ (TryReel) sớm → Result (Nothing)
    ///
    /// Bắt được hay không KHÔNG quyết định ở đây: FishingController roll random rồi gọi
    /// <see cref="SetReelOutcome"/> trước <see cref="TryReel"/> trong pha Bite.
    /// </summary>
    public sealed class FishingStateMachine
    {
        // Cấu hình lượt câu (giây) — chép từ FishingConfig, giữ dạng primitive để test.
        private readonly float _castDuration;
        private readonly float _biteWaitMin;
        private readonly float _biteWaitMax;
        private readonly float _biteWindow;
        private readonly float _reelDuration;
        private readonly float _resultShow;

        // Hiệu chỉnh từ cần đang cầm (RodData), đặt lại mỗi lần quăng.
        private float _rodBiteWaitMultiplier = 1f;
        private float _rodBiteWindowBonus = 0f;

        // Kết quả roll của controller, áp vào LastOutcome lúc bước vào Reeling.
        private bool _pendingCaught;

        public FishingPhase Phase { get; private set; } = FishingPhase.Idle;
        /// <summary>Giây đã trôi trong pha hiện tại.</summary>
        public float PhaseElapsed { get; private set; }
        /// <summary>Tổng giây phải chờ ở Waiting trước khi cá cắn (đã nhân hệ số cần).</summary>
        public float BiteWaitTarget { get; private set; }
        /// <summary>Kết cục lượt vừa rồi; đọc ở pha Result. None khi chưa có.</summary>
        public FishingOutcome LastOutcome { get; private set; } = FishingOutcome.None;

        /// <summary>Cửa sổ THU thật sự (config + bonus cần), luôn &gt; 0 để không chia cho 0.</summary>
        public float BiteWindowTotal
        {
            get { float w = _biteWindow + _rodBiteWindowBonus; return w > 0.01f ? w : 0.01f; }
        }

        /// <summary>1 → 0 trong pha Bite (HUD vẽ vòng đếm ngược); 0 ở mọi pha khác.</summary>
        public float BiteWindowRemaining01
        {
            get
            {
                if (Phase != FishingPhase.Bite) { return 0f; }
                float r = 1f - PhaseElapsed / BiteWindowTotal;
                if (r < 0f) { return 0f; }
                if (r > 1f) { return 1f; }
                return r;
            }
        }

        /// <summary>(pha cũ, pha mới) — bắn đúng 1 lần mỗi lần đổi pha.</summary>
        public event Action<FishingPhase, FishingPhase> OnPhaseChanged;

        public FishingStateMachine(float castDuration, float biteWaitMin, float biteWaitMax,
                                   float biteWindow, float reelDuration, float resultShow)
        {
            // Kẹp sàn nhỏ để số 0/âm trong config không làm pha nhảy tức thì hoặc kẹt vĩnh viễn.
            _castDuration = Max(castDuration, 0.01f);
            _biteWaitMin = Max(biteWaitMin, 0f);
            _biteWaitMax = Max(biteWaitMax, _biteWaitMin);
            _biteWindow = Max(biteWindow, 0.01f);
            _reelDuration = Max(reelDuration, 0.01f);
            _resultShow = Max(resultShow, 0.01f);
        }

        /// <summary>Hiệu chỉnh từ RodData: nhân thời gian chờ cắn, cộng giây vào cửa sổ THU.</summary>
        public void SetRodModifiers(float biteWaitMultiplier, float biteWindowBonus)
        {
            _rodBiteWaitMultiplier = biteWaitMultiplier > 0.01f ? biteWaitMultiplier : 0.01f;
            _rodBiteWindowBonus = biteWindowBonus > 0f ? biteWindowBonus : 0f;
        }

        /// <summary>Idle → Casting. rollBiteWait01 (0..1) chọn điểm trong [min, max] rồi nhân hệ số cần. Trả false nếu không ở Idle.</summary>
        public bool TryCast(float rollBiteWait01)
        {
            if (Phase != FishingPhase.Idle) { return false; }
            float t = Clamp01(rollBiteWait01);
            BiteWaitTarget = (_biteWaitMin + (_biteWaitMax - _biteWaitMin) * t) * _rodBiteWaitMultiplier;
            LastOutcome = FishingOutcome.None;
            _pendingCaught = false;
            ChangePhase(FishingPhase.Casting);
            return true;
        }

        /// <summary>Controller đặt kết quả roll (bắt được / thoát) TRƯỚC khi gọi TryReel trong pha Bite.</summary>
        public void SetReelOutcome(bool caught)
        {
            _pendingCaught = caught;
            // Cho phép gọi trễ (đã vào Reeling) — vẫn cập nhật để Result đọc đúng.
            if (Phase == FishingPhase.Reeling) { LastOutcome = caught ? FishingOutcome.Caught : FishingOutcome.Escaped; }
        }

        /// <summary>Bấm THU. Waiting → Result(Nothing); Bite → Reeling (kết quả theo SetReelOutcome); pha khác → Ignored.</summary>
        public ReelResult TryReel()
        {
            switch (Phase)
            {
                case FishingPhase.Waiting:
                    LastOutcome = FishingOutcome.Nothing;
                    ChangePhase(FishingPhase.Result);
                    return ReelResult.Nothing;
                case FishingPhase.Bite:
                    LastOutcome = _pendingCaught ? FishingOutcome.Caught : FishingOutcome.Escaped;
                    ChangePhase(FishingPhase.Reeling);
                    return ReelResult.BiteAttempt;
                default:
                    return ReelResult.Ignored;
            }
        }

        /// <summary>Ép lượt đang chạy về Result ngay (huỷ giữa chừng); Result tự về Idle sau resultShow. Không làm gì ở Idle.</summary>
        public void SetResultAndReturn()
        {
            if (Phase == FishingPhase.Idle || Phase == FishingPhase.Result) { return; }
            ChangePhase(FishingPhase.Result);
        }

        /// <summary>Về Idle tức thì, không qua Result (đổi scene, huỷ controller).</summary>
        public void ForceIdle()
        {
            if (Phase == FishingPhase.Idle) { return; }
            LastOutcome = FishingOutcome.None;
            ChangePhase(FishingPhase.Idle);
        }

        /// <summary>Gọi mỗi frame với deltaTime. Mỗi Tick đổi tối đa MỘT pha.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f || Phase == FishingPhase.Idle) { return; }
            PhaseElapsed += dt;

            switch (Phase)
            {
                case FishingPhase.Casting:
                    if (PhaseElapsed >= _castDuration) { ChangePhase(FishingPhase.Waiting); }
                    break;
                case FishingPhase.Waiting:
                    if (PhaseElapsed >= BiteWaitTarget) { ChangePhase(FishingPhase.Bite); }
                    break;
                case FishingPhase.Bite:
                    // Hết cửa sổ mà không THU → cá thoát.
                    if (PhaseElapsed >= BiteWindowTotal) { LastOutcome = FishingOutcome.Escaped; ChangePhase(FishingPhase.Result); }
                    break;
                case FishingPhase.Reeling:
                    if (PhaseElapsed >= _reelDuration) { ChangePhase(FishingPhase.Result); }
                    break;
                case FishingPhase.Result:
                    if (PhaseElapsed >= _resultShow) { ChangePhase(FishingPhase.Idle); }
                    break;
            }
        }

        private void ChangePhase(FishingPhase next)
        {
            if (next == Phase) { return; }
            FishingPhase prev = Phase;
            Phase = next;
            // Reset về 0 (không mang phần dư): một frame giật dài không được ăn mất cửa sổ Bite của người chơi.
            PhaseElapsed = 0f;
            OnPhaseChanged?.Invoke(prev, next);
        }

        private static float Max(float a, float b) { return a > b ? a : b; }
        private static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }
    }
}
