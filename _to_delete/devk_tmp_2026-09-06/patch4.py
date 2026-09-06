# -*- coding: utf-8 -*-
import hashlib,sys
P="Assets/_Game/Farm/Scripts/UI/LevelUpPopupUI.cs"
t=open(P,'rb').read().decode('utf-8')
def crlf(s): return s.replace("\r\n","\n").replace("\n","\r\n")
def rep(old,new,tag):
    global t
    o,n=crlf(old),crlf(new)
    if t.count(o)!=1:
        print("FAIL %s count=%d"%(tag,t.count(o))); sys.exit(1)
    t=t.replace(o,n,1)

# 1) Tra MERGED_ROW_GAP ve 10f, thay bang CHUA CHO BANG CHU trong chieu cao hang
rep(u"""    // [V7 — 2026-09-06] 10f → 62f. Flow-layout tính chiều cao hàng CHỈ bằng chiều cao ô
    // (sizes[i].y = 190), KHÔNG kể bảng chữ treo dưới ô. Bảng chữ nay cao
    // CAPTION_GAP_Y(4) + CAPTION_H(52) = 56px, nên khe 10px cũ khiến nhãn hàng trên đâm
    // thẳng vào ô hàng dưới. 62 > 56 → hết chồng. Chỉ ảnh hưởng khi dải phải xuống 2 hàng;
    // ca 1 hàng (ảnh Sếp chụp: 10 ô một hàng) không đổi gì.
    private const float MERGED_ROW_GAP   = 62f;   // khoảng cách dọc giữa 2 hàng (chừa bảng chữ)
""",
u"""    private const float MERGED_ROW_GAP   = 10f;   // khoảng cách dọc giữa 2 hàng

    // ═════════════════════════════════════════════════════════════════════════
    // [V7 — 2026-09-06] CHỪA CHỖ CHO BẢNG CHỮ DƯỚI Ô
    // ─────────────────────────────────────────────────────────────────────────
    //  Flow-layout trước nay tính chiều cao một hàng CHỈ bằng chiều cao ô (190), KHÔNG kể
    //  bảng chữ treo bên dưới. Hậu quả đo được:
    //
    //   • Dải trắng Dai_MoKhoa cao STRIP_H = 250 và Viewport có RectMask2D. Ô 190 nằm giữa
    //     → mép dưới ô ở y = −95, đáy mask ở y = −125, tức CHỈ CÒN 30px cho chữ. Bảng chữ
    //     cũ cao 26px vừa khít 30px đó (nên chữ hiện được), nhưng chỉ cần cao hơn một chút
    //     là bị mask XÉN NGANG.
    //   • Khi dải xuống 2 hàng, chữ hàng trên đâm thẳng vào ô hàng dưới (khe chỉ 10px).
    //
    //  Cách sửa: cộng chiều cao bảng chữ vào chiều cao HÀNG (chỉ để tính chỗ, không đụng
    //  sizeDelta của ô), rồi đẩy ô lên nửa bảng chữ. Thế là cụm được canh giữa theo ĐÚNG
    //  chiều cao thật (ô + chữ): chữ không bao giờ lọt ra ngoài mask, hai hàng không đụng
    //  nhau, và nếu chật thì k tự co — thay vì âm thầm xén mất chữ.
    //
    //  Phải KHỚP với UnlockSlotUI: CAPTION_GAP_Y(4) + CAPTION_H(52) = 56.
    // ═════════════════════════════════════════════════════════════════════════
    private const float MERGED_CAPTION_BAND = 56f;
""","rowgap")

# 2) Cong bang chu vao chieu cao hang - o mo khoa cua scene
rep(u"""            if (sz.x < 1f || sz.y < 1f) sz = new Vector2(190f, 190f);   // phòng hờ, chuẩn tool = 190
            rts.Add(rt); sizes.Add(sz); unlockOf.Add(s);
""",
u"""            if (sz.x < 1f || sz.y < 1f) sz = new Vector2(190f, 190f);   // phòng hờ, chuẩn tool = 190
            sz.y += MERGED_CAPTION_BAND;   // [V7] chừa chỗ bảng chữ, xem chú thích ở hằng số
            rts.Add(rt); sizes.Add(sz); unlockOf.Add(s);
""","size1")

# 3) ... va o qua dung runtime
rep(u"""            if (sz.x < 1f || sz.y < 1f) sz = new Vector2(190f, 190f);   // chuẩn đồng bộ 190x190
            rts.Add(rt); sizes.Add(sz); unlockOf.Add(c);   // [B4] ô runtime cũng là UnlockSlotUI → co qua SetBaseScale
""",
u"""            if (sz.x < 1f || sz.y < 1f) sz = new Vector2(190f, 190f);   // chuẩn đồng bộ 190x190
            sz.y += MERGED_CAPTION_BAND;   // [V7] chừa chỗ bảng chữ, xem chú thích ở hằng số
            rts.Add(rt); sizes.Add(sz); unlockOf.Add(c);   // [B4] ô runtime cũng là UnlockSlotUI → co qua SetBaseScale
""","size2")

# 4) Day o len nua bang chu (bang chu nam DUOI o nen phai lech tam len)
rep(u"""                rt.anchoredPosition = new Vector2(x + w * 0.5f, centerY);
""",
u"""                // [V7] Hàng nay cao (ô + bảng chữ); bảng chữ nằm DƯỚI ô nên ô phải lệch
                // LÊN nửa bảng chữ, chừa đúng phần dưới cho chữ.
                rt.anchoredPosition = new Vector2(x + w * 0.5f, centerY + MERGED_CAPTION_BAND * 0.5f * k);
""","place")

o=t.encode('utf-8'); open(P,'wb').write(o)
print("WROTE md5=%s crlf=%d lf=%d"%(hashlib.md5(o).hexdigest(),o.count(b'\r\n'),o.count(b'\n')))
