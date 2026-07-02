---
name: add-dynamodb-repository
description: Scaffolds a PostgreSQL repository with interface in Application, implementation in Infrastructure using EF Core
inclusion: manual
---

# Add PostgreSQL Repository

Creates a complete data access layer for a new entity using Entity Framework Core + PostgreSQL.

## Input Required
- Bounded context (Identity | Mentoring | Content | Engagement)
- Entity/Aggregate name (e.g., Feedback, Bookmark)
- Key operations needed (save, get, query, delete)
- Relationships to other entities (if any)

## Steps

### 1. Define interface in Application layer
`src/{Context}/GuidedMentor.{Context}.Application/Interfaces/I{Entity}Repository.cs`

```csharp
namespace GuidedMentor.{Context}.Application.Interfaces;

public interface I{Entity}Repository
{
    Task<{Entity}?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync({Entity} entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

### 2. Create EF entity (persistence model)
`src/Shared/GuidedMentor.SharedInfrastructure/Data/Entities/{Entity}Entity.cs`

```csharp
namespace GuidedMentor.SharedInfrastructure.Data.Entities;

public sealed class {Entity}Entity
{
    public Guid Id { get; set; }
    // Map all columns from PostgreSQL schema
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 3. Add to DbContext
In `GuidedMentorDbContext.cs`:
```csharp
public DbSet<{Entity}Entity> {Entities} => Set<{Entity}Entity>();
```
And add mapping in `OnModelCreating`:
```csharp
modelBuilder.Entity<{Entity}Entity>(e => {
    e.ToTable("{table_name}");
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).HasColumnName("id");
    // ... map all columns
});
```

### 4. Create PostgreSQL implementation
`src/{Context}/GuidedMentor.{Context}.Infrastructure/Repositories/Postgres{Entity}Repository.cs`

```csharp
namespace GuidedMentor.{Context}.Infrastructure.Repositories;

public sealed class Postgres{Entity}Repository : I{Entity}Repository
{
    private readonly GuidedMentorDbContext _db;

    public Postgres{Entity}Repository(GuidedMentorDbContext db) => _db = db;

    public async Task<{Entity}?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.{Entities}.FindAsync([id], ct);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task SaveAsync({Entity} entity, CancellationToken ct = default)
    {
        var existing = await _db.{Entities}.FindAsync([entity.Id], ct);
        if (existing is null)
            _db.{Entities}.Add(MapToEntity(entity));
        else
            UpdateEntity(existing, entity);
        await _db.SaveChangesAsync(ct);
    }
}
```

### 5. Add SQL migration
Add to `scripts/init-db.sql`:
```sql
CREATE TABLE {table_name} (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- columns
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_{table_name}_{key} ON {table_name}({key});
```

### 6. Register in DI
```csharp
services.AddScoped<I{Entity}Repository, Postgres{Entity}Repository>();
```

### 7. Write unit tests
- Test happy path (save + retrieve)
- Test not found (returns null)
- Test mapping between domain entity and persistence entity

### 8. Verify
`dotnet build -c Release`

## Conventions
- Interface in Application layer, implementation in Infrastructure
- EF entities are separate from Domain entities (persistence POCOs)
- Use JSONB for complex nested objects
- Use TEXT[] for list fields
- Always include created_at / updated_at
- Indexes on foreign keys and query fields
