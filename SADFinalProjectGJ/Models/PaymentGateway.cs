using SADFinalProjectGJ.Models;
using System.ComponentModel.DataAnnotations;
namespace SADFinalProjectGJ.Models
{
    public class PaymentGateway
    {
        [Key]
        // PK
        public string GatewayName { get; set; }

        public string ApiKey { get; set; }

        // ICollection
        public ICollection<Payment> Payments { get; set; }
    }
}
