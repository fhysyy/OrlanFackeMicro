using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;

namespace FakeMicro.Silo.Clustering
{
    /// <summary>
    /// 简单的内存实现IMembershipTable，用于解决Orleans启动时的依赖注入问题
    /// 这是一个完整的接口实现，提供所有必需的方法来让Silo正常启动和运行
    /// </summary>
    public class SimpleInMemoryMembershipTable : IMembershipTable
    {
        private readonly List<MembershipEntry> _members = new List<MembershipEntry>();
        private readonly object _lock = new object();

        public Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate) => Task.CompletedTask;

        public Task DeleteMembershipTableEntries(string clusterId) 
        {
            lock (_lock)
            {
                _members.Clear();
            }
            return Task.CompletedTask;
        }

        public Task InitializeMembershipTable(bool tryInitTableVersion) => Task.CompletedTask;

        public Task<MembershipTableData> ReadAll() 
        {
            lock (_lock)
            {
                // 转换为MembershipTableData期望的Tuple格式
                var memberTuples = new List<Tuple<MembershipEntry, string>>();
                foreach (var member in _members)
                {
                    memberTuples.Add(new Tuple<MembershipEntry, string>(member, member.SiloAddress?.ToString() ?? ""));
                }
                
                return Task.FromResult(new MembershipTableData(
                    memberTuples,
                    new TableVersion(0, "0")));
            }
        }

        public Task<MembershipTableData> ReadRow(string clusterId, string instanceId) 
        {
            lock (_lock)
            {
                // 转换为MembershipTableData期望的Tuple格式
                var memberTuples = new List<Tuple<MembershipEntry, string>>();
                foreach (var member in _members.FindAll(m => m.SiloAddress != null && m.SiloAddress.ToString() == instanceId))
                {
                    memberTuples.Add(new Tuple<MembershipEntry, string>(member, member.SiloAddress.ToString()));
                }
                
                return Task.FromResult(new MembershipTableData(
                    memberTuples,
                    new TableVersion(0, "0")));
            }
        }

        public Task<MembershipTableData> ReadRow(SiloAddress siloAddress) 
        {
            lock (_lock)
            {
                // 转换为MembershipTableData期望的Tuple格式
                var memberTuples = new List<Tuple<MembershipEntry, string>>();
                foreach (var member in _members.FindAll(m => m.SiloAddress == siloAddress))
                {
                    memberTuples.Add(new Tuple<MembershipEntry, string>(member, member.SiloAddress.ToString()));
                }
                
                return Task.FromResult(new MembershipTableData(
                    memberTuples,
                    new TableVersion(0, "0")));
            }
        }

        public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion) 
        {
            lock (_lock)
            {
                // 使用SiloAddress作为唯一标识
                if (!_members.Exists(m => m.SiloAddress == entry.SiloAddress))
                {
                    _members.Add(entry);
                    return Task.FromResult(true); // 插入成功
                }
                return Task.FromResult(false); // 已存在，插入失败
            }
        }

        public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion) 
        {
            lock (_lock)
            {
                var existingIndex = _members.FindIndex(m => m.SiloAddress == entry.SiloAddress);
                if (existingIndex >= 0)
                {
                    _members[existingIndex] = entry;
                    return Task.FromResult(true); // 更新成功
                }
                return Task.FromResult(false); // 不存在，更新失败
            }
        }

        // 保留原始的UpdateRow重载以保持兼容性
        public Task UpdateRow(MembershipEntry entry, TableVersion tableVersion, MembershipEntry? previousEntry = null) 
        {
            return UpdateRow(entry, "", tableVersion).ContinueWith(t => { });
        }

        public Task UpdateIAmAlive(MembershipEntry entry) 
        {
            lock (_lock)
            {
                var existingIndex = _members.FindIndex(m => m.SiloAddress == entry.SiloAddress);
                if (existingIndex >= 0)
                {
                    // 更新IAmAlive时间戳等信息
                    _members[existingIndex] = entry;
                }
                return Task.CompletedTask;
            }
        }
    }
}
