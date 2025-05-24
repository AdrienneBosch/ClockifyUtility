using System.Threading.Tasks;

namespace ClockifyUtility.Services
{
    public interface IPdfService
    {
        Task SavePdfAsync(string html, string outputPath);
    }
}
