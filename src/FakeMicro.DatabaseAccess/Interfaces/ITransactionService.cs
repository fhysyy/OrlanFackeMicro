using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 事务服务接口
/// 提供事务管理功能
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// 在事务中执行操作
    /// </summary>
    Task ExecuteInTransactionAsync(System.Func<Task> action);
}
