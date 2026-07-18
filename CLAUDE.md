# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proje Nedir

XmlToKtb, karayolu/demiryolu güzergah (yol ekseni) mühendisliği verileri arasında dönüşüm yapan, tek formlu bir
Windows Forms masaüstü uygulamasıdır. Girdi/çıktı formatları LandXML, NetCAD'in KTB formatı, Leica GSI (ALN/PRF),
DXF, düz metin nokta/kilometre listeleri ve TCDD'ye özgü teknik tablolardır. Ortadaki ortak veri modeli her zaman
`LandXML` sınıfıdır: hemen her özellik önce bir girdiyi `LandXML`'e çevirir, sonra `LandXML`'den istenen çıktı
formatını üretir.

## Çözüm Yapısı

3 projeden oluşan bir .NET Framework 4.8 çözümü (`XmlToKtb.sln`):

- **XmlToKtb** (WinForms, `OutputType=WinExe`) — UI ve tüm dönüşüm/hesaplama mantığı burada yaşıyor. Giriş noktası
  `Program.cs`, tek form `XmlToKtbUI.cs` (+ `.Designer.cs`).
- **Common** (Library) — Paylaşılan veri modeli: `LandXML.cs` (LandXML-1.2 şemasına karşılık gelen, elle
  sadeleştirilmiş serileştirme sınıfı), `DosyaDeserializer.cs` (generic `XmlSerializer` sarmalayıcı), `Modeller/`
  altında `Nokta`, `Ucgen`, `Spiral`, `NivelmanNokta` gibi basit veri sınıfları, `Enum/` altında `DosyaTip` (desteklenen
  dosya türleri) ve `DurumTip` (UI akış durumu).
  - ⚠️ `LandXML-1_0.cs` (23k+ satır) bu projede duruyor ama **derlemeye dahil değil** (`Common.csproj` içinde
    `<Compile Include>` yok) — `xsd.exe` ile üretilmiş eski/tam LandXML-1.0 şema sınıfı, referans amaçlı duruyor,
    canlı kod değil. Kafa karıştırmasın diye bilerek kullanılmıyor.
- **DXFLib** — El yazımı, bağımsız bir DXF parser/writer kütüphanesi (`DXFDocument`, `DXFEntity`, `DXFPolyLine`,
  `DXF3DFace` vb.). Not: proje ayrıca NuGet üzerinden **netDxf** paketini de kullanıyor (bkz. `packages/netDxf.*`,
  `using netDxf;`) — bazı DXF yazma işlemleri netDxf ile, bazı okuma/parse işlemleri kendi DXFLib'i ile yapılıyor.

## Build / Çalıştırma

- Klasik MSBuild/.NET Framework projesi; `dotnet build` çalışmaz (WinForms + net48 + eski csproj formatı).
  Visual Studio ile açıp Build almak ya da Developer Command Prompt'tan `msbuild XmlToKtb.sln` ile derlemek gerekir.
- Çalıştırmak için: Release modda derlenip üretilen `.exe` doğrudan çalıştırılır. Hedef makinede .NET Framework
  4.8 (README'de 4.6.1 yazsa da `.csproj` dosyaları artık `v4.8` hedefliyor) kurulu olmalı.
- Otomatik test projesi yok (README: "Test için bir proje yazılacak"). Doğrulama şu an manuel: bir örnek dosya
  seçip UI üzerinden dönüştürüp çıktıyı gözden geçirmek.

## UI Akışı ve Sekmeler (`XmlToKtbUI.cs`)

Form, her biri bağımsız bir dönüşüm/hesaplama hattını temsil eden sekmelerden oluşuyor. Genel desen her buton için
aynı: `OpenFileDialog` ile girdi(ler) seçilir → bir `*Icerigi`/`*XML`/`*Hesapla` statik yardımcı sınıfı çağrılarak
`LandXML` (ve gerekirse yardımcı nokta/km listeleri) işlenir → sonuç string `SaveFileDialog` ile diske yazılır
(`DosyaKaydetFiltreIle` / `AlnPrfDosyasiniKaydet`).

- **Ana Dönüştürme sekmesi** — `dosyaTuruComboBox` ile hedef format seçilir (`DosyaTip`: KTB, ALNPRF, TBL, XML...).
  Girdi `.ktb` ise `KTBtoXML` ile önce LandXML'e çevrilir; `.xml` ise doğrudan deserialize edilir. Çıktı türüne göre
  `KTBIcerigi`, `ALNIcerigi` + `PrfIcerigi`, ya da `LISIcerigi` çağrılır.
- **Enkesit/Gabari sekmesi** — `KSEIcerigi` üzerinden düz/dairesel enkesit (cross-section), DXF tip kesitten enkesit,
  gabari (açıklık/clearance) kesitleri üretir; girdi olarak LandXML + NCN (nokta) + km listesi dosyaları kullanır.
- **Tablo sekmesi** — `GzgIcerigi` (güzergah tablosu) ve `TcddteknikIcerigi` (TCDD teknik tablo) ile rapor tabloları
  üretir.
- **DXF sekmesi** — `dxftoXML_click`: NetCAD'den DXF olarak kaydedilmiş üçgen yüzey modelini (3DPolyline/3DFACE)
  okuyup `YuzeyXML` ile XML yüzey dosyasına çevirir (kendi `DXFLib`'i ile parse eder). `JsontoDXF_click`: Parsel
  Sorgu'dan gelen GeoJSON benzeri parsel/ada verisini (`Json.cs` — `RootObject`/`Feature` modelleri) okuyup netDxf
  ile poligon+etiket DXF'i üretir.
- **Ofset/Kot sekmesi** — `KmdenKoorHesapla` (km'den koordinat hesabı) ve `OfsetVeKoticerigi` (ofset ve kot hesabı),
  güzergaha göre nokta ofset/kot hesaplamaları yapar.
- **Nivelman sekmesi** — `MDT_icerigi`: Leica DNA03/DNA10 nivo cihazlarının `.MDT` kayıt dosyalarını Excel'e
  yapıştırılabilir listeye çevirir. `GSIoku_Click` henüz tamamlanmamış (TODO yorumu var).
- **Dever sekmesi** — `Dever.cs`: dever (yol eğim/superelevation) hesap/gösterimi. `Geodosyasi_olusturma`:
  `GeoCevir` ile LandXML'den `.geo` güzergah dosyası üretir.

## Türkçe Alan Terminolojisi (kod boyunca aynen kullanılıyor)

Kod tabanı tamamen Türkçe isimlendirilmiş ve karayolu/demiryolu jargonu içeriyor. Bunları çözmeden ilgili sınıfları
anlamak zor:

| Terim | Anlamı |
|---|---|
| Güzergah | Yol/demiryolu ekseni (route/alignment) |
| KTB | NetCAD'in yol ekseni (line/curve/spiral) dosya formatı |
| ALN / PRF | Leica GSI ekseni (ALN) ve profil (PRF) dosyaları |
| Enkesit / Kesit | Cross-section |
| Gabari | Açıklık/clearance kesiti |
| Dever | Superelevation (viraj enine eğimi) |
| Nivelman | Leveling (yükseklik/kot ölçümü) |
| MDT / GSI | Leica nivo cihazı ham kayıt dosyası formatları |
| NCN | Düz metin nokta/koordinat listesi dosyası |
| Ofset / Kot | Offset / elevation |
| Km (kilometre) | Güzergah üzerindeki zincirleme (chainage) konumu |
| TBL / LIS | Rapor amaçlı düz metin tablo çıktısı |
| GEO | Üretilen güzergah tanım dosyası |

## Bilinmesi Gereken Kırılganlıklar

- `dosyaAdi` ve `rootPath` (`XmlToKtbUI.cs`) `static` alanlar — form tek instance çalıştığı için sorun çıkarmıyor
  ama yeni kod eklerken bu paylaşılan durağan duruma dikkat edin.
- Dosya türü ayrımı genelde uzantıya (`Path.GetExtension` + `ToLower`) bakılarak yapılıyor; yeni bir format
  eklerken aynı switch/if desenini (`DosyaTip` enum + `GetirLandXmlDosyaTipiIle` gibi yardımcı metotlar) takip edin.
- Aynı isimli iki `DXFDocument` sınıfı var: `DXFLib.DXFDocument` (özel parser) ve `netDxf.DxfDocument` (NuGet
  paketi, ayrıca `XmlToKtb/Dxf/DxfDocument.cs` ve `Common/Dxf/DxfDocument.cs` içinde de sarmalayıcılar mevcut) —
  hangi DXF sınıfının kullanıldığına dikkat edin, `using` ifadelerinden ayırt edin.
