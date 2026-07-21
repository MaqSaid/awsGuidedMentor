---
inclusion: fileMatch
fileMatchPattern: "**/*.cs"
---

# C# 14 Modernization Patterns

When modifying `.cs` files for the react19-dotnet10-modernization spec, apply these exact patterns.

## field Keyword in Property Accessors

**Before:**
```csharp
private string _title;
public string Title
{
    get => _title;
    set => _title = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
}
```

**After:**
```csharp
public string Title
{
    get;
    set => field = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
}
```

Rules:
- Replace `get => _fieldName;` with `get;`
- Replace `set => _fieldName = expr;` with `set => field = expr;`
- Remove the `private T _fieldName;` declaration entirely
- Preserve ALL validation logic and transformations in the setter (Trim, ToLowerInvariant, null checks, range checks)
- The `field` keyword refers to the compiler-generated backing field

**DO NOT apply to:**
- Auto-properties with no explicit backing field (e.g., `public string Name { get; private set; }`)
- Computed expression-bodied getters (e.g., `public bool IsActive => !IsDisabled;`)
- Backing fields used by multiple properties (e.g., a `List<T>` exposed as both `IReadOnlyList<T>` getter and internal mutation)
- Backing fields referenced outside their own property accessor

## Extension Members (extension blocks)

**Before:**
```csharp
public static class MentorEntityExtensions
{
    public static string FormattedAvailability(this MentorEntity mentor) =>
        mentor.Availability.Status.ToString();

    public static MentorBrowseDto ToBrowseDto(this MentorEntity mentor) => new(
        mentor.Id, mentor.DisplayName, mentor.Availability);
}
```

**After:**
```csharp
extension(MentorEntity mentor)
{
    public string FormattedAvailability =>
        mentor.Availability.Status.ToString();

    public MentorBrowseDto ToBrowseDto() => new(
        mentor.Id, mentor.DisplayName, mentor.Availability);
}
```

Rules:
- Replace `public static class XExtensions` with `extension(TargetType varName)`
- Parameterless methods that return a value → convert to expression-bodied properties
- Methods with parameters → keep as methods but remove the `this TargetType` first param
- The target instance is accessed via the variable name declared in `extension(...)`
- Call-site syntax is unchanged (consumers still use `entity.Method()` dot notation)

**DO NOT convert:**
- Extensions on `IServiceCollection` (DI registration helpers)
- Extensions on `IApplicationBuilder` or `WebApplication` (middleware pipeline)
- Extensions on `IHealthChecksBuilder` (health check registration)
- Extensions on `ILoggingBuilder` or `IConfigurationBuilder`
- Any extension where the `this` parameter is NOT a domain type, entity, or value object within the same bounded context

**Mixed static classes (contains both extension methods and non-extension statics):**
- Move only the extension methods to the `extension` block
- Keep remaining static members in the original `static class`
- If the class becomes empty after migration, delete it
