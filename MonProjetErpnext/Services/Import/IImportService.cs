using MonProjetErpnext.Models.Import;

namespace MonProjetErpnext.Services.Import
{
    public interface IImportService
    {
        Task<ImportResult> ImportFromCsvAsync(ImportRequest request);
    }
}