using FakeMicro.Entities.ManagerVersion;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces
{
    public interface IMongoGrain:IGrainWithStringKey
    {
        Task<BaseResultModel> InsertData(string formName, string data);
        Task<BaseResultModel> UpdateData(string id, Dictionary<string, object> data);
        Task<BaseResultModel> DeleteData(string id);
        [ReadOnly]
        [AlwaysInterleave]
        Task<BaseResultModel> DataInfo(string formName, string id);
        [ReadOnly]
        [AlwaysInterleave]
        Task<string> SearchData(PageQueryModel queryModel);
        [ReadOnly]
        [AlwaysInterleave]
        Task<BaseResultModel> ValidateData(Dictionary<string, object> data);
    }
}
