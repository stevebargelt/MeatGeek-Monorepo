# Sessions API .NET 8 Modernization Plan

## Current Status

### ✅ **Already .NET 8 Compatible**
- All projects target `net8.0`
- Uses Azure Functions v4 runtime
- Modern async/await patterns throughout

### ⚠️ **Critical Modernization Needed**

#### 1. Azure Functions Model Migration (HIGH PRIORITY)
- Currently using **in-process model** (deprecated Nov 10, 2026)
- Should migrate to **isolated worker model** for .NET 8
- Packages needing replacement:
  - `Microsoft.Azure.Functions.Extensions` → `Microsoft.Azure.Functions.Worker`
  - `Microsoft.Azure.WebJobs.Extensions.OpenApi` (v0.7.2-preview) → `Microsoft.Azure.Functions.Worker.Extensions.OpenApi` (v1.5.1)

#### 2. Package Updates Needed
```xml
<!-- Current (In-Process) -->
<PackageReference Include="Microsoft.Azure.WebJobs.Extensions.OpenApi" Version="0.7.2-preview" />
<PackageReference Include="Microsoft.Azure.Functions.Extensions" Version="1.1.0" />

<!-- Should become (Isolated Worker) -->
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.OpenApi" Version="1.5.1" />
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.21.0" />
```

### 🚀 **Modernization Opportunities**

#### 1. JSON Serialization
- Heavy use of `Newtonsoft.Json` (24 files)
- Consider migrating to `System.Text.Json` for better performance
- `SessionSummariesConverter.cs` needs modernization

#### 2. Nullable Reference Types  
- Only enabled in 3 files
- Add `<Nullable>enable</Nullable>` to all projects

#### 3. Collection Expressions (.NET 8)
```csharp
// Current
tags: new[] { "session" }

// .NET 8 Collection Expression
tags: ["session"]
```

#### 4. Primary Constructors
- `CosmosDbConnectionString.cs` could use primary constructor pattern

#### 5. Performance Improvements
- Consider using `System.Text.Json` source generators
- Use `ReadOnlySpan<char>` for string operations where appropriate

## Migration Priority

### 1. **URGENT**: Migrate to Isolated Worker Model
**Impact**: Critical for long-term support beyond Nov 2026
**Effort**: High - requires significant code changes
**Files affected**: All Azure Function projects

**Changes required**:
- Update all `.csproj` files with new package references
- Modify `Startup.cs` to use isolated worker model dependency injection
- Update function signatures and attributes
- Replace `ILogger` injection patterns
- Update OpenAPI configuration

### 2. **HIGH**: Update OpenAPI Packages  
**Impact**: Access to latest OpenAPI features and .NET 8 compatibility
**Effort**: Medium
**Files affected**: 
- `MeatGeek.Sessions.Api.csproj`
- `MeatGeek.Sessions.WorkerApi.csproj`

### 3. **MEDIUM**: Enable Nullable Reference Types
**Impact**: Better null safety and modern C# practices
**Effort**: Medium
**Files affected**: All `.csproj` files and code files requiring null annotations

### 4. **LOW**: Modernize Collection Initialization and JSON Handling
**Impact**: Performance improvements and modern syntax
**Effort**: Low to Medium
**Files affected**: 
- All files using `new[]` syntax
- All files using `Newtonsoft.Json`

## Implementation Steps

### Phase 1: Isolated Worker Model Migration
1. Create new isolated worker projects
2. Update package references
3. Migrate dependency injection configuration
4. Update function signatures and bindings
5. Update OpenAPI configuration
6. Update unit tests
7. Test thoroughly in development environment

### Phase 2: Package Modernization
1. Update OpenAPI packages to latest stable versions
2. Enable nullable reference types across all projects
3. Add necessary null annotations

### Phase 3: Code Modernization
1. Replace collection initializations with collection expressions
2. Consider `System.Text.Json` migration for performance
3. Implement primary constructors where beneficial

## Benefits of Migration

- **Long-term Support**: Isolated worker model continues receiving updates
- **Better Performance**: Improved startup times and memory usage
- **Modern Patterns**: Access to latest .NET 8 features
- **Better Testing**: Easier unit testing with isolated worker model
- **Future-Proof**: Ready for future .NET versions

## Risks and Considerations

- **Breaking Changes**: Migration requires significant testing
- **Downtime**: May require coordinated deployment
- **Dependencies**: Ensure all shared libraries are compatible
- **Testing**: Comprehensive testing required across all scenarios

## Next Steps

1. **Plan Migration Window**: Schedule development time for isolated worker migration
2. **Create Migration Branch**: Develop changes in isolated branch
3. **Update CI/CD**: Ensure deployment pipelines support new model
4. **Comprehensive Testing**: Test all endpoints and integrations
5. **Gradual Rollout**: Consider blue-green deployment strategy