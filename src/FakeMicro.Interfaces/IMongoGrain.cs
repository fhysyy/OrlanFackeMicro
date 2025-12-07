using FakeMicro.Entities.ManagerVersion;
using FakeMicro.Interfaces.FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
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
        // 使用强类型参数替代string，减少JSON序列化/反序列化
        Task<BaseResultModel> InsertData(string formName, Dictionary<string, object> data);
        Task<BaseResultModel> UpdateData(string id, Dictionary<string, object> data);
        Task<BaseResultModel> DeleteData(string id);
        Task<BaseResultModel> DataInfo(string formName, string id);
        Task<string> SearchData(PageQueryModel queryModel);
        Task<BaseResultModel> ValidateData(Dictionary<string, object> data);
    }
}
