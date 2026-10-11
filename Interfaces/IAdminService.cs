using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Dtos;

namespace SellFlowApi.Interfaces
{
   
public interface IAdminService
{
    Task LinkWhatsAppAsync(LinkWhatsAppRequest request);
    Task SetPaystackSubaccountAsync(Guid businessId, string subaccountCode);
}
}