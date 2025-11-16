using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces.Models
{
    [GenerateSerializer]
    public class PageBaseModel
    {
       [Id(0)]
        public int pageSize { get; set; }
        [Id(1)]
        public int pageNumber { get; set; }

        [Id(2)]
        public int offset
        {
            get
            {
                return (pageNumber - 1) * pageSize;
            }
            set { }
        }
    }
}
