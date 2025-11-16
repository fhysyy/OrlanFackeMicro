using System;
using System.Threading;

namespace FakeMicro.Utilities;

/// <summary>
/// 雪花ID生成器
/// 基于Twitter Snowflake算法，生成64位唯一ID
/// 结构：1位符号位 + 41位时间戳 + 10位机器ID + 12位序列号
/// </summary>
public class SnowflakeIdGenerator
{
    private const long EPOCH = 1288834974657L; // Twitter Snowflake epoch (2010-11-04 01:42:54 UTC)
    private const int TIMESTAMP_BITS = 41;
    private const int MACHINE_ID_BITS = 10;
    private const int SEQUENCE_BITS = 12;
    
    private const long MAX_MACHINE_ID = (1L << MACHINE_ID_BITS) - 1;
    private const long MAX_SEQUENCE = (1L << SEQUENCE_BITS) - 1;
    
    private const int MACHINE_ID_SHIFT = SEQUENCE_BITS;
    private const int TIMESTAMP_SHIFT = SEQUENCE_BITS + MACHINE_ID_BITS;
    
    private readonly long _machineId;
    private long _lastTimestamp = -1L;
    private long _sequence = 0L;
    private readonly object _lock = new object();
    
    /// <summary>
    /// 初始化雪花ID生成器
    /// </summary>
    /// <param name="machineId">机器ID (0-1023)</param>
    public SnowflakeIdGenerator(long machineId = 0)
    {
        if (machineId < 0 || machineId > MAX_MACHINE_ID)
        {
            throw new ArgumentException($"Machine ID must be between 0 and {MAX_MACHINE_ID}");
        }
        _machineId = machineId;
    }
    
    /// <summary>
    /// 生成下一个雪花ID
    /// </summary>
    /// <returns>64位雪花ID</returns>
    public long NextId()
    {
        lock (_lock)
        {
            var timestamp = GetCurrentTimestamp();
            
            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException("Clock moved backwards. Refusing to generate ID.");
            }
            
            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MAX_SEQUENCE;
                if (_sequence == 0)
                {
                    timestamp = WaitNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0L;
            }
            
            _lastTimestamp = timestamp;
            
            return ((timestamp - EPOCH) << TIMESTAMP_SHIFT)
                   | (_machineId << MACHINE_ID_SHIFT)
                   | _sequence;
        }
    }
    
    /// <summary>
    /// 获取当前时间戳（毫秒）
    /// </summary>
    private static long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    
    /// <summary>
    /// 等待下一毫秒
    /// </summary>
    private static long WaitNextMillis(long lastTimestamp)
    {
        var timestamp = GetCurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            Thread.Sleep(1);
            timestamp = GetCurrentTimestamp();
        }
        return timestamp;
    }
    
    /// <summary>
    /// 从雪花ID解析时间戳
    /// </summary>
    public static DateTime GetTimestampFromId(long id)
    {
        var timestamp = (id >> TIMESTAMP_SHIFT) + EPOCH;
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
    }
    
    /// <summary>
    /// 从雪花ID解析机器ID
    /// </summary>
    public static long GetMachineIdFromId(long id)
    {
        return (id >> MACHINE_ID_SHIFT) & MAX_MACHINE_ID;
    }
    
    /// <summary>
    /// 从雪花ID解析序列号
    /// </summary>
    public static long GetSequenceFromId(long id)
    {
        return id & MAX_SEQUENCE;
    }
}