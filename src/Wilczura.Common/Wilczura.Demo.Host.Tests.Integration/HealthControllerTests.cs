using Microsoft.AspNetCore.Mvc.Testing;

namespace Wilczura.Demo.Host.Tests.Integration;

public static class HealthControllerTests
{
    public class TheGetMethod : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public TheGetMethod(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async void WhenCalledReturnsValue()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health/");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("text/plain; charset=utf-8", response?.Content?.Headers?.ContentType?.ToString());
        }
    }
}