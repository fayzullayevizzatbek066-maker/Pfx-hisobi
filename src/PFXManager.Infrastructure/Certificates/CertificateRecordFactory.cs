using Microsoft.Extensions.Logging;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Certificates;

public sealed class CertificateRecordFactory : ICertificateRecordFactory
{
    private readonly ICertificateParser _parser;
    private readonly ICertificateStatusEngine _statusEngine;
    private readonly IIdentifierExtractionService _identifierExtractionService;
    private readonly ILogger<CertificateRecordFactory> _logger;

    public CertificateRecordFactory(
        ICertificateParser parser,
        ICertificateStatusEngine statusEngine,
        IIdentifierExtractionService identifierExtractionService,
        ILogger<CertificateRecordFactory> logger)
    {
        _parser = parser;
        _statusEngine = statusEngine;
        _identifierExtractionService = identifierExtractionService;
        _logger = logger;
    }

    public async Task<CertificateRecord> BuildAsync(string filePath, string? password, Guid scanSessionId, CancellationToken cancellationToken)
    {
        var record = new CertificateRecord
        {
            FullPath = filePath,
            Drive = Path.GetPathRoot(filePath) ?? string.Empty,
            ScanSessionId = scanSessionId
        };

        try
        {
            var fileInfo = new FileInfo(filePath);
            record.FileSizeBytes = fileInfo.Length;
            record.CreatedTimeUtc = fileInfo.CreationTimeUtc;
            record.LastModifiedTimeUtc = fileInfo.LastWriteTimeUtc;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Could not read file metadata for {Path}", filePath);
        }

        var parseResult = await _parser.ParseAsync(filePath, password, cancellationToken);

        record.IsPasswordProtected = parseResult.PasswordRequired;
        record.ReadErrorMessage = parseResult.ErrorMessage;

        if (parseResult.Success)
        {
            record.Subject = parseResult.Subject;
            record.CommonName = parseResult.CommonName;
            record.Organization = parseResult.Organization;
            record.OrganizationalUnit = parseResult.OrganizationalUnit;
            record.Issuer = parseResult.Issuer;
            record.SerialNumber = parseResult.SerialNumber;
            record.Thumbprint = parseResult.Thumbprint;
            record.NotBefore = parseResult.NotBefore;
            record.NotAfter = parseResult.NotAfter;
            record.HasPrivateKey = parseResult.HasPrivateKey;
            record.SignatureAlgorithm = parseResult.SignatureAlgorithm;
            record.FriendlyName = parseResult.FriendlyName;
            record.CertificateVersion = parseResult.CertificateVersion;
            record.KeyAlgorithm = parseResult.KeyAlgorithm;
            record.RawSubject = parseResult.Subject;

            var identifiers = _identifierExtractionService.Extract(parseResult.Subject);
            record.Stir = identifiers.Stir;
            record.Pinfl = identifiers.Pinfl;
            record.OwnerDisplayName = identifiers.OwnerDisplayName ?? parseResult.CommonName;
        }

        record.Status = _statusEngine.DetermineStatus(
            record.NotAfter,
            passwordRequired: parseResult.PasswordRequired,
            readError: !parseResult.Success && !parseResult.PasswordRequired);

        if (record.NotAfter is not null)
        {
            record.RemainingDays = _statusEngine.ComputeRemainingDays(record.NotAfter.Value);
        }

        return record;
    }
}
