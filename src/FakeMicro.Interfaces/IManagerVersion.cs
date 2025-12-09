using FakeMicro.Entities.ManagerVersion;
using FakeMicro.Interfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace FakeMicro.Interfaces
{
    public interface IManagerVersion:IGrainWithStringKey
    {
        Task<BaseResultModel> InsertData(string data); 
        Task<BaseResultModel> UpdateData(string data);
        Task<BaseResultModel> DeleteData(string data);
        Task<BaseResultModel<ManagerVersion>> DataInfo(string data);
        Task<BaseResultModel> SearchData(string data);
        Task<BaseResultModel> ValidateData(string data);
    }
}
