using Microsoft.Extensions.DependencyInjection;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Services;
using PFXManager.Infrastructure.Certificates;
using PFXManager.Infrastructure.FileSystem;
using PFXManager.Infrastructure.Persistence;
using PFXManager.Infrastructure.Quarantine;
using PFXManager.Infrastructure.Scanning;

namespace PFXManager.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPfxManagerInfrastructure(this IServiceCollection services, string? databasePath = null)
    {
        AppPaths.EnsureDirectoriesExist();

        services.AddSingleton(new SqliteConnectionFactory(databasePath));
        services.AddSingleton<DatabaseMigrator>();

        services.AddSingleton<ICertificateStatusEngine, CertificateStatusEngine>();
        services.AddSingleton<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddSingleton<IBulkSelectionService, BulkSelectionService>();
        services.AddSingleton<IIdentifierExtractionService, IdentifierExtractionService>();

        services.AddSingleton<IDriveDiscoveryService, DriveDiscoveryService>();
        services.AddTransient<IFileSystemScanner, FileSystemScanner>();
        services.AddTransient<ICertificateParser, X509CertificateParser>();
        services.AddTransient<ICertificateRecordFactory, CertificateRecordFactory>();
        services.AddTransient<IScanOrchestrator, ScanOrchestrator>();
        services.AddSingleton<IWindowsCertificateStoreService, WindowsCertificateStoreService>();
        services.AddTransient<IQuarantineService, QuarantineService>();

        services.AddSingleton<ICertificateRecordRepository, SqliteCertificateRecordRepository>();
        services.AddSingleton<IScanSessionRepository, SqliteScanSessionRepository>();
        services.AddSingleton<IQuarantineRepository, SqliteQuarantineRepository>();
        services.AddSingleton<IOperationLogRepository, SqliteOperationLogRepository>();
        services.AddSingleton<IAppSettingsRepository, SqliteAppSettingsRepository>();
        services.AddSingleton<IAuditLogger, SqliteAuditLogger>();

        return services;
    }
}
