namespace PFXManager.App.Resources;

/// <summary>
/// Single source of truth for every user-facing string (default: O'zbekcha, lotin yozuvi). No
/// view or view model should embed a literal UI string outside of this class — that keeps
/// localization a drop-in change: add a Russian/English variant of this class (or promote it to
/// a ResourceManager-backed .resx set, which this class's flat static-property shape maps onto
/// directly) and swap it in via <see cref="Services.ILocalizationService"/> without touching any
/// view.
/// </summary>
public static class Strings
{
    public const string AppTitle = "PFX Manager";

    // Navigation
    public const string Nav_Dashboard = "Boshqaruv paneli";
    public const string Nav_PfxFiles = "PFX fayllar";
    public const string Nav_WindowsCertificates = "Windows sertifikatlari";
    public const string Nav_Duplicates = "Dublikatlar";
    public const string Nav_Quarantine = "Karantin";
    public const string Nav_ScanHistory = "Skanerlash tarixi";
    public const string Nav_Settings = "Sozlamalar";

    // Top bar / scan
    public const string ScanComputer = "Kompyuterni skanerlash";
    public const string Scanning = "Skanerlanmoqda...";
    public const string Cancel = "Bekor qilish";
    public const string CurrentFolder = "Joriy papka";
    public const string FilesCheckedLabel = "Ko'rib chiqildi";
    public const string PfxFoundLabel = "Topildi";
    public const string ErrorsLabel = "Xatolik";
    public const string SearchPlaceholder = "Qidirish...";

    // Dashboard cards / statuses
    public const string Total = "Jami";
    public const string Active = "Faol";
    public const string Expired = "Muddati o'tgan";
    public const string ExpiringSoon30 = "30 kun ichida tugaydi";
    public const string Expiring90 = "90 kun ichida tugaydi";
    public const string PasswordRequired = "Parol talab qilinadi";
    public const string ReadError = "O'qib bo'lmadi";
    public const string DuplicatesLabel = "Dublikatlar";

    public const string StatusBadge_Active = "\U0001F7E2 Faol";
    public const string StatusBadge_Expiring = "\U0001F7E0 90 kun ichida tugaydi";
    public const string StatusBadge_ExpiringSoon = "\U0001F7E1 30 kun ichida tugaydi";
    public const string StatusBadge_Expired = "\U0001F534 Muddati o'tgan";
    public const string StatusBadge_PasswordRequired = "\U0001F512 Parol talab qilinadi";
    public const string StatusBadge_ReadError = "⚠ O'qib bo'lmadi";

    // Actions
    public const string SelectAllExpired = "Barcha muddati o'tganlarni tanlash";
    public const string MoveToQuarantine = "Karantinga ko'chirish";
    public const string Restore = "Qayta tiklash";
    public const string PermanentDelete = "Butunlay o'chirish";
    public const string ShowInExplorer = "Explorer'da ko'rsatish";
    public const string CopyPath = "Manzilni nusxalash";
    public const string Refresh = "Yangilash";
    public const string ClearSelection = "Tanlovni bekor qilish";
    public const string EnterPassword = "Parol kiritish";

    // Filters
    public const string FilterStatusLabel = "Status";
    public const string FilterAll = "Barchasi";
    public const string FilterDriveLabel = "Disk";
    public const string FilterAllDrives = "Barcha disklar";

    // Columns
    public const string Col_Select = "";
    public const string Col_Status = "Status";
    public const string Col_Owner = "Egasi / Tashkilot";
    public const string Col_Stir = "STIR / JShShIR";
    public const string Col_Serial = "Serial Number";
    public const string Col_Thumbprint = "Thumbprint";
    public const string Col_NotBefore = "Boshlanish";
    public const string Col_NotAfter = "Tugash";
    public const string Col_RemainingDays = "Qolgan kun";
    public const string Col_FileName = "Fayl nomi";
    public const string Col_FilePath = "Fayl manzili";
    public const string Col_Drive = "Disk";
    public const string Col_Size = "Hajmi";

    // Permanent delete confirmation
    public const string Dialog_PermanentDeleteTitle = "Butunlay o'chirish";
    public const string Dialog_PermanentDeleteMessageFormat = "{0} ta PFX fayl butunlay o'chiriladi.\nBu amalni ortga qaytarib bo'lmaydi.";
    public const string Dialog_Cancel = "Bekor qilish";
    public const string Dialog_DeletePermanently = "Butunlay o'chirish";

    // Password prompt
    public const string Dialog_PasswordTitle = "Parol kiritish";
    public const string Dialog_PasswordPromptFormat = "\"{0}\" faylini ochish uchun parolni kiriting:";
    public const string Dialog_Ok = "OK";

    // Restore conflict
    public const string Dialog_RestoreConflictTitle = "Fayl allaqachon mavjud";
    public const string Dialog_RestoreConflictMessageFormat = "\"{0}\" manzilida shu nomli fayl allaqachon mavjud. Nima qilishni tanlang:";
    public const string Dialog_RenameNew = "Boshqa nom bilan tiklash";
    public const string Dialog_ChooseDestination = "Manzil tanlash";

    // Settings
    public const string Settings_ScanLocations = "Skanerlash joylari";
    public const string Settings_LocalFixedDrives = "Local fixed drives";
    public const string Settings_NetworkDrives = "Network drives";
    public const string Settings_RemovableDrives = "Removable drives";
    public const string Settings_CustomFolders = "Maxsus papkalar";
    public const string Settings_AddFolder = "Papka qo'shish";
    public const string Settings_RemoveFolder = "O'chirish";
    public const string Settings_Expiration = "Amal qilish muddati";
    public const string Settings_WarningDays = "Ogohlantirish (kun): 30";
    public const string Settings_SecondaryWarningDays = "Ikkinchi ogohlantirish (kun): 90";
    public const string Settings_QuarantinePath = "Karantin manzili";
    public const string Settings_Appearance = "Ko'rinish";
    public const string Settings_ThemeSystem = "Tizim";
    public const string Settings_ThemeLight = "Yorug'";
    public const string Settings_ThemeDark = "Qorong'i";
    public const string Settings_Save = "Saqlash";
    public const string Settings_Saved = "Sozlamalar saqlandi.";

    // Scan history
    public const string ScanHistory_StartedAt = "Boshlangan";
    public const string ScanHistory_FinishedAt = "Tugagan";
    public const string ScanHistory_FilesChecked = "Ko'rib chiqilgan fayllar";
    public const string ScanHistory_PfxFound = "Topilgan PFX";
    public const string ScanHistory_Expired = "Muddati o'tgan";
    public const string ScanHistory_Errors = "Xatoliklar";
    public const string ScanHistory_Cancelled = "Bekor qilingan";

    // Quarantine
    public const string Quarantine_Owner = "Egasi";
    public const string Quarantine_OriginalPath = "Oldingi manzil";
    public const string Quarantine_QuarantinedAt = "Karantinga olingan sana";
    public const string Quarantine_Reason = "Sabab";
    public const string Quarantine_Empty = "Karantin bo'sh.";

    // Windows certificate store
    public const string WindowsCert_StoreLabel = "Do'kon";
    public const string WindowsCert_CurrentUser = "CurrentUser\\My";
    public const string WindowsCert_LocalMachine = "LocalMachine\\My";
    public const string WindowsCert_Remove = "Do'kondan olib tashlash";
    public const string WindowsCert_RemoveConfirmFormat = "\"{0}\" sertifikati Windows sertifikat do'konidan olib tashlanadi. Davom etasizmi?";
    public const string WindowsCert_ElevationRequired = "LocalMachine do'konidan olib tashlash uchun administrator huquqlari kerak bo'lishi mumkin.";

    public const string DiscoveredCount_Format = "{0} ta muddati o'tgan PFX topildi.";
    public const string SelectedCount_Format = "{0} ta tanlandi.";
    public const string OperationSucceededFormat = "{0} ta muvaffaqiyatli, {1} ta muvaffaqiyatsiz.";
    public const string NoRecordsSelected = "Hech qanday fayl tanlanmagan.";
    public const string ScanCompletedFormat = "Skanerlash tugadi. Topildi: {0}, muddati o'tgan: {1}, xatolik: {2}.";
    public const string ScanCancelled = "Skanerlash bekor qilindi.";
}
