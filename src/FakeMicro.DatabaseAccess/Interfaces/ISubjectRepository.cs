using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using System.Linq.Expressions;
using System.Threading;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 科目数据访问接口
/// </summary>
public interface ISubjectRepository : IRepository<Subject, long>
{
    /// <summary>
    /// 根据科目代码获取科目
    /// </summary>
    Task<Subject?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据科目名称获取科目
    /// </summary>
    Task<Subject?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取所有科目列表
    /// </summary>
    Task<List<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据年级获取科目列表
    /// </summary>
    Task<List<Subject>> GetSubjectsByGradeAsync(string grade, CancellationToken cancellationToken = default);
}