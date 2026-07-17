namespace DCView.Hackathon.Application.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportUserAsync(string userId);
    Task<byte[]> ExportAllAsync();
}
