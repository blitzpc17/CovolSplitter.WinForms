using CovolSplitter.WinForms.Models;
using CovolSplitter.WinForms.Services;

namespace CovolSplitter.WinForms.Services;

public sealed class CovolImportService
{
    private readonly CovolXmlParser _parser;
    private readonly CovolRepository _repository;

    public CovolImportService(CovolXmlParser parser, CovolRepository repository)
    {
        _parser = parser;
        _repository = repository;
    }

    public async Task<long> ImportAsync(
        string xmlPath,
        IProgress<CovolImportProgress>? progress,
        CancellationToken ct)
    {
        var parsed = await _parser.ParseAsync(xmlPath, progress, ct);

        return await _repository.SavePackageAsync(
            parsed.Package,
            parsed.Products,
            parsed.Transactions,
            progress,
            ct
        );
    }
}