using FakeMicro.Interfaces;
using Orleans;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Grains
{
    public class HelloGrain : OrleansGrainBase, IHelloGrain
    {
        private string? _lastGreeting;

        public HelloGrain(ILogger<HelloGrain> logger) : base(logger)
        {
        }

        public Task<string> SayHelloAsync(string greeting)
        {
            _lastGreeting = greeting;
            return Task.FromResult($"Hello, you said: '{greeting}'! My grain key is: {this.GetPrimaryKeyString()}");
        }
    }
}