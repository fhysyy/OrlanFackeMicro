using FakeMicro.Interfaces;
using Orleans;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Grains
{
    public class CounterGrain : OrleansGrainBase, ICounterGrain
    {
        private int _count = 0;

        public CounterGrain(ILogger<CounterGrain> logger) : base(logger)
        {
        }

        public Task<int> IncrementAsync()
        {
            _count++;
            return Task.FromResult(_count);
        }

        public Task<int> DecrementAsync()
        {
            _count--;
            return Task.FromResult(_count);
        }

        public Task<int> GetCountAsync()
        {
            return Task.FromResult(_count);
        }

        public Task ResetAsync()
        {
            _count = 0;
            return Task.CompletedTask;
        }
    }
}