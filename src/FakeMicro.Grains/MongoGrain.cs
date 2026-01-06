using FakeMicro.DatabaseAccess.Interfaces.Mongo;
using FakeMicro.DatabaseAccess.Repositories.Mongo;
using FakeMicro.Entities;

using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Grains
{
    public class MongoGrain : OrleansGrainBase, IMongoGrain
    {
        private readonly IMongoActRepository mongoActRepository;

        public MongoGrain(IMongoActRepository _mongoActRepository, ILogger<MongoGrain> logger) : base(logger)
        {
            mongoActRepository = _mongoActRepository;
        }
        public async Task<BaseResultModel> DataInfo(string formName, string id)
        {
            var expand=new BaseResultModel();
            try
            {
                var dbName = this.GetPrimaryKeyString();
                var result=await mongoActRepository.GetByIdAsync(ObjectId.Parse(id), dbName, formName);
                expand=BaseResultModel.SuccessResult(data:result, message: "操作成功");
            }
            catch (Exception ex)
            {

               expand=BaseResultModel.FailedResult(message:ex.Message);
                
            }
            return expand;
        }

        public async Task<BaseResultModel> DeleteData(string id)
        {
            
            var expand = new BaseResultModel();
            try
            {
                var dbName = this.GetPrimaryKeyString();
                await mongoActRepository.DeleteByIdAsync(ObjectId.Parse(id), dbName, "FormDefinition");
                expand.Success = true;
                expand.Data = id;
            }
            catch (Exception ex)
            {

                expand.Message = ex.Message;
                expand.Success = false;
            }
            return expand;

        }



        public async Task<BaseResultModel> InsertData(string formName, string data)
        {
            var expand = new BaseResultModel();
            try
            {
                // 直接使用传入的字典数据，减少JSON序列化/反序列化
                dynamic dydata = ((JObject)JsonConvert.DeserializeObject<object>(data)).ToObject<IDictionary<string, object>>().ToExpando();
                var objectId = ObjectId.GenerateNewId();
                dydata._id = objectId.ToString();
                await mongoActRepository.AddAsync(dydata, "FakeMicroDB", formName);
                expand = BaseResultModel.SuccessResult(
                                        data: objectId.ToString(),
                                        message: "操作成功"
                                    );


            }
            catch (Exception ex)
            {
                expand = BaseResultModel.FailedResult(message: ex.Message);
            }
            return expand;
        }

        public async Task<string> SearchData(PageQueryModel queryModel)
        {
            var expand = new BaseResultModel();
            try
            {
                // 直接使用传入的查询模型，减少JSON序列化/反序列化
                
                // 从查询模型中提取过滤条件
                List<QueryFilter> filters = new List<QueryFilter>();
                
                // 如果Filter字典不为空，将其转换为QueryFilter列表
                if (queryModel.Filter != null && queryModel.Filter.Any())
                {
                    foreach (var kvp in queryModel.Filter)
                    {
                        // 默认使用相等操作符，可根据需要扩展
                        filters.Add(new QueryFilter
                        {
                            Field = kvp.Key,
                            Operator = "eq",
                            Value = kvp.Value
                        });
                    }
                }
                
                // 使用查询构建器构建MongoDB过滤条件
                var filter = MongoQueryBuilder.BuildFilter(filters);
                
                var result = await mongoActRepository.GetPagedByConditionAsync(filter, (int)queryModel.PageNumber, queryModel.PageSize,null,true, "FakeMicroDB", "FormDefinition");
                
                expand = BaseResultModel.SuccessResult(
                                        data: result.Data,
                                        message: "操作成功"
                                    );
            }
            catch (Exception ex)
            {
                expand = BaseResultModel.FailedResult(message: ex.Message);
            }
            return await  Task.FromResult(JsonConvert.SerializeObject(expand));
        }

        public async Task<BaseResultModel> UpdateData(string id, Dictionary<string, object> data)
        {
            var formName = "FormDefinition";
            var expand = new BaseResultModel();
            try
            {
                // 直接使用传入的字典数据，减少JSON序列化/反序列化
                await mongoActRepository.UpdateAsync(ObjectId.Parse(id), data, "FakeMicroDB", formName);
                expand = BaseResultModel.SuccessResult(data: id, message: "操作成功");
            }
            catch (Exception ex)
            {
                expand = BaseResultModel.FailedResult(message: ex.Message);
            }
            return expand;
        }

        public Task<BaseResultModel> ValidateData(Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }
    }
}
