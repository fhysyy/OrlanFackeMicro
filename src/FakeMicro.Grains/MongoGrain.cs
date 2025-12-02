using FakeMicro.DatabaseAccess.Interfaces.Mongo;
using FakeMicro.DatabaseAccess.Repositories.Mongo;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.FakeMicro.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;
using Newtonsoft.Json;
using MongoDB.Bson;
namespace FakeMicro.Grains
{
    internal class MongoGrain : Grain, IMongoGrain
    {
        private readonly IMongoActRepository mongoActRepository;
        public MongoGrain(IMongoActRepository _mongoActRepository) {

            mongoActRepository= _mongoActRepository;
        }
        public Task<BaseResultModel<object>> DataInfo(string data)
        {
            var expand=new BaseResultModel<object>();
            try
            {
                dynamic info = JsonConvert.SerializeObject(data);
                info._id = ObjectId.GenerateNewId().ToString();
                // 使用支持collectionName参数的方法重载
                var result= mongoActRepository.AddAsync(data, "FakeMicroDB", "activities");
                expand.Success = true;
                expand.Data = result;
            }
            catch (Exception ex)
            {

                expand.ErrorMessage = ex.Message;
                expand.Success = false;
                
            }
           return Task.FromResult(expand);
        }

        public Task<BaseResultModel> DeleteData(string data)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResultModel> InsertData(string data)
        {
            var expand = new BaseResultModel();
            try
            {
                dynamic info = JsonConvert.DeserializeObject<dynamic>(data);
                info._id = ObjectId.GenerateNewId().ToString();
                // 使用支持collectionName参数的方法重载
                var result = mongoActRepository.AddAsync(data, "FakeMicroDB", "activities");
                expand.Success = true;
                expand.Data = result;
            }
            catch (Exception ex)
            {

                expand.ErrorMessage = ex.Message;
                expand.Success = false;

            }
            return Task.FromResult(expand);
        }

        public Task<BaseResultModel> SearchData(string data)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResultModel> UpdateData(string data)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResultModel> ValidateData(string data)
        {
            throw new NotImplementedException();
        }
    }
}
