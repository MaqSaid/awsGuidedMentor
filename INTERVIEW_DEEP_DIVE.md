# Deep Dive: React.js & .NET Features — Interview Q&A with Analogies

> Every feature from GuidedMentor with WHY/WHEN/WHO/WHICH/HOW + real-world analogies.

---

## SECTION A: .NET BACKEND FEATURES

---

### A1. Record Types & Immutability

**Q: What are record types and when do you use them over classes?**

**WHY:** Value-based equality + immutability + concise syntax for data carriers.
**WHEN:** Commands, DTOs, domain events, strongly-typed IDs.
**HOW (Project Example):**
```csharp
public sealed record MarkCompleteCommand(Guid SessionId, Guid UserId, Role Role) : IRequest<Result>;
public sealed record UserId(Guid Value) { public static UserId New() => new(Guid.NewGuid()); }
```

**Analogy:** Records are like passport numbers — two passports with the same number ARE the same passport. Classes are like twins — same appearance but different people.

---

### A2. Result Pattern (No-Exception Business Logic)

**Q: How do you handle business logic failures without throwing exceptions?**

**WHY:** Exceptions are expensive and meant for unexpected failures. Business rules failing is expected.
**WHEN:** Every command handler returns Result or Result of T.
**WHO:** Domain layer defines Result; handlers return it; API maps to HTTP codes.
**HOW:**
```csharp
public sealed class Result {
    public bool IsSuccess { get; }
    public string Error { get; }
    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
}
// Usage: if (session is null) return Result.Failure("Session not found.");
```

**Analogy:** Like a doctor's report — it says "healthy" or describes what's wrong. The doctor doesn't collapse (throw exception) when they find an issue.
