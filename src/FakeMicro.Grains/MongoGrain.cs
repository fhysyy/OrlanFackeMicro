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
        public async Task<string> DataInfo(string formName, string data)
        {
            var expand=new BaseResultModel();
            try
            {
                var dbName = this.GetPrimaryKeyString();
                var result=await mongoActRepository.GetByIdAsync(ObjectId.Parse(data), dbName, formName);
                expand=BaseResultModel.SuccessResult(data:result, message: "操作成功");
            }
            catch (Exception ex)
            {

               expand=BaseResultModel.FailedResult(message:ex.Message);
                
            }
            return await Task.FromResult(JsonConvert.SerializeObject(expand));
        }

        public async Task<string> DeleteData(string data)
        {
            
            var expand = new BaseResultModel<object>();
            try
            {
                var dbName = this.GetPrimaryKeyString();
                await mongoActRepository.DeleteByIdAsync(ObjectId.Parse(data), dbName, "FormDefinition");
                expand.Success = true;
                expand.Data = data;
            }
            catch (Exception ex)
            {

                expand.Message = ex.Message;
                expand.Success = false;
            }
            return await Task.FromResult(JsonConvert.SerializeObject(expand));

        }

        public async Task<string> InsertData(string formName,string data)
        {
            var expand = new BaseResultModel();
            try
            {
                dynamic info = ((JObject)JsonConvert.DeserializeObject<object>(data)).ToObject<IDictionary<string, object>>().ToExpando();
                info._id = ObjectId.GenerateNewId();
                await mongoActRepository.AddAsync(info, "FakeMicroDB",formName);
                expand = BaseResultModel.SuccessResult(
                                        data: info._id,
                                        message: "操作成功"
                                    );
            }
            catch (Exception ex)
            {
                expand=BaseResultModel.FailedResult(message:ex.Message);
            }
            return await Task.FromResult(JsonConvert.SerializeObject(expand));
        }

        public Task<BaseResultModel> SearchData(string data)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResultModel> UpdateData(string data)
        {
            var formName = "FormDefinition";
            var expand = new BaseResultModel();
            try
            {
                dynamic info = ((JObject)JsonConvert.DeserializeObject<object>(data)).ToObject<IDictionary<string, object>>().ToExpando();
                info._id = ObjectId.GenerateNewId();
                await mongoActRepository.UpdateAsync(info, "FakeMicroDB",formName);
                expand = BaseResultModel.SuccessResult(
                                        data: info._id,
                                        message: "操作成功"
                                    );
            }
            catch (Exception ex)
            {
                expand = BaseResultModel.FailedResult(message: ex.Message);
            }
            return await Task.FromResult(JsonConvert.SerializeObject(expand));
        }

        public Task<BaseResultModel> ValidateData(string data)
        {
            throw new NotImplementedException();
        }
    }
}
