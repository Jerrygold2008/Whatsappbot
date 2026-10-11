using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Models
{
    public class WhatsAppAccount
    {
   public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public string WabaId { get; set; } = "";
    public string PhoneNumberId { get; set; } = "";       // unique routing key
    public string DisplayPhoneNumber { get; set; } = "";
    public string EncryptedAccessToken { get; set; } = "";
    public WhatsAppStatus Status { get; set; } = WhatsAppStatus.Pending;
    }
}