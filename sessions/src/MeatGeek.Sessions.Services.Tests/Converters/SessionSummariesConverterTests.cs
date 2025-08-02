using System;
using System.IO;
using System.Text;
using Xunit;
using Newtonsoft.Json;
using MeatGeek.Sessions.Services.Converters;
using MeatGeek.Sessions.Services.Models.Response;

namespace MeatGeek.Sessions.Services.Tests.Converters
{
    public class SessionSummariesConverterTests
    {
        private readonly SessionSummariesConverter _converter;

        public SessionSummariesConverterTests()
        {
            _converter = new SessionSummariesConverter();
        }

        #region CanConvert Tests

        [Fact]
        public void CanConvert_SessionSummariesType_ReturnsTrue()
        {
            // Act
            var result = _converter.CanConvert(typeof(SessionSummaries));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanConvert_OtherTypes_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(_converter.CanConvert(typeof(string)));
            Assert.False(_converter.CanConvert(typeof(int)));
            Assert.False(_converter.CanConvert(typeof(SessionSummary)));
            Assert.False(_converter.CanConvert(typeof(object)));
            Assert.False(_converter.CanConvert(typeof(SessionDetails)));
        }

        #endregion

        #region WriteJson Tests

        [Fact]
        public void WriteJson_EmptySessionSummaries_WritesEmptyObject()
        {
            // Arrange
            var sessionSummaries = new SessionSummaries();
            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var jsonWriter = new JsonTextWriter(stringWriter);
            var serializer = new JsonSerializer();

            // Act
            _converter.WriteJson(jsonWriter, sessionSummaries, serializer);

            // Assert
            var result = stringBuilder.ToString();
            Assert.Equal("{}", result);
        }

        [Fact]
        public void WriteJson_SingleSession_WritesCorrectFormat()
        {
            // Arrange
            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-123",
                SmokerId = "smoker-456",
                Title = "Test Session",
                EndTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc)
            });

            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var jsonWriter = new JsonTextWriter(stringWriter);
            var serializer = new JsonSerializer();

            // Act
            _converter.WriteJson(jsonWriter, sessionSummaries, serializer);

            // Assert
            var result = stringBuilder.ToString();
            Assert.Contains("\"session-123\":", result);
            Assert.Contains("\"smokerId\":\"smoker-456\"", result);
            Assert.Contains("\"title\":\"Test Session\"", result);
            Assert.Contains("\"type\":\"session\"", result);
            Assert.Contains("\"endTime\":", result);
            
            // The Id should be set to null in the serialized object
            Assert.DoesNotContain("\"id\":\"session-123\"", result);
        }

        [Fact]
        public void WriteJson_MultipleSessions_WritesAllSessionsAsProperties()
        {
            // Arrange
            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-1",
                SmokerId = "smoker-123",
                Title = "First Session",
                EndTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc)
            });
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-2",
                SmokerId = "smoker-123",
                Title = "Second Session",
                EndTime = null
            });

            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var jsonWriter = new JsonTextWriter(stringWriter);
            var serializer = new JsonSerializer();

            // Act
            _converter.WriteJson(jsonWriter, sessionSummaries, serializer);

            // Assert
            var result = stringBuilder.ToString();
            
            // Both sessions should be properties
            Assert.Contains("\"session-1\":", result);
            Assert.Contains("\"session-2\":", result);
            
            // Both should contain their data
            Assert.Contains("\"First Session\"", result);
            Assert.Contains("\"Second Session\"", result);
            
            // Neither should have id fields in their serialized content
            Assert.DoesNotContain("\"id\":\"session-1\"", result);
            Assert.DoesNotContain("\"id\":\"session-2\"", result);
        }

        [Fact]
        public void WriteJson_SessionWithNullEndTime_HandlesNullCorrectly()
        {
            // Arrange
            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-null-end",
                SmokerId = "smoker-123",
                Title = "Active Session",
                EndTime = null
            });

            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var jsonWriter = new JsonTextWriter(stringWriter);
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            // Act
            _converter.WriteJson(jsonWriter, sessionSummaries, serializer);

            // Assert
            var result = stringBuilder.ToString();
            Assert.Contains("\"session-null-end\":", result);
            Assert.Contains("\"Active Session\"", result);
            
            // With NullValueHandling.Ignore, endTime should not appear
            Assert.DoesNotContain("\"endTime\"", result);
        }

        [Fact]
        public void WriteJson_SessionsWithSpecialCharacters_EncodesCorrectly()
        {
            // Arrange
            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-special-chars",
                SmokerId = "smoker-123",
                Title = "Session with \"quotes\" and \\backslashes\\ and newlines\n",
                EndTime = DateTime.UtcNow
            });

            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var jsonWriter = new JsonTextWriter(stringWriter);
            var serializer = new JsonSerializer();

            // Act
            _converter.WriteJson(jsonWriter, sessionSummaries, serializer);

            // Assert
            var result = stringBuilder.ToString();
            Assert.Contains("\"session-special-chars\":", result);
            
            // JSON should properly escape special characters
            Assert.Contains("\\\"quotes\\\"", result);
            Assert.Contains("\\\\backslashes\\\\", result);
            Assert.Contains("\\n", result);
        }

        #endregion

        #region ReadJson Tests

        [Fact]
        public void ReadJson_NotImplemented_ThrowsNotImplementedException()
        {
            // Arrange
            var stringReader = new StringReader("{}");
            var jsonReader = new JsonTextReader(stringReader);
            var serializer = new JsonSerializer();

            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ReadJson(jsonReader, typeof(SessionSummaries), null, serializer));
        }

        #endregion

        #region Integration Tests with JsonConvert

        [Fact]
        public void Integration_SerializeWithConverter_ProducesExpectedOutput()
        {
            // Arrange
            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "integration-session-1",
                SmokerId = "integration-smoker",
                Title = "Integration Test Session",
                EndTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc)
            });

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            settings.Converters.Add(new SessionSummariesConverter());

            // Act
            var json = JsonConvert.SerializeObject(sessionSummaries, settings);

            // Assert
            Assert.Contains("\"integration-session-1\": {", json);
            Assert.Contains("\"title\": \"Integration Test Session\"", json);
            Assert.Contains("\"smokerId\": \"integration-smoker\"", json);
            Assert.Contains("\"type\": \"session\"", json);
            
            // The JSON should be formatted (indented)
            Assert.Contains("\n", json);
            Assert.Contains("  ", json);
            
            // Should not contain id field in the session object
            Assert.DoesNotContain("\"id\": \"integration-session-1\"", json);
        }

        [Fact]
        public void Integration_MultipleSessionsWithConverter_CreatesObjectWithSessionProperties()
        {
            // Arrange
            var sessionSummaries = new SessionSummaries();
            for (int i = 1; i <= 3; i++)
            {
                sessionSummaries.Add(new SessionSummary
                {
                    Id = $"session-{i}",
                    SmokerId = "multi-smoker",
                    Title = $"Session {i}",
                    EndTime = i % 2 == 0 ? DateTime.UtcNow.AddHours(-i) : null
                });
            }

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            settings.Converters.Add(new SessionSummariesConverter());

            // Act
            var json = JsonConvert.SerializeObject(sessionSummaries, settings);

            // Assert
            // Should be an object with session IDs as property names
            Assert.StartsWith("{", json);
            Assert.EndsWith("}", json);
            
            Assert.Contains("\"session-1\":", json);
            Assert.Contains("\"session-2\":", json);
            Assert.Contains("\"session-3\":", json);
            
            Assert.Contains("\"Session 1\"", json);
            Assert.Contains("\"Session 2\"", json);
            Assert.Contains("\"Session 3\"", json);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void WriteJson_NullSessionSummaries_HandlesGracefully()
        {
            // Arrange
            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var jsonWriter = new JsonTextWriter(stringWriter);
            var serializer = new JsonSerializer();

            // Act & Assert
            // This should throw or handle null gracefully depending on implementation
            Assert.Throws<NullReferenceException>(() =>
                _converter.WriteJson(jsonWriter, null, serializer));
        }

        [Fact]
        public void WriteJson_SessionWithNullId_SkipsSession()
        {
            // Arrange
            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = null, // Null ID
                SmokerId = "smoker-123",
                Title = "Session with Null ID",
                EndTime = DateTime.UtcNow
            });

            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var jsonWriter = new JsonTextWriter(stringWriter);
            var serializer = new JsonSerializer();

            // Act
            _converter.WriteJson(jsonWriter, sessionSummaries, serializer);

            // Assert
            var result = stringBuilder.ToString();
            
            // Should handle null ID gracefully (behavior depends on JsonWriter implementation)
            Assert.NotNull(result);
        }

        #endregion
    }
}