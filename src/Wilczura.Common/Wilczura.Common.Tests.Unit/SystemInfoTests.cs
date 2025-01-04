using FluentAssertions;

namespace Wilczura.Common.Tests.Unit;

public static class SystemInfoTests
{
    public class GetInfo
    {
        [Fact]
        public void WhenCalledThenInformationIsReturned()
        {
            // Arrange
            var expectedResult = "testhost | 15.0.0.0";

            // Act
            var result = SystemInfo.GetInfo();

            // Assert
            result.Should().Be(expectedResult);
        }
    }
}