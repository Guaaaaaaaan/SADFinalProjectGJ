using System.Threading.Tasks;

namespace SADFinalProjectGJ.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityType, string entityId, string details);
    }
}