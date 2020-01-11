using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Xunit;

namespace FplBot.WebApi.Tests
{
    public class RedisIntegrationTests
    {
        [Fact()]
        public async Task CanConnect()
        {
            var connection = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("REDIS_URL"));
            Assert.True(connection.IsConnected);
            await connection.CloseAsync();
        }
    }
}