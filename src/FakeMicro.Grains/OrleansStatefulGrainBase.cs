using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Reflection;
namespace FakeMicro.Grains
{
    /// <summary>
    /// Orleans状态管理Grain基类（有状态版本）
    /// 遵循Orleans 9.x最佳实践
    /// 包含批量状态更新和冲突解决策略
    /// </summary>
    public abstract class OrleansStatefulGrainBase<TState> : OrleansGrainBase
        where TState : class, new()
    {
        protected IPersistentState<TState> _state;
        
        // 批量更新队列
        private readonly ConcurrentQueue<Func<Task>> _stateUpdateQueue = new ConcurrentQueue<Func<Task>>();
        
        // 批量更新锁
        private readonly SemaphoreSlim _batchUpdateLock = new SemaphoreSlim(1, 1);
        
        // 批量更新任务标志
        private bool _isBatchUpdateRunning = false;
        
        // 重试配置
        private const int MaxConflictRetries = 3;
        private const int RetryDelayMilliseconds = 100;
        
        /// <summary>
        /// 状态恢复配置
        /// </summary>
        private const int MaxStateLoadRetries = 5;
        private const int StateLoadRetryDelayMilliseconds = 200;

        protected OrleansStatefulGrainBase(
            IPersistentState<TState> state,
            ILogger logger,
            IGrainContext grainContext = null)
            : base(logger, grainContext)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// Orleans最佳实践：状态持久化
        /// 实现冲突解决策略和断路器保护
        /// </summary>
        protected virtual async Task PersistStateAsync()
        {
            int retryCount = 0;
            bool success = false;
            
            while (!success && retryCount < MaxConflictRetries)
            {
                try
                {
                    await _state.WriteStateAsync();
                    LogDebug("状态持久化完成: {GrainIdentity}, 版本: {StateVersion}", 
                        GetGrainIdentity(), GetStateVersion());
                    success = true;
                }
                catch (Exception ex) when (retryCount < MaxConflictRetries - 1 && IsConflictException(ex))
                {
                    // 乐观并发冲突，执行重试策略
                    retryCount++;
                    LogWarning("状态持久化冲突，正在重试 ({RetryCount}/{MaxRetries}): {GrainIdentity}, 错误: {ErrorMessage}", 
                        retryCount, MaxConflictRetries, GetGrainIdentity(), ex.Message);
                    
                    // 等待一段时间后重试
                    await Task.Delay(RetryDelayMilliseconds * retryCount, CancellationToken.None);
                    
                    // 重新加载状态以获取最新版本
                    await _state.ReadStateAsync();
                    
                    // 应用冲突解决策略
                    await ResolveStateConflictAsync(ex);
                }
                catch (Exception ex)
                {
                    LogError(ex, "状态持久化失败: {GrainIdentity}", GetGrainIdentity());
                    throw;
                }
            }
        }

        /// <summary>
        /// 判断是否为并发冲突异常
        /// </summary>
        private bool IsConflictException(Exception ex)
        {
            // 在较新版本的Orleans中，检查异常类型或消息是否表示并发冲突
            return ex.GetType().Name.Contains("InconsistentState") ||
                   ex.GetType().Name.Contains("Concurrency") ||
                   ex.Message.Contains("conflict") ||
                   ex.Message.Contains("version") ||
                   ex.Message.Contains("并发") ||
                   ex.Message.Contains("版本");
        }

        /// <summary>
        /// 冲突解决策略
        /// 可由子类重写以实现特定的冲突解决逻辑
        /// </summary>
        protected virtual Task ResolveStateConflictAsync(Exception ex)
        {
            LogDebug("使用默认冲突解决策略: 服务器版本优先");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取当前状态（只读引用）
        /// </summary>
        protected TState State => _state?.State ?? throw new InvalidOperationException("状态未初始化");

        /// <summary>
        /// 获取状态版本
        /// </summary>
        protected virtual int GetStateVersion()
        {
            // 尝试反射获取Version属性，如果存在
            var versionProperty = typeof(TState).GetProperty("Version");
            if (versionProperty != null && versionProperty.PropertyType == typeof(int) && _state?.State != null)
            {
                return (int)versionProperty.GetValue(_state.State);
            }
            return 0;
        }

        /// <summary>
        /// 批量状态更新
        /// 将多个状态更新操作合并为一个持久化操作
        /// </summary>
        protected async Task<T> ExecuteBatchStateUpdateAsync<T>(string operationName, Func<Task<T>> operation)
        {
            LogDebug($"执行批量状态更新操作: {operationName}");
            var result = await operation();
            
            // 将持久化操作加入队列
            await EnqueueStateUpdateAsync(() => PersistStateAsync());
            
            return result;
        }

        /// <summary>
        /// 执行非返回值的批量状态更新
        /// </summary>
        protected async Task ExecuteBatchStateUpdateAsync(string operationName, Func<Task> operation)
        {
            LogDebug($"执行批量状态更新操作: {operationName}");
            await operation();
            
            // 将持久化操作加入队列
            await EnqueueStateUpdateAsync(() => PersistStateAsync());
        }

        /// <summary>
        /// 将状态更新操作加入队列
        /// </summary>
        private async Task EnqueueStateUpdateAsync(Func<Task> updateOperation)
        {
            _stateUpdateQueue.Enqueue(updateOperation);
            
            // 启动批量更新处理（如果尚未运行）
            if (!_isBatchUpdateRunning)
            {
                await _batchUpdateLock.WaitAsync();
                try
                {
                    if (!_isBatchUpdateRunning)
                    {
                        _isBatchUpdateRunning = true;
                        // 在后台线程中处理批量更新
                        _ = Task.Run(ProcessBatchUpdatesAsync);
                    }
                }
                finally
                {
                    _batchUpdateLock.Release();
                }
            }
        }

        /// <summary>
        /// 处理批量更新队列
        /// </summary>
        private async Task ProcessBatchUpdatesAsync()
        {
            try
            {
                // 等待一小段时间，收集更多更新操作
                await Task.Delay(50, CancellationToken.None);
                
                // 执行所有队列中的更新操作
                var operations = new List<Func<Task>>();
                while (_stateUpdateQueue.TryDequeue(out var operation))
                {
                    operations.Add(operation);
                }
                
                if (operations.Count > 0)
                {
                    LogDebug("执行批量状态更新，操作数: {OperationCount}", operations.Count);
                    
                    // 合并为一个持久化操作（对于状态持久化，多次调用WriteStateAsync是冗余的）
                    // 我们只需要最后一次持久化即可
                    await operations.Last().Invoke();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "批量状态更新处理失败");
            }
            finally
            {
                await _batchUpdateLock.WaitAsync();
                try
                {
                    // 如果队列中还有操作，继续处理
                    if (!_stateUpdateQueue.IsEmpty)
                    {
                        _ = Task.Run(ProcessBatchUpdatesAsync);
                    }
                    else
                    {
                        _isBatchUpdateRunning = false;
                    }
                }
                finally
                {
                    _batchUpdateLock.Release();
                }
            }
        }

        /// <summary>
        /// 强制立即持久化状态（不使用批量更新）
        /// 适用于关键操作或长时间运行的事务后
        /// </summary>
        protected virtual async Task ForcePersistStateAsync()
        {
            // 确保所有队列中的操作都已完成
            await _batchUpdateLock.WaitAsync();
            try
            {
                await PersistStateAsync();
            }
            finally
            {
                _batchUpdateLock.Release();
            }
        }
        
        /// <summary>
        /// 使用断路器和重试机制执行状态更新操作
        /// </summary>
        protected async Task<T> ExecuteStateUpdateWithCircuitBreakerAsync<T>(string operationName, Func<Task<T>> operation, int maxRetries = MaxConflictRetries)
        {
            return await ExecuteWithCircuitBreakerAsync(
                operationName,
                $"{GetGrainTypeName()}-StateOperations",
                async () => await ExecuteBatchStateUpdateAsync(operationName, operation),
                maxRetries);
        }
        
        /// <summary>
        /// 执行带重试机制的状态读取操作
        /// </summary>
        protected async Task<TState> ReadStateWithRetryAsync()
        {
            int retryCount = 0;
            Exception lastException = null;
            
            while (retryCount < MaxStateLoadRetries)
            {
                try
                {
                    await _state.ReadStateAsync();
                    
                    // 验证状态完整性
                    if (ValidateStateIntegrity(_state.State))
                    {
                        LogDebug("状态读取成功: {GrainIdentity}, 版本: {StateVersion}", 
                            GetGrainIdentity(), GetStateVersion());
                        return _state.State;
                    }
                    else
                    {
                        LogWarning("状态完整性验证失败: {GrainIdentity}", GetGrainIdentity());
                        
                        // 尝试修复状态
                        var repairedState = RepairStateIntegrity(_state.State);
                        if (repairedState != null)
                        {
                            _state.State = repairedState;
                            await PersistStateAsync();
                            LogInformation("状态修复成功: {GrainIdentity}", GetGrainIdentity());
                            return _state.State;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    LogWarning("状态读取失败，正在重试 ({RetryCount}/{MaxRetries}): {GrainIdentity}, 错误: {ErrorMessage}", 
                        retryCount, MaxStateLoadRetries, GetGrainIdentity(), ex.Message);
                    
                    // 指数退避重试
                    await Task.Delay((int)(StateLoadRetryDelayMilliseconds * Math.Pow(2, retryCount - 1)), CancellationToken.None);
                }
            }
            
            LogError(lastException, "状态读取最终失败: {GrainIdentity}", GetGrainIdentity());
            
            // 作为最后的手段，创建新状态
            LogWarning("创建新状态: {GrainIdentity}", GetGrainIdentity());
            _state.State = CreateDefaultState();
            await PersistStateAsync();
            
            return _state.State;
        }
        
        /// <summary>
        /// 验证状态完整性
        /// </summary>
        protected virtual bool ValidateStateIntegrity(TState state)
        {
            // 默认实现：检查状态是否为null
            return state != null;
        }
        
        /// <summary>
        /// 修复状态完整性
        /// </summary>
        protected virtual TState RepairStateIntegrity(TState state)
        {
            // 默认实现：返回null表示无法修复
            return null;
        }
        
        /// <summary>
        /// 创建默认状态
        /// </summary>
        protected virtual TState CreateDefaultState()
        {
            return new TState();
        }
        
        /// <summary>
        /// 重写状态恢复方法
        /// </summary>
        protected override async Task RecoverStateAsync(CancellationToken cancellationToken)
        {
            LogInformation("开始恢复Grain状态: {GrainIdentity}", GetGrainIdentity());
            
            try
            {
                // 使用带重试机制的状态读取
                await ReadStateWithRetryAsync();
                
                // 执行自定义的状态恢复逻辑
                await OnStateRecoveredAsync(cancellationToken);
                
                LogInformation("Grain状态恢复成功: {GrainIdentity}", GetGrainIdentity());
            }
            catch (Exception ex)
            {
                LogError(ex, "Grain状态恢复失败: {GrainIdentity}", GetGrainIdentity());
                
                // 尝试创建新状态
                try
                {
                    _state.State = CreateDefaultState();
                    await PersistStateAsync();
                    LogInformation("创建新状态成功: {GrainIdentity}", GetGrainIdentity());
                }
                catch (Exception createEx)
                {
                    LogCritical(createEx, "无法创建新状态: {GrainIdentity}", GetGrainIdentity());
                    // 继续激活，但状态将不可用
                }
            }
            
            await base.RecoverStateAsync(cancellationToken);
        }
        
        /// <summary>
        /// 状态恢复完成后调用（可由子类重写）
        /// </summary>
        protected virtual Task OnStateRecoveredAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Orleans最佳实践：重写停用方法，自动持久化状态
        /// </summary>
        public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            LogInformation("有状态Grain停用: {GrainIdentity}, 原因: {Reason}",
                GetGrainIdentity(), reason.Description);
            
            try
            {
                // 强制立即持久化所有未提交的状态更改
                await ForcePersistStateAsync();
            }
            catch (Exception ex)
            {
                LogError(ex, "Grain停用过程中状态持久化失败: {GrainIdentity}", GetGrainIdentity());
                // 即使失败也继续停用过程
            }
            finally
            {
                // 清理资源
                _batchUpdateLock.Dispose();
                await base.OnDeactivateAsync(reason, cancellationToken);
            }
        }

        /// <summary>
        /// 性能监控和状态管理（仅用于关键操作）
        /// </summary>
        protected async Task<T> TrackPerformanceWithStateAsync<T>(string operationName, Func<Task<T>> operation)
        {
            return await TrackPerformanceAsync(operationName, async () =>
            {
                var result = await operation();
                // 使用批量更新机制
                await EnqueueStateUpdateAsync(() => PersistStateAsync());
                return result;
            });
        }
        
        // 注意：IsConflictException方法已在OrleansGrainBase中定义，不需要重复定义
    }
}