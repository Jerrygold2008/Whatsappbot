using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Exceptions
{
public class NotFoundException(string message) : DomainException(message);
}