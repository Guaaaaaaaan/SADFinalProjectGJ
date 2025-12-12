using System;
using System.ComponentModel.DataAnnotations;

namespace SADFinalProjectGJ.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public string? UserId { get; set; }      // 操作人ID
        public string? UserName { get; set; }    // 操作人名字 (冗余存一份方便查询)
        public string Action { get; set; } = string.Empty; // 动作: Create, Edit, Delete, Send
        public string EntityType { get; set; } = string.Empty; // 对象类型: Invoice, Client
        public string? EntityId { get; set; }    // 对象ID: 1001
        public string? Details { get; set; }     // 详情: "Changed status to Paid"
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}