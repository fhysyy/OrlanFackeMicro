using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities.ManagerVersion;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.FakeMicro.Interfaces;
using MongoDB.Bson;
using Newtonsoft.Json;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.Grains
{
    public class ManagerVersionGrain : Grain, IManagerVersion
    {
        private readonly IManagerVersionRepository _managerVersionRepository;
        private readonly string _databaseName; // 添加数据库名称字段
        
        public ManagerVersionGrain(IManagerVersionRepository managerVersionRepository)
        {
            _managerVersionRepository = managerVersionRepository;
            _databaseName = "your-database-name"; // 可以从配置中获取，这里示例直接赋值
        }
        public async Task<BaseResultModel> DeleteData(string data)
        {
            var expand = new BaseResultModel();
            try
            {
                // 实现删除逻辑
                expand.Success = true;
                expand.Data = "删除成功";
            }
            catch (Exception ex)
            {
                expand.Success = false;
                expand.Data = $"{ex.Message}{ex.StackTrace}";
            }
            return await Task.FromResult(expand);
        }

        public async Task<BaseResultModel> DataInfo(string data)
        {
            var  expand = new BaseResultModel();
            try
            {
                // 实现获取数据信息逻辑
                expand.Success = true;
                expand.Data = "数据信息获取成功";
            }
            catch (Exception ex)
            {
                expand.Success = false;
                expand.Data = $"{ex.Message}{ex.StackTrace}";
            }
            return await Task.FromResult(expand);
        }

        public async Task<BaseResultModel> InsertData(string data)
        {
            var expand = new BaseResultModel();
            try
            {
                var id=Guid.NewGuid().ToString();
                var entity = new ManagerVersion() {Id=id };
                
                // 使用仓储自带的AddAsync方法，并传入databaseName参数
                await _managerVersionRepository.AddAsync(entity,"FackeMicroDB");
                
                expand.Success = true;
                expand.Data = $"添加成功{id}";
            }
            catch (Exception ex)
            {
                expand.Success = false;
                expand.Data = $"{ex.Message}{ex.StackTrace}";
            }
            return await Task.FromResult(expand);
        }

        public async Task<BaseResultModel> SearchData(string data)
        {
            var expand = new BaseResultModel();
            try
            {
                // 实现搜索逻辑
                expand.Success = true;
                expand.Data = "搜索成功";
            }
            catch (Exception ex)
            {
                expand.Success = false;
                expand.Data = $"{ex.Message}{ex.StackTrace}";
            }
            return await Task.FromResult(expand);
        }

        public async Task<BaseResultModel> UpdateData(string data)
        {
            var expand = new BaseResultModel();
            try
            {
                // 实现更新逻辑
                expand.Success = true;
                expand.Data = "更新成功";
            }
            catch (Exception ex)
            {
                expand.Success = false;
                expand.Data = $"{ex.Message}{ex.StackTrace}";
            }
            return await Task.FromResult(expand);
        }

        public async Task<BaseResultModel> ValidateData(string data)
        {
            var expand = new BaseResultModel();
            try
            {
                // 实现验证逻辑
                expand.Success = true;

                    expand.Data = "验证成功";
            }
            catch (Exception ex)
            {
                expand.Success = false;
                expand.Data = $"{ex.Message}{ex.StackTrace}";
            }
            return await Task.FromResult(expand);
        }
    }
}
