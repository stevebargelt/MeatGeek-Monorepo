using System;
using FluentAssertions;
using Xunit;

namespace MeatGeek.Shared.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void Truncate_WhenValueIsNull_ShouldReturnNull()
        {
            // Arrange
            string value = null;
            int maximumLength = 10;

            // Act
            var result = value.Truncate(maximumLength);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Truncate_WhenValueIsEmpty_ShouldReturnEmpty()
        {
            // Arrange
            string value = string.Empty;
            int maximumLength = 10;

            // Act
            var result = value.Truncate(maximumLength);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Truncate_WhenValueIsShorterThanMaxLength_ShouldReturnOriginalValue()
        {
            // Arrange
            string value = "Hello";
            int maximumLength = 10;

            // Act
            var result = value.Truncate(maximumLength);

            // Assert
            result.Should().Be("Hello");
        }

        [Fact]
        public void Truncate_WhenValueIsEqualToMaxLength_ShouldReturnOriginalValue()
        {
            // Arrange
            string value = "Hello World";
            int maximumLength = 11;

            // Act
            var result = value.Truncate(maximumLength);

            // Assert
            result.Should().Be("Hello World");
        }

        [Fact]
        public void Truncate_WhenValueIsLongerThanMaxLength_ShouldTruncateWithDefaultMarker()
        {
            // Arrange
            string value = "This is a very long string that needs to be truncated";
            int maximumLength = 20;

            // Act
            var result = value.Truncate(maximumLength);

            // Assert
            result.Should().Be("This is a very lo...");
            result.Length.Should().Be(maximumLength);
        }

        [Fact]
        public void Truncate_WhenValueIsLongerThanMaxLength_WithCustomMarker_ShouldTruncateWithCustomMarker()
        {
            // Arrange
            string value = "This is a very long string that needs to be truncated";
            int maximumLength = 20;
            string customMarker = " [more]";

            // Act
            var result = value.Truncate(maximumLength, customMarker);

            // Assert
            result.Should().Be("This is a ver [more]");
            result.Length.Should().Be(maximumLength);
        }

        [Fact]
        public void Truncate_WhenMaxLengthIsEqualToContinuationMarkerLength_ShouldReturnOnlyMarker()
        {
            // Arrange
            string value = "Hello World";
            int maximumLength = 3;
            string marker = "...";

            // Act
            var result = value.Truncate(maximumLength, marker);

            // Assert
            result.Should().Be("...");
            result.Length.Should().Be(maximumLength);
        }

        [Fact]
        public void Truncate_WhenMaxLengthIsLessThanContinuationMarkerLength_ShouldThrowException()
        {
            // Arrange
            string value = "Hello World";
            int maximumLength = 2;
            string marker = "...";

            // Act & Assert
            Action act = () => value.Truncate(maximumLength, marker);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData("", 5, "")]
        [InlineData("Hi", 5, "Hi")]
        [InlineData("Hello", 5, "Hello")]
        [InlineData("Hello World", 5, "He...")]
        [InlineData("Test string with more content", 15, "Test string ...")]
        public void Truncate_WithVariousInputs_ShouldProduceExpectedResults(string input, int maxLength, string expected)
        {
            // Act
            var result = input.Truncate(maxLength);

            // Assert
            result.Should().Be(expected);
        }
    }
}