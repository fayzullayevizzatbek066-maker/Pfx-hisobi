# PFX Manager

Windows kompyuterda mavjud `.pfx` va `.p12` elektron sertifikat/kalit fayllarini avtomatik
topish, sertifikat ma'lumotlarini tahlil qilish, amal qilish muddati bo'yicha saralash,
dublikatlarni aniqlash va muddati o'tgan kalitlarni xavfsiz ommaviy boshqarish uchun
production-ready Windows desktop dastur.

Interfeys tili: **o'zbekcha (lotin yozuvi)** — barcha matnlar `PFXManager.App/Resources/Strings.cs`
ichida markazlashtirilgan, kelajakda rus/ingliz tillarini qo'shish shu bitta joyni almashtirish
bilan cheklanadi.

## 1. Tizim talablari

- Windows 10 x64 yoki Windows 11 x64
- Ishga tushirish uchun: hech narsa o'rnatish shart emas — installer/publish self-contained
  (.NET runtime ilova ichida keladi)
- Qurish (build) uchun: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) va Windows
  (WPF/XAML kompilyatori faqat Windows'da ishlaydi — pastga qarang)

## 2. Texnologiya tanlovi: WPF vs WinUI 3

Loyiha boshida ikkalasi ham solishtirildi:

| Mezon | WPF | WinUI 3 |
|---|---|---|
| Windows 10/11 muvofiqligi | To'liq, .NET 8 orqali barqaror | Windows 10 1809+ da ishlaydi, lekin ba'zi API'lar cheklangan |
| Barqarorlik / yetuklik | 15+ yillik, keng ishlatilgan, muammolar yaxshi hujjatlangan | Nisbatan yangi, ba'zi komponentlarda kutilmagan xatti-harakatlar |
| Deployment / installer | Oddiy: self-contained EXE + Inno Setup/MSI | MSIX talab qiladi (murakkabroq, ba'zi enterprise muhitlarda muammoli) |
| Windows API bilan integratsiya | `System.Security.Cryptography.X509Certificates`, `X509Store`, Explorer integratsiyasi — muammosiz | Xuddi shunday ishlaydi, lekin ba'zi Win32 interoplar qo'shimcha paket talab qiladi |
| DataGrid / jadval imkoniyatlari | `DataGrid` — sorting, virtualization, ko'p ustunli jadval uchun tayyor va yetuk | `DataGrid` community paket sifatida, kamroq yetuk |
| Uzoq muddatli qo'llab-quvvatlash | Microsoft tomonidan .NET bilan birga rasmiy LTS | Loyihaga qarab o'zgaruvchan release sikli |

**Qaror: WPF (.NET 8, self-contained, win-x64)** — sertifikat boshqaruv dasturi uchun kerak
bo'ladigan barqaror `DataGrid`, oson MSI/EXE deployment va Windows kripto API'lari bilan
uzoq muddatli, ishonchli integratsiya WinUI 3'dan ustun turadi. MVVM (CommunityToolkit.Mvvm)
va Dependency Injection (`Microsoft.Extensions.Hosting`) bilan.

## 3. Arxitektura

```
PFXManager.sln
├── src/
│   ├── PFXManager.App             WPF UI: Views, ViewModels (MVVM), dialogs, resources
│   ├── PFXManager.Core            Domain modellar, enum'lar, interfeyslar, biznes qoidalar
│   │                              (status engine, duplicate detection, bulk-selection rule)
│   └── PFXManager.Infrastructure  Filesystem scanner, X509 parser, STIR/PINFL extraction,
│                                  SQLite persistence + migratsiyalar, quarantine/restore,
│                                  Windows Certificate Store, scan orchestrator
└── tests/
    └── PFXManager.Tests           xUnit: status, duplicate detection, bulk selection,
                                   path/error handling, parser, quarantine (real temp fayllar
                                   va real SQLite bilan, hech qachon foydalanuvchi fayllari bilan emas)
```

`PFXManager.Core` va `PFXManager.Infrastructure` — **platformadan mustaqil** (`net8.0`),
Linux/macOS'da ham build va test qilinadi. Faqat `PFXManager.App` Windows-only (`net8.0-windows`,
WPF), chunki XAML kompilyatori (`PresentationBuildTasks`) faqat Windows'da ishlaydi.

Muhim arxitektura qoidasi (talab #26): diskdagi `.pfx` fayl va Windows Certificate Store'dagi
o'rnatilgan sertifikat **hech qachon aralashtirilmaydi**. `ICertificateRecordRepository`
(fayllar) va `IWindowsCertificateStoreService` (do'kon) butunlay alohida servislar; birini
o'chirish ikkinchisiga umuman ta'sir qilmaydi.

## 4. Build

```bash
git clone <repo>
cd Pfx-hisobi
dotnet restore PFXManager.sln
dotnet build PFXManager.sln -c Debug
```

> **Muhim:** `PFXManager.App` loyihasini build qilish faqat Windows'da ishlaydi (WPF SDK
> talab qiladi). Faqat `PFXManager.Core` / `PFXManager.Infrastructure` / `PFXManager.Tests`ni
> build qilish istalgan platformada ishlaydi:
> ```bash
> dotnet build tests/PFXManager.Tests/PFXManager.Tests.csproj -c Debug
> ```

## 5. Ishga tushirish (development)

Windows'da:

```powershell
dotnet run --project src\PFXManager.App\PFXManager.App.csproj
```

## 6. Testlar

```bash
dotnet test tests/PFXManager.Tests/PFXManager.Tests.csproj
```

31 ta unit/integration test: `CertificateStatusTests`, `DuplicateDetectionTests`,
`BulkSelectionTests`, `PathHandlingTests`, `ParserTests`, `QuarantineTests`. Testlar hech qachon
haqiqiy foydalanuvchi PFX fayllarini ishlatmaydi — har biri o'z temp papkasida `RSA.Create()`
orqali generatsiya qilingan, tashlab yuboriladigan (throwaway) sertifikatlar bilan ishlaydi
(`TestSupport/TestCertificateFactory.cs`), va `QuarantineTests` haqiqiy vaqtinchalik SQLite
baza fayliga qarshi ishlaydi.

## 7. Release build va installer yaratish

Windows'da (yoki loyihaning `.github/workflows/build.yml` CI orqali avtomatik):

```powershell
# 1. Self-contained, win-x64 nashr (runtime alohida o'rnatish shart emas)
dotnet publish src\PFXManager.App\PFXManager.App.csproj `
    -c Release -r win-x64 --self-contained true `
    -o publish\PFXManager

# 2. Installer (Inno Setup 6 o'rnatilgan bo'lishi kerak: https://jrsoftware.org/isinfo.php)
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\PFXManager.iss
```

Natija: `dist\PFXManager-Setup.exe`. Installer Start Menu yorlig'ini har doim, Ish stoli
yorlig'ini ixtiyoriy ravishda yaratadi va standart `Uninstall` yozuvini qo'shadi.

GitHub Actions workflow (`.github/workflows/build.yml`) har push'da: (1) Core/Infrastructure/
Tests'ni Linux'da tekshiradi, (2) to'liq solution'ni Windows'da build+test qiladi, (3)
self-contained publish yaratadi, (4) Inno Setup bilan `PFXManager-Setup.exe`ni compile qilib
artifact sifatida yuklaydi.

## 8. Fayl joylashuvlari

| Nima | Qayerda |
|---|---|
| SQLite baza | `%ProgramData%\PFXManager\pfxmanager.db` |
| Karantin (default) | `%ProgramData%\PFXManager\Quarantine\<yyyy-MM-dd_HHmmss>\` |
| Loglar | `%ProgramData%\PFXManager\Logs\` |

Karantin manzilini **Sozlamalar** sahifasidan o'zgartirish mumkin.

## 9. Administrator huquqlari

Dastur odatiy holatda **standard user** sifatida ishga tushadi (`app.manifest`:
`requestedExecutionLevel level="asInvoker"`) — Principle of Least Privilege. Faqat quyidagi
amal administrator talab qilishi mumkin:

- Windows Certificate Store'ning **LocalMachine\My** do'konidan sertifikat o'chirish.

Bunday holatda dastur elevation'ni majburlamaydi — amal muvaffaqiyatsiz bo'lsa, foydalanuvchiga
tushunarli xabar ko'rsatiladi (`WindowsCert_ElevationRequired`), butun dastur qayta administrator
sifatida ishga tushirilishi shart emas.

## 10. Xavfsizlik modeli

- **Parol hech qachon saqlanmaydi**: `PasswordBox` orqali kiritiladi, faqat bitta parse
  operatsiyasi uchun xotirada ishlatiladi, database'ga, log'ga yoki settings'ga yozilmaydi,
  clipboard'ga avtomatik nusxalanmaydi.
- **Bulk "muddati o'tganlarni tanlash" faqat verified `Expired` statusdagi yozuvlarni tanlaydi**
  — `PasswordRequired` va `ReadError` hech qachon kirmaydi (`BulkSelectionService`,
  `BulkSelectionTests` bilan tasdiqlangan). Status hech qachon fayl nomidan yoki
  ModifiedDate'dan emas, faqat parse qilingan `NotAfter`dan hisoblanadi
  (`CertificateStatusEngine`).
- **Default o'chirish — Karantin**, Permanent Delete emas. Permanent Delete faqat aniq
  tasdiqlash dialogidan keyin (default/focused tugma — "Bekor qilish"), va audit log'ga yoziladi.
- **Reparse point / symlink cheksiz recursion himoyasi**: scanner default holatda reparse
  point'larni o'tkazib yuboradi; kiritilsa ham, ko'rilgan katalog'lar to'plami cycle'ni oldini
  oladi (`FileSystemScanner`, `PathHandlingTests`).
- **TOCTOU kamaytirish**: karantinga ko'chirishdan oldin fayl mavjudligi qayta tekshiriladi;
  file-move muvaffaqiyatsiz bo'lsa, database hech qachon "moved" deb belgilanmaydi
  (`QuarantineService`, `QuarantineTests`).
- **Restore konflikt**: original manzilda fayl mavjud bo'lsa, silent overwrite qilinmaydi —
  foydalanuvchi "boshqa nom bilan tiklash" / "manzil tanlash" / "bekor qilish"dan birini tanlaydi.
- **Internet/cloud yo'q**: barcha tahlil butunlay lokal bajariladi; telemetriya default
  o'chirilgan (`AppSettings.TelemetryEnabled = false`).
- **File va Certificate Store ajratilgan** (talab #26): PFX fayl o'chirish diskdagi faylga,
  Store'dan remove qilish faqat Windows Certificate Store yozuviga ta'sir qiladi.

## 11. Ma'lum cheklovlar (Known limitations)

- Bu development muhiti Linux konteynerida ishlaydi va WPF/XAML kompilyatori faqat Windows'da
  mavjud bo'lgani uchun, `PFXManager.App` loyihasi **bu sessiyada mahalliy ravishda build/run
  qilinmagan va ekran skrinshoti olinmagan**. `PFXManager.Core` va `PFXManager.Infrastructure`
  (barcha biznes-mantiq — scanner, parser, status engine, duplicate detection, SQLite, quarantine)
  to'liq build qilingan va 31 ta test bilan tekshirilgan. WPF qatlami qo'lda diqqat bilan yozilgan
  va CommunityToolkit.Mvvm/`Microsoft.Win32` API'lariga mos, lekin yakuniy tasdiqlash uchun
  Windows'da (yoki ilova bilan birga kelgan GitHub Actions workflow orqali) birinchi build talab
  qilinadi. `.github/workflows/build.yml` buni har push'da avtomatik tekshiradi.
- STIR/JShShIR ajratish evristik (heuristic) — sertifikat Subject formatiga qarab har doim
  100% aniq bo'lishi kafolatlanmaydi; bu faqat ko'rsatish uchun, o'chirish qarorlariga hech qachon
  ta'sir qilmaydi.
- Birinchi versiyada cloud sync, E-IMZO server integratsiyasi, sertifikat yangilash/berish kabi
  funksiyalar qasddan kiritilmagan (talab #44, MVP scope).

## 12. Asosiy foydalanuvchi senariysi

1. **Kompyuterni skanerlash** tugmasi bosiladi → local fixed disklar (sozlamalarga qarab
   network/removable ham) rekursiv, asinxron, bekor qilinadigan tarzda skanerlanadi.
2. Topilgan har bir `.pfx`/`.p12` uchun sertifikat ma'lumotlari o'qiladi, statusi hisoblanadi.
3. Dashboard'da statistikalar (Jami / Faol / Muddati o'tgan / 30 kun / 90 kun / Dublikat /
   Parol talab qiladi / Xatolik) ko'rinadi — kartaga bosilsa PFX Files sahifasi shu filtr bilan
   ochiladi.
4. PFX Files sahifasida **"Barcha muddati o'tganlarni tanlash"** → faqat verified expired
   yozuvlar tanlanadi → **"Karantinga ko'chirish"** → barchasi bitta operatsiyada
   `%ProgramData%\PFXManager\Quarantine\<sana>\` papkasiga ko'chiriladi.
5. Karantin sahifasidan istalgan fayl **Qayta tiklash** yoki (aniq tasdiqlashdan keyin)
   **Butunlay o'chirish** qilinishi mumkin.
