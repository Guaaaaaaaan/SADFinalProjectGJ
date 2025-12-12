using System.ComponentModel.DataAnnotations;

namespace SADFinalProjectGJ.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Key { get; set; } = string.Empty; // 例如: "GstRate"

        [Required]
        public string Value { get; set; } = string.Empty; // 例如: "9"

        public string? Description { get; set; } // 例如: "Current GST Rate in %"
    }
}