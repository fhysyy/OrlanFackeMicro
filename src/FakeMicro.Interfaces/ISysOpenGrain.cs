using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
namespace FakeMicro.Interfaces
{
    public interface ISysOpenGrain:IGrainWithStringKey
    {
        Task<string> GetSysOpenSetting(string data);
    }
}
