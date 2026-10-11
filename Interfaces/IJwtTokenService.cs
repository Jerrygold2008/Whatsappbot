using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Models;

namespace SellFlowApi.Interfaces
{
public interface IJwtTokenService
{
    string Create(User user, Guid? businessId);
}
}