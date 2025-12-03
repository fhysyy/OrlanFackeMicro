using FakeMicro.DatabaseAccess.Interfaces.Mongo;
using FakeMicro.DatabaseAccess.Repositories.Mongo;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.FakeMicro.Interfaces;
using FakeMicro.Utilities;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;
namespace FakeMicro.Grains
{
    internal class MongoGrain : Grain, IMongoGrain
    {
        private readonly IMongoActRepository mongoActRepository;
        public MongoGrain(IMongoActRepository _mongoActRepository) {

            mongoActRepository= _mongoActRepository;
        }
        public async Task<string> DataInfo(string data)
        {
            var expand=new BaseResultModel<object>();
            try
            {
                //dynamic info = JsonConvert.SerializeObject(data);
                //info._id = ObjectId.GenerateNewId().ToString();
                var result=await mongoActRepository.GetByIdAsync(data, "FakeMicroDB", "activities");
                // 使用支持collectionName参数的方法重载
                //var result= mongoActRepository.AddAsync(data, "FakeMicroDB", "activities");
                expand.Success = true;
                expand.Data = result;
            }
            catch (Exception ex)
            {

                expand.ErrorMessage = ex.Message;
                expand.Success = false;
                
            }
            return await Task.FromResult(JsonConvert.SerializeObject(expand));
        }

        public async Task<string> DeleteData(string data)
        {
            
            var expand = new BaseResultModel<object>();
            try
            {
                await mongoActRepository.DeleteAsync(data, "FakeMicroDB", "activities");
                expand.Success = true;
                expand.Data = data;
            }
            catch (Exception ex)
            {

                expand.ErrorMessage = ex.Message;
                expand.Success = false;
            }
            return await Task.FromResult(JsonConvert.SerializeObject(expand));

        }

        public async Task<string> InsertData(string data)
        {
            var expand = new BaseResultModel();
            try
            {
                // dynamic info = JsonConvert.DeserializeObject<dynamic>(data).ToBsonDocument();
                dynamic info = ((JObject)JsonConvert.DeserializeObject<object>(data)).ToObject<IDictionary<string, object>>().ToExpando();
                //var dynData=JsonObjectNode.Parse(data);

                info._id = ObjectId.GenerateNewId();
                // 使用支持collectionName参数的方法重载
                await mongoActRepository.AddAsync(info, "FakeMicroDB", "activities");
                expand.Success = true;
                expand.Data = info._id.ToString();
                //expand.SuccessResult();
            }
            catch (Exception ex)
            {

                expand.ErrorMessage = ex.Message;
                expand.Success = false;

            }
            return await Task.FromResult(JsonConvert.SerializeObject(expand));
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
