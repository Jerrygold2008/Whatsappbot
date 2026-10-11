using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Exceptions
{
   public class DomainException(string message) : Exception(message);
}