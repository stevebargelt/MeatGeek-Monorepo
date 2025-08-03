# MeatGeek Shared Library

The Shared Library provides common utilities, event schemas, and shared functionality used across all MeatGeek microservices. This .NET 8.0 library ensures consistency and reduces code duplication throughout the platform.

## Features

- **Event Grid Integration**: Publisher and subscriber services for Event Grid
- **Event Schemas**: Standardized event data models for session lifecycle
- **String Utilities**: Common string manipulation and validation functions
- **Event Types**: Centralized event type definitions
- **Cross-Service Models**: Shared data models and utilities

## Nx Development Commands

```bash
# Build Shared Library
nx build MeatGeek.Shared

# Run unit tests
nx test MeatGeek.Shared.Tests

# Lint/format code
nx lint MeatGeek.Shared
```

## Components

### Event Grid Services
- **EventGridPublisherService**: Publishes events to Azure Event Grid
- **EventGridSubscriberService**: Handles incoming Event Grid events
- **EventGridEvent**: Wrapper for Event Grid event structure

### Event Schemas
Standardized event data models for:
- **SessionCreatedEventData**: New session creation events
- **SessionUpdatedEventData**: Session modification events  
- **SessionEndedEventData**: Session completion events
- **SessionDeletedEventData**: Session deletion events

### Utilities
- **StringExtensions**: String manipulation and validation methods
- **EventTypes**: Centralized event type constants

## Usage in Projects

Add reference to the shared library in your project:

```xml
<ProjectReference Include="..\..\shared\src\MeatGeek.Shared\MeatGeek.Shared.csproj" />
```

### Event Publishing Example
```csharp
var publisher = new EventGridPublisherService(topicEndpoint, accessKey);
var eventData = new SessionCreatedEventData { SessionId = "123", Name = "BBQ Session" };
await publisher.PublishAsync("session.created", eventData);
```

### Event Subscription Example  
```csharp
var subscriber = new EventGridSubscriberService();
var events = await subscriber.DeserializeEventsAsync(requestBody);
```

## Testing

The shared library includes comprehensive unit tests covering:
- Event Grid publisher functionality
- Event Grid subscriber operations
- Event schema validation
- String extension methods
- Event type definitions

Run tests with: `nx test MeatGeek.Shared.Tests`

## NuGet Package

The shared library is packaged as a NuGet package for consumption by other projects:
- Automated versioning based on build number
- Published to internal NuGet feed
- Referenced by all MeatGeek microservices

## Dependencies

- Microsoft.Extensions.Logging (8.0.1)
- Microsoft.NET.Sdk.Functions (4.6.0)
- Newtonsoft.Json (for JSON serialization)

## Contributing

When adding new shared functionality:
1. Add appropriate unit tests
2. Update this README if adding new components
3. Ensure backward compatibility
4. Follow established patterns and naming conventions