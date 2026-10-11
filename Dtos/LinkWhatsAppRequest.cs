using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
  public record LinkWhatsAppRequest(Guid BusinessId, string WabaId, string PhoneNumberId,
    string DisplayPhoneNumber, string AccessToken);
}
