using FakeMicro.Interfaces;
using Orleans;

namespace FakeMicro.Grains
{
    public class CounterGrain : Grain, ICounterGrain
    {
        private int _count = 0;

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