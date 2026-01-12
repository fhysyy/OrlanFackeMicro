using FakeMicro.Interfaces.Models;
using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 用户查询Grain接口 - 负责用户数据查询和统计
    /// </summary>
    public interface IUserQueryGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// 根据用户名或邮箱查找用户
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<long?> FindUserByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<UserStatistics> GetUserStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户列表
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<List<UserDto>> GetUsersAsync(string? username = null, string? email = null, string? status = null, CancellationToken cancellationToken = default);
    }
}
