# Kiem tra popup tren dien thoai (2026-09-25 12:34)
Che do: EDIT (popup sinh tu prefab luc chay chua co trong danh sach).

- Bo qua popup cu khong dung: Canvas_Popup/LevelUpPopup/ContentPanel

Tong: 28 bang popup, 16 bang TRAN tren it nhat 1 may, trong do 0 bang CHUA co tu co.

| Popup | Tu co | Tran lon nhat (dv canvas) | May bi tran |
|---|---|---|---|
| Canvas_MarketPopup/Panel_Dim/Popup_Board | co | 125 | Android 20:9 (2400x1080): tren 40; iPhone tai tho (2532x1170): tren 47; iPhone SE 16:9 (1334x750): trai 3, phai 16; iPad 4:3 (2048x1536): trai 113, phai 125; Tablet 16:10 (2560x1600): trai 34, phai 47 |
| Canvas_Popup/WarehousePopup/Panel_Dim/Board_Border | co | 119 | Android 20:9 (2400x1080): duoi 51; iPhone tai tho (2532x1170): duoi 58; iPhone SE 16:9 (1334x750): duoi 2, trai 9; iPad 4:3 (2048x1536): trai 119, phai 56; Tablet 16:10 (2560x1600): trai 40 |
| Canvas_Popup/MillPopup_Root/PopupRoot/Window | co | 104 | PC 16:9 (1920x1080): tren 23, duoi 24; Android 20:9 (2400x1080): tren 95, duoi 96; iPhone tai tho (2532x1170): tren 102, duoi 104; iPhone SE 16:9 (1334x750): tren 46, duoi 48; iPad 4:3 (2048x1536): trai 43, phai 50 |
| Canvas_Popup/Popup_item_Train | co | 104 | iPad 4:3 (2048x1536): trai 104; Tablet 16:10 (2560x1600): trai 25 |
| Canvas_Popup/Popup_seed | co | 88 | iPad 4:3 (2048x1536): trai 7, phai 88; Tablet 16:10 (2560x1600): phai 9 |
| Canvas_Popup/Popup_hoa | co | 88 | iPad 4:3 (2048x1536): trai 7, phai 88; Tablet 16:10 (2560x1600): phai 9 |
| Canvas_Popup/popup_Menu/Board_Border | co | 47 | Android 20:9 (2400x1080): duoi 40; iPhone tai tho (2532x1170): duoi 47 |
| Canvas_Popup/WarehousePopup/Panel_Dim/Left_Container/Inner_GridBox | co | 45 | iPad 4:3 (2048x1536): trai 45 |
| Canvas_StallPopup/Panel_Dim/Popup_Main/Board_Border | co | 37 | Android 20:9 (2400x1080): duoi 30; iPhone tai tho (2532x1170): tren 2, duoi 37 |
| Canvas_Popup/popup_Menu/Board_Fill_Bottom | co | 37 | Android 20:9 (2400x1080): duoi 30; iPhone tai tho (2532x1170): duoi 37 |
| Canvas_Popup/popup_Menu/Board_Fill_Top | co | 37 | Android 20:9 (2400x1080): duoi 30; iPhone tai tho (2532x1170): duoi 37 |
| Canvas_Popup/Popup_Train_MasterStation/Main_Frame | co | 32 | Android 20:9 (2400x1080): tren 25; iPhone tai tho (2532x1170): tren 32 |
| Canvas_Popup/WarehousePopup/Panel_Dim/Board_Fill_Bottom | co | 27 | Android 20:9 (2400x1080): duoi 20; iPhone tai tho (2532x1170): duoi 27 |
| Canvas_Popup/WarehousePopup/Panel_Dim/Board_Fill_Top | co | 27 | Android 20:9 (2400x1080): duoi 20; iPhone tai tho (2532x1170): duoi 27 |
| Canvas_Popup/popup_Menu/Inner_PaperContainer | co | 12 | Android 20:9 (2400x1080): duoi 5; iPhone tai tho (2532x1170): duoi 12 |
| Canvas_Popup/WarehousePopup/Panel_Dim/Right_DetailPanel | co | 2 | iPhone tai tho (2532x1170): duoi 2 |
| Canvas_Popup/Popup_Settings/Board | KHONG | - | vua het |
| Canvas_Popup/Popup_AvatarProfile/Board_Wooden | KHONG | - | vua het |
| Canvas_TouristBoatPopup/TouristBoatPopups/BoatAnnouncePopup/Root/Card | KHONG | - | vua het |
| Canvas_Popup/Popup_WarehouseMissingItems | KHONG | - | vua het |
| Canvas_OrderBoardPopup/Panel_Dim/Popup_Main/Panel_TicketArea | co | - | vua het |
| Canvas_Popup/Popup_WarehouseTransferItem | KHONG | - | vua het |
| Canvas_OrderBoardPopup/Panel_Dim/Popup_Main/Board_Border | co | - | vua het |
| Canvas_TouristBoatPopup/TouristBoatPopups/DockPurchasePopup/Root/Card | KHONG | - | vua het |
| Canvas_OrderBoardPopup/Panel_Dim/Popup_Main/Col_Detail | co | - | vua het |
| Canvas_Popup/Popup_WarehouseUpgrade | KHONG | - | vua het |

Nam NGOAI man hinh ngay ca tren PC (panel truot vao / dang an) -> khong danh gia, bam Play MO popup do roi chay lai muc 1:
- Canvas_StallPopup/Panel_Dim/Popup_Main/Picker_Root/Picker_Panel/Col_Items
- Canvas_StallPopup/Panel_Dim/Popup_Main/Picker_Root/Picker_Panel/Col_Setup

Bang 'Tu co = co' se tu dich / co lai luc mo -> khong can lam gi. Bang KHONG + tran: chay muc 2 (gan PopupTuCo) roi Ctrl+S.
