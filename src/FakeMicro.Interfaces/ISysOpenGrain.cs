using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
namespace FakeMicro.Interfaces
{
    public interface ISysOpenGrain:IGrainWithStringKey
    {
        [ReadOnly]
        [AlwaysInterleave]
        Task<string> GetSysOpenSetting(string data);
    }
}
