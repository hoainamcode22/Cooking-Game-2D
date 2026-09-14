// =============================================================================
//  GameAnalytics.jslib  —  cau noi Unity WebGL  ->  Google Analytics 4 / Firebase JS
// -----------------------------------------------------------------------------
//  Vi sao file nay ton tai:
//    Firebase Unity SDK KHONG ho tro WebGL (chi Android/iOS). Cach duy nhat de ban
//    telemetry tu ban WebGL la goi thang sang JavaScript cua trang web dang host game.
//    Trang web (WebGLTemplates/FarmAnalytics/index.html) nap gtag.js va dinh nghia
//    window.__gameAnalytics; file .jslib nay chi la lop mong goi vao do.
//
//  NGUYEN TAC AN TOAN (rat quan trong):
//    - MOI ham bao trong try/catch. Mot exception nem nguoc vao Unity se lam
//      abort() ca WebAssembly runtime => nguoi choi thay man hinh trang.
//    - Neu gtag / firebase khong ton tai (ad-blocker, uBlock, Brave shield, mang
//      cong ty chan googletagmanager.com) thi im lang bo qua. Game van chay binh thuong.
// =============================================================================

mergeInto(LibraryManager.library, {

  // ---------------------------------------------------------------------------
  // Tra ve 1 neu trang web co san he thong analytics, 0 neu khong.
  // C# dung ket qua nay de biet co nen log hay khong (va de hien trong debug).
  // ---------------------------------------------------------------------------
  AnalyticsJsReady: function () {
    try {
      if (typeof window === 'undefined') { return 0; }
      var ga = window.__gameAnalytics;
      if (ga && typeof ga.ready === 'function') {
        return ga.ready() ? 1 : 0;
      }
      if (typeof window.gtag === 'function') { return 1; }
      if (window.firebase && window.firebase.analytics) { return 1; }
      return 0;
    } catch (e) {
      return 0;
    }
  },

  // ---------------------------------------------------------------------------
  // Ban 1 event.
  //   namePtr       : con tro chuoi UTF8 - ten event da duoc C# lam sach theo luat GA4
  //   jsonParamsPtr : con tro chuoi UTF8 - JSON object cac tham so, vd {"level":7}
  //                   Co the la chuoi rong hoac "{}" neu khong co tham so.
  // ---------------------------------------------------------------------------
  AnalyticsJsLogEvent: function (namePtr, jsonParamsPtr) {
    try {
      var name = UTF8ToString(namePtr);
      if (!name) { return; }

      var raw = jsonParamsPtr ? UTF8ToString(jsonParamsPtr) : '';
      var params = {};
      if (raw && raw.length > 1) {
        try {
          var parsed = JSON.parse(raw);
          if (parsed && typeof parsed === 'object') { params = parsed; }
        } catch (parseErr) {
          // JSON hong thi van ban event khong tham so, con hon mat luon event.
          params = {};
        }
      }

      if (typeof window === 'undefined') { return; }

      var ga = window.__gameAnalytics;
      if (ga && typeof ga.logEvent === 'function') {
        ga.logEvent(name, params);
        return;
      }

      // Du phong: trang web khong dung template cua game (vd host tu che)
      // nhung van co gtag toan cuc.
      if (typeof window.gtag === 'function') {
        window.gtag('event', name, params);
        return;
      }

      // Khong co gi ca -> im lang. KHONG throw.
    } catch (e) {
      // Nuot loi. Analytics chet thi game van phai song.
    }
  },

  // ---------------------------------------------------------------------------
  // Dat user property (vd: "player_level" = "12"). Hien trong GA4 > Audiences.
  // ---------------------------------------------------------------------------
  AnalyticsJsSetUserProp: function (keyPtr, valPtr) {
    try {
      var key = UTF8ToString(keyPtr);
      if (!key) { return; }
      var val = valPtr ? UTF8ToString(valPtr) : '';

      if (typeof window === 'undefined') { return; }

      var ga = window.__gameAnalytics;
      if (ga && typeof ga.setUserProperty === 'function') {
        ga.setUserProperty(key, val);
        return;
      }

      if (typeof window.gtag === 'function') {
        var props = {};
        props[key] = val;
        window.gtag('set', 'user_properties', props);
      }
    } catch (e) {
      // Im lang.
    }
  }

});
