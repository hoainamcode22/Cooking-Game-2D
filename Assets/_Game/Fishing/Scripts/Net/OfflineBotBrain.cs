using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Bộ não 1 bot OFFLINE (plain C#, không MonoBehaviour). CHỦ FILE: Dev D.
    /// Máy trạng thái: Wander (đi tới điểm random trong walkArea) → Idle 2-5s → GoFish (đi tới điểm câu gần nhất,
    /// chạy các pha Casting → Waiting → Bite → Reeling → Result theo thời gian trong FishingConfig) → lặp lại.
    /// Đôi lúc (12-25s) bot nói 1 câu mẫu; khi người chơi chat, room chọn 1 bot gọi ScheduleReply → đáp sau 1-3s.
    /// Dùng System.Random (seed) để tái lập, KHÔNG dùng UnityEngine.Random. Bubble dùng long Unix.
    /// Tick() trả true khi state cần đẩy lên (OnPlayerUpdated): đổi pha/bubble/moving ngay lập tức,
    /// vị trí thì theo nhịp cfg.netPushRate để giống hành vi mạng thật.
    /// </summary>
    public class OfflineBotBrain
    {
        private enum Mode { Wander = 0, Idle = 1, GoFish = 2, Fishing = 3 }

        /// <summary>Câu bot tự nói (vô hại, tiếng Việt).</summary>
        private static readonly string[] IdleLines =
        {
            "Hôm nay nước đẹp quá",
            "Câu được con gì chưa?",
            "Chào bạn 👋",
            "Chỗ này nhiều cá lắm",
            "Trời mát câu cá là nhất",
            "Mình vừa sẩy một con to 😅",
            "Ai câu được cá koi chưa?",
            "Ngồi câu thư giãn thật",
        };

        /// <summary>Câu bot đáp khi người chơi chat.</summary>
        private static readonly string[] ReplyLines =
        {
            "Hello!",
            "Chào bạn mới",
            "Câu vui nhé",
            "Hì hì 😄",
            "Ừ đúng rồi",
            "Chúc bạn câu được cá to",
        };

        public PlayerNetState State { get; private set; }

        private readonly FishingConfig _cfg;
        private readonly System.Random _rng;
        private readonly Action<string, string> _chatSink;   // (botId, text)

        private Rect _walkArea = new Rect(-4f, -3f, 8f, 6f);
        private readonly List<Vector2> _spots = new List<Vector2> { new Vector2(0f, -1f) };

        private Mode _mode = Mode.Idle;
        private float _modeTimer;
        private Vector2 _target;
        private int _fishCyclesBeforeWander;

        // Pha câu
        private FishingPhase _phase = FishingPhase.Idle;
        private float _phaseTimer;

        // Chat
        private float _nextIdleChatIn;
        private float _replyIn = -1f;
        private float _pushAccumulator;
        private bool _positionDirty;

        public OfflineBotBrain(PlayerNetState state, FishingConfig cfg, System.Random rng, Action<string, string> chatSink)
        {
            State = state;
            _cfg = cfg;
            _rng = rng ?? new System.Random(1);
            _chatSink = chatSink;
            _modeTimer = RandRange(0.5f, 2f);
            _nextIdleChatIn = RandRange(12f, 25f);
            _fishCyclesBeforeWander = _rng.Next(1, 3);
        }

        /// <summary>Room gọi khi scene báo vùng đi lại + điểm câu.</summary>
        public void Configure(Rect walkArea, IReadOnlyList<Vector2> spots)
        {
            if (walkArea.width > 0.01f && walkArea.height > 0.01f) { _walkArea = walkArea; }
            if (spots != null && spots.Count > 0)
            {
                _spots.Clear();
                for (int i = 0; i < spots.Count; i++) { _spots.Add(spots[i]); }
            }
            // Bot đang đứng ngoài vùng mới → kéo về trong vùng.
            if (!_walkArea.Contains(new Vector2(State.x, State.y)))
            {
                Vector2 p = RandomPointInArea();
                State.x = p.x; State.y = p.y;
                _positionDirty = true;
            }
        }

        /// <summary>Room gọi khi người chơi vừa chat và chọn bot này đáp.</summary>
        public void ScheduleReply()
        {
            if (_replyIn < 0f) { _replyIn = RandRange(1f, 3f); }
        }

        /// <summary>Đặt bubble từ ngoài (room.SetBotBubble). Trả true = state đổi.</summary>
        public bool SetBubble(string text, long nowUnix)
        {
            State.bubbleText = text ?? string.Empty;
            State.bubbleUntilUnix = nowUnix + (long)Math.Ceiling(Mathf.Max(1f, _cfg.bubbleShowSeconds));
            return true;
        }

        /// <summary>Chạy 1 bước mô phỏng. Trả true nếu State đổi đáng kể và room nên bắn OnPlayerUpdated.</summary>
        public bool Tick(float dt, long nowUnix)
        {
            if (dt <= 0f) { return false; }
            bool dirty = _positionDirty;
            _positionDirty = false;

            // Bubble hết hạn → xoá chữ để view không vẽ lại.
            if (!string.IsNullOrEmpty(State.bubbleText) && nowUnix >= State.bubbleUntilUnix)
            {
                State.bubbleText = string.Empty;
                dirty = true;
            }

            // Chat
            if (_replyIn >= 0f)
            {
                _replyIn -= dt;
                if (_replyIn < 0f)
                {
                    _replyIn = -1f;
                    Say(ReplyLines[_rng.Next(ReplyLines.Length)], nowUnix);
                    dirty = true;
                }
            }
            _nextIdleChatIn -= dt;
            if (_nextIdleChatIn <= 0f)
            {
                _nextIdleChatIn = RandRange(12f, 25f);
                Say(IdleLines[_rng.Next(IdleLines.Length)], nowUnix);
                dirty = true;
            }

            // Máy trạng thái
            switch (_mode)
            {
                case Mode.Wander:
                case Mode.GoFish:
                    dirty |= TickMove(dt);
                    break;
                case Mode.Idle:
                    _modeTimer -= dt;
                    if (_modeTimer <= 0f) { dirty |= NextAfterIdle(); }
                    break;
                case Mode.Fishing:
                    dirty |= TickFishing(dt);
                    break;
            }

            // Vị trí: đẩy theo nhịp netPushRate.
            _pushAccumulator += dt;
            float pushInterval = 1f / Mathf.Max(1f, _cfg.netPushRate);
            if (State.moving && _pushAccumulator >= pushInterval)
            {
                _pushAccumulator = 0f;
                dirty = true;
            }

            if (dirty) { State.lastSeenUnix = nowUnix; }
            return dirty;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Trạng thái con
        // ─────────────────────────────────────────────────────────────────

        private bool NextAfterIdle()
        {
            if (_fishCyclesBeforeWander <= 0)
            {
                _fishCyclesBeforeWander = _rng.Next(1, 3);
                _target = RandomPointInArea();
                _mode = Mode.Wander;
            }
            else
            {
                _fishCyclesBeforeWander--;
                _target = NearestSpotWithJitter();
                _mode = Mode.GoFish;
            }
            return SetMoving(true);
        }

        private bool TickMove(float dt)
        {
            Vector2 pos = new Vector2(State.x, State.y);
            Vector2 delta = _target - pos;
            float speed = Mathf.Max(0.1f, _cfg.moveSpeed) * 0.8f;
            float step = speed * dt;
            bool changed = false;

            if (delta.magnitude <= step + 0.02f)
            {
                State.x = _target.x; State.y = _target.y;
                changed |= SetMoving(false);
                if (_mode == Mode.GoFish)
                {
                    _mode = Mode.Fishing;
                    changed |= EnterPhase(FishingPhase.Casting, Mathf.Max(0.1f, _cfg.castDurationSeconds));
                }
                else
                {
                    _mode = Mode.Idle;
                    _modeTimer = RandRange(2f, 5f);
                }
                return true;
            }

            Vector2 dirV = delta.normalized;
            State.x += dirV.x * step;
            State.y += dirV.y * step;
            int newDir = (int)FaceCardinal(dirV);
            if (newDir != State.dir) { State.dir = newDir; changed = true; }
            changed |= SetMoving(true);
            return changed;
        }

        private bool TickFishing(float dt)
        {
            _phaseTimer -= dt;
            if (_phaseTimer > 0f) { return false; }

            switch (_phase)
            {
                case FishingPhase.Casting:
                    {
                        Vector2 r = _cfg.biteWaitSecondsRange;
                        float wait = RandRange(Mathf.Min(r.x, r.y), Mathf.Max(r.x, r.y));
                        return EnterPhase(FishingPhase.Waiting, Mathf.Max(0.5f, wait));
                    }
                case FishingPhase.Waiting:
                    return EnterPhase(FishingPhase.Bite, Mathf.Max(0.2f, _cfg.biteWindowSeconds) * RandRange(0.3f, 1f));
                case FishingPhase.Bite:
                    return EnterPhase(FishingPhase.Reeling, Mathf.Max(0.1f, _cfg.reelDurationSeconds));
                case FishingPhase.Reeling:
                    return EnterPhase(FishingPhase.Result, Mathf.Max(0.1f, _cfg.resultShowSeconds));
                case FishingPhase.Result:
                default:
                    {
                        bool changed = EnterPhase(FishingPhase.Idle, 0f);
                        _mode = Mode.Idle;
                        _modeTimer = RandRange(2f, 5f);
                        return changed;
                    }
            }
        }

        private bool EnterPhase(FishingPhase phase, float duration)
        {
            _phase = phase;
            _phaseTimer = duration;
            int p = (int)phase;
            if (State.phase == p) { return false; }
            State.phase = p;
            return true;
        }

        private bool SetMoving(bool moving)
        {
            if (State.moving == moving) { return false; }
            State.moving = moving;
            return true;
        }

        private void Say(string text, long nowUnix)
        {
            SetBubble(text, nowUnix);
            if (_chatSink != null) { _chatSink(State.playerId, text); }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Trục mạnh hơn thắng (giống TouristAgent.FaceCardinal), không flipX.</summary>
        private static FacingDir FaceCardinal(Vector2 v)
        {
            if (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) { return v.x < 0f ? FacingDir.Left : FacingDir.Right; }
            return v.y < 0f ? FacingDir.Down : FacingDir.Up;
        }

        private Vector2 RandomPointInArea()
        {
            return new Vector2(
                RandRange(_walkArea.xMin, _walkArea.xMax),
                RandRange(_walkArea.yMin, _walkArea.yMax));
        }

        private Vector2 NearestSpotWithJitter()
        {
            Vector2 pos = new Vector2(State.x, State.y);
            Vector2 best = _spots[0];
            float bestD = float.MaxValue;
            for (int i = 0; i < _spots.Count; i++)
            {
                float d = (_spots[i] - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = _spots[i]; }
            }
            // Lệch nhẹ để nhiều bot không đứng chồng nhau, nhưng vẫn trong walkArea.
            Vector2 j = best + new Vector2(RandRange(-0.6f, 0.6f), RandRange(-0.3f, 0.3f));
            j.x = Mathf.Clamp(j.x, _walkArea.xMin, _walkArea.xMax);
            j.y = Mathf.Clamp(j.y, _walkArea.yMin, _walkArea.yMax);
            return j;
        }

        private float RandRange(float min, float max)
        {
            return min + (float)_rng.NextDouble() * (max - min);
        }
    }
}
