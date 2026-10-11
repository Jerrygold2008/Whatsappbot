using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Models
{
    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = "";
    }
}