using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;
using System;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task LogAsync(string action, string entityType, string entityId, string details)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            string userId = "System";
            string userName = "System";

            if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
            {
                userId = _userManager.GetUserId(user) ?? "Unknown";
                userName = user.Identity.Name ?? "Unknown";
            }

            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}