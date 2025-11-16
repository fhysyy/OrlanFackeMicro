using FakeMicro.Interfaces;
using Orleans;

namespace FakeMicro.Grains
{
    public class HelloGrain : Grain, IHelloGrain
    {
        private string? _lastGreeting;

        public Task<string> SayHelloAsync(string greeting)
        {
            _lastGreeting = greeting;
            return Task.FromResult($"Hello, you said: '{greeting}'! My grain key is: {this.GetPrimaryKeyString()}");
        }
    }
}