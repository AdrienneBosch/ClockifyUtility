using System.Threading.Tasks;

namespace ClockifyUtility.Services
{
    public interface IPdfService
    {
        Task GeneratePdfAsync(string html, string outputPath);
    }
}
