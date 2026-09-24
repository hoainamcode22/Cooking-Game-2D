# Don dep tool + backup (2026-09-24)

Chua xoa gi. Danh sach de Sep duyet. Tool Editor KHONG vao ban build, xoa chi cho gon menu va source.

## A. XOA NGAY - tool TU CHAY NGAM (nguy hiem)
| File | Vi sao |
|---|---|
| CozyKitchenV3AutoRunner.cs | Moi lan MO LAI Unity (dau moc nam trong Temp/ bi xoa) tu chay: sua prefab the nguyen lieu + thanh don "Tomato Pasta" trong bep -> de len bo cuc Sep chinh tay |
| CustomUserArtProcessTool.cs | Moi lan compile tu chay ProcessUserArt() (xu ly lai anh) -> cham, co the ghi de anh |
| FixMobileIconsAndShopSizeTool.cs | Moi lan compile tu sua import texture + data shop |
| ProcessFeedMillSpriteTool.cs | Tu chay khi co file jpg nguon; da xu ly xong may thuc an |

## B. XOA DUOC - sua 1 lan, da xong, khong file nao goi toi
SuaFontTMP_2026_08_29, PhucHoiFontTMP_2026_08_29, ToiUuTexture_2026_08_29, DenBuNguyenLieuMat_2026_08_27,
Vong24FixTool, FixMarketLayerTool + FixMarketAlphaTool (xoa ca 2), FixHUDLayoutWindow, RebuildHUD, HUDGenerator,
GeneratePerfectHUD, AutoWireHUD, CreateEXPBarUI (HUD cu, da thay bang TownshipHUDBuilderTool), MillRebuildBatch,
AutoScreenshotTool, Phase1TestTool + NapTienTestTool + MillTestTool (xoa ca 3), WarehousePopupUIHierarchyBuilder,
SceneReloadTool, DonRacUIGocSceneTool, DonMissingScriptTool, KitchenVaPhanThieuTool, JuicyVFXSetupTool,
GemCostTextFixTool, TutorialStepTextFixTool, DecorStageRepairTool, FinalizeDecor5Tool, StudioRefurbishMasterTool,
HouseAssetExtractorTool, MillAssetHandoff, IngestGeneratedArtTool, KitchenFreezeFromPlayTool,
KitchenFreezeToHierarchyTool, CozyKitchenUISetupTool, CozyKitchenPixelPerfectSetupTool,
EditModeLuoiTool (tool 2x2 hong - da hoan tac xong).

## C. GIU - dang dung / co ich
- Tools/Kitchen V3 (KitchenV3BepTool: 12 lua lo, 15 dia trinh bay, 16 chu chi tiet), KitchenV3BuilderTool, KitchenV3LayoutFixTool (chi chay 1 lan)
- Tools/VFX (VfxToolMenu), Tools/Khach du lich (TouristOrderBubbleTool), Tools/Kho (KhoTabTatCaTool)
- Tools/Edit Mode (EditModeODat1x1Tool: 3 o dat, 4 hoan tac, 5 so o nha)
- Tourist Boat: TouristBoatDiagnosticTool (6 chan doan, 7 test ngay, 8 xoa save tau), TouristBoatOneClickSetup, TouristBoatSetupTool
- Save: FarmResetTool, SaveDebugTool, FarmSaveCleanupTool, ChoiLaiTuDauTool
- Build: GameBuildTool, ReleaseBuildSetupTool, ExportWebGLForItchTool, SpriteAtlasBuilderTool, TextureQualityRestoreTool, RenderSettingsOptimizerTool, MobileAssetOptimizationTool
- GameProgressionStudioWindow (Studio cap 1-30), MissingScriptScanTool, UnusedAssetAuditTool
- Moi file *SpriteFactory / *SpriteGenerator / *Builder dang duoc tool khac goi -> giu

## D. BACKUP (ngoai Assets, Unity khong doc, tong ~260 MB)
| Thu muc | Nen |
|---|---|
| _Backup_Popup_20260923_063745 (17M), _Backup_Stall_Market_20260923_083835 (33M), _Backup_Kitchen_Layout_20260923_104922 (7.6M), _Backup_Kho_Bep_20260923_132858 (33M), _Backup_Scene_TruocKhiVa (42M) | Xoa (tu 23/09, da on) |
| _Backup_TouristBubble_VFX_..062822, _Backup_VFX_Farm_UI_Env_..065235, _Backup_Buom_Chim_..071624, _Backup_Bep_Combo_..075457, _Backup_Bep_CongThuc_..084352, _Backup_DiaTrinhBay_..095220 | Xoa (sang 24/09, da test) |
| _Backup_EditModeTool_20260924_191416 (16M) | Xoa (da hoan tac xong) |
| _Backup_ODat1x1_20260924_211410 (16M) | Xoa (trung voi ban 211430) |
| _Backup_ODat1x1_20260924_203641 (16M) | GIU tam: o dat NGUYEN GOC truoc tool 3 |
| _Backup_ODat1x1_20260924_211430 (16M) | GIU tam: truoc tool 5 (so o nha) |
| _Backup_Farm_EditMode_20260924_113819 (21M) | GIU tam: code goc cua cac file sua hom nay |
| _to_delete (204K), bash.exe.stackdump | Xoa |
3 ban GIU TAM: xoa sau khi Sep choi thu thay on.
