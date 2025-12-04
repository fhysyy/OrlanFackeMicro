using FakeMicro.Entities.ManagerVersion;
using FakeMicro.Interfaces.FakeMicro.Interfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces
{
    public interface IMongoGrain:IGrainWithStringKey
    {
        Task<string> InsertData(string formName,string data);
        Task<BaseResultModel> UpdateData(string data);
        Task<string> DeleteData(string data);
        Task<string> DataInfo(string formName, string data);
        Task<BaseResultModel> SearchData(string data);
        Task<BaseResultModel> ValidateData(string data);
    }
}
