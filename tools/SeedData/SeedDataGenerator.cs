using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Bogus;
using GuidedMentor.SharedTestUtils;

namespace GuidedMentor.Tools.SeedData;

/// <summary>
/// Orchestrates the creation of seed data across all DynamoDB tables.
/// Idempotent: checks for a seed marker record before creating any data.
/// Rejects production environment to prevent accidental data pollution.
/// </summary>
public sealed class SeedDataGenerator
{
    private const string SeedMarkerTableName = "GuidedMentor-SeedMarkers";
    private const string SeedMarkerPartitionKey = "SEED_MARKER";
    private const string SeedMarkerSortKey = "v1";

    private readonly IAmazonDynamoDB _dynamoDbClient;
    private string _tablePrefix = null!;

    // Deterministic seed for idempotent generation
    private const int FakerSeed = 42;

    public SeedDataGenerator(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
    }

    /// <summary>
    /// Seeds all DynamoDB tables with realistic demo data.
    /// </summary>
    public async Task SeedAsync(string environment)
    {
        RejectProductionEnvironment(environment);
        _tablePrefix = $"{environment}-guidedmentor";

        if (await SeedMarkerExistsAsync())
        {
            Console.WriteLine("✓ Seed marker found — data already seeded. Skipping.");
            Console.WriteLine("  To re-seed, delete the seed marker record from the SeedMarkers table.");
            return;
        }

        Console.WriteLine("No seed marker found. Beginning data seeding...");
        Console.WriteLine();

        // Phase 1: Create admin and special accounts
        Console.WriteLine("[1/10] Creating Super Admin account...");
        var adminUserId = await CreateSuperAdminAsync();

        // Phase 2: Create Chapter Leads
        Console.WriteLine("[2/10] Creating Chapter Lead accounts...");
        var chapterLeadIds = await CreateChapterLeadsAsync();

        // Phase 3: Create Mentors
        Console.WriteLine("[3/10] Creating Mentor profiles (20)...");
        var mentorIds = await CreateMentorsAsync(count: 20);

        // Phase 4: Create Mentees
        Console.WriteLine("[4/10] Creating Mentee profiles (30)...");
        var menteeIds = await CreateMenteesAsync(count: 30);

        // Phase 5: Create Sessions
        Console.WriteLine("[5/10] Creating Sessions (15 active, 5 completed, 3 pending, 2 unresolved)...");
        var meetupEventIds = await CreateSessionsAsync(mentorIds, menteeIds);

        // Phase 6: Create Job Postings
        Console.WriteLine("[6/10] Creating Job Postings (10 active, 3 expired, 2 archived)...");
        await CreateJobPostingsAsync(mentorIds);

        // Phase 7: Create Meetup Events
        Console.WriteLine("[7/10] Creating Meetup Events (5 upcoming, 2 past, 3 sessions aligned)...");
        await CreateMeetupEventsAsync(chapterLeadIds, meetupEventIds);

        // Phase 8: Create Notifications
        Console.WriteLine("[8/10] Creating Notifications (50)...");
        await CreateNotificationsAsync(mentorIds, menteeIds);

        // Phase 9: Create Dual-Role User
        Console.WriteLine("[9/10] Creating Dual-Role user (role toggle demo)...");
        await CreateDualRoleUserAsync();

        // Phase 10: Write seed marker
        Console.WriteLine("[10/10] Writing seed marker...");
        await WriteSeedMarkerAsync(environment);

        Console.WriteLine();
        Console.WriteLine("✅ Seed data created successfully!");
        Console.WriteLine($"   Environment: {environment}");
        Console.WriteLine($"   Timestamp: {DateTime.UtcNow:O}");
    }

    /// <summary>
    /// Throws if the environment is production to prevent accidental data pollution.
    /// </summary>
    internal static void RejectProductionEnvironment(string environment)
    {
        var normalized = environment.Trim().ToLowerInvariant();

        if (normalized is "prod" or "production")
        {
            throw new InvalidOperationException(
                $"Cannot seed production environment! Environment '{environment}' is not allowed. " +
                "The seed data generator is only intended for dev, staging, or demo environments.");
        }
    }

    /// <summary>
    /// Checks whether the seed marker already exists in DynamoDB.
    /// </summary>
    internal async Task<bool> SeedMarkerExistsAsync()
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = SeedMarkerTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["pk"] = new AttributeValue { S = SeedMarkerPartitionKey },
                    ["sk"] = new AttributeValue { S = SeedMarkerSortKey }
                },
                ConsistentRead = true
            });

            return response.Item is not null && response.Item.Count > 0;
        }
        catch (ResourceNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Writes the seed marker record to indicate successful seeding.
    /// </summary>
    private async Task WriteSeedMarkerAsync(string environment)
    {
        await _dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = SeedMarkerTableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue { S = SeedMarkerPartitionKey },
                ["sk"] = new AttributeValue { S = SeedMarkerSortKey },
                ["seededAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                ["environment"] = new AttributeValue { S = environment },
                ["version"] = new AttributeValue { S = "1.0.0" }
            }
        });
    }

    // =========================================================================
    // Super Admin
    // =========================================================================

    private async Task<string> CreateSuperAdminAsync()
    {
        var userId = "user-admin-00000000";

        await _dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = $"{_tablePrefix}-users",
            Item = new Dictionary<string, AttributeValue>
            {
                ["userId"] = new AttributeValue { S = userId },
                ["email"] = new AttributeValue { S = "admin@guidedmentor.dev" },
                ["displayName"] = new AttributeValue { S = "Platform Admin" },
                ["activeRole"] = new AttributeValue { S = "SuperAdmin" },
                ["isSuperAdmin"] = new AttributeValue { BOOL = true },
                ["mfaEnabled"] = new AttributeValue { BOOL = true },
                ["mentorOnboardingStatus"] = new AttributeValue { S = "NotStarted" },
                ["menteeOnboardingStatus"] = new AttributeValue { S = "NotStarted" },
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
            }
        });

        Console.WriteLine("   → Super Admin: admin@guidedmentor.dev (MFA enabled)");
        return userId;
    }

    // =========================================================================
    // Chapter Leads
    // =========================================================================

    private async Task<List<string>> CreateChapterLeadsAsync()
    {
        var chapterLeads = new[]
        {
            new { UserId = "user-chapterlead-sydney-001", Email = "lead.sydney@guidedmentor.dev",
                  DisplayName = "Sarah Mitchell", Chapter = "Sydney" },
            new { UserId = "user-chapterlead-melb-002", Email = "lead.melbourne@guidedmentor.dev",
                  DisplayName = "James Chen", Chapter = "Melbourne" }
        };

        var ids = new List<string>();

        foreach (var lead in chapterLeads)
        {
            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = $"{_tablePrefix}-users",
                Item = new Dictionary<string, AttributeValue>
                {
                    ["userId"] = new AttributeValue { S = lead.UserId },
                    ["email"] = new AttributeValue { S = lead.Email },
                    ["displayName"] = new AttributeValue { S = lead.DisplayName },
                    ["activeRole"] = new AttributeValue { S = "Mentor" },
                    ["isChapterLead"] = new AttributeValue { BOOL = true },
                    ["chapter"] = new AttributeValue { S = lead.Chapter },
                    ["mentorOnboardingStatus"] = new AttributeValue { S = "Completed" },
                    ["menteeOnboardingStatus"] = new AttributeValue { S = "NotStarted" },
                    ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
                }
            });

            ids.Add(lead.UserId);
            Console.WriteLine($"   → Chapter Lead: {lead.DisplayName} ({lead.Chapter})");
        }

        return ids;
    }

    // =========================================================================
    // Mentors
    // =========================================================================

    private async Task<List<string>> CreateMentorsAsync(int count)
    {
        var faker = AustralianFakers.MentorFaker.UseSeed(FakerSeed);
        var mentors = faker.Generate(count);
        var mentorIds = new List<string>();

        var batchItems = new List<WriteRequest>();

        for (var i = 0; i < mentors.Count; i++)
        {
            var mentor = mentors[i];
            var mentorId = $"mentor-{i:D4}";
            var userId = $"user-mentor-{i:D4}";
            mentorIds.Add(mentorId);

            // Write user record
            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = $"{_tablePrefix}-users",
                Item = new Dictionary<string, AttributeValue>
                {
                    ["userId"] = new AttributeValue { S = userId },
                    ["email"] = new AttributeValue { S = mentor.Email },
                    ["displayName"] = new AttributeValue { S = mentor.DisplayName },
                    ["activeRole"] = new AttributeValue { S = "Mentor" },
                    ["mentorOnboardingStatus"] = new AttributeValue { S = "Completed" },
                    ["menteeOnboardingStatus"] = new AttributeValue { S = "NotStarted" },
                    ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
                }
            });

            // Build mentor record
            var mentorItem = new Dictionary<string, AttributeValue>
            {
                ["mentorId"] = new AttributeValue { S = mentorId },
                ["userId"] = new AttributeValue { S = userId },
                ["displayName"] = new AttributeValue { S = mentor.DisplayName },
                ["email"] = new AttributeValue { S = mentor.Email },
                ["chapter"] = new AttributeValue { S = mentor.Chapter.ToString() },
                ["city"] = new AttributeValue { S = mentor.City },
                ["professionalTitle"] = new AttributeValue { S = mentor.ProfessionalTitle },
                ["companyName"] = new AttributeValue { S = mentor.CompanyName },
                ["bio"] = new AttributeValue { S = mentor.Bio },
                ["expertiseAreas"] = new AttributeValue { L = mentor.ExpertiseAreas.Select(e => new AttributeValue { S = e }).ToList() },
                ["certifications"] = new AttributeValue { L = mentor.Certifications.Select(c => new AttributeValue { S = c }).ToList() },
                ["topics"] = new AttributeValue { L = mentor.Topics.Select(t => new AttributeValue { S = t }).ToList() },
                ["yearsOfExperience"] = new AttributeValue { N = mentor.YearsOfExperience.ToString() },
                ["maxMentees"] = new AttributeValue { N = mentor.MaxMentees.ToString() },
                ["activeMenteeCount"] = new AttributeValue { N = mentor.ActiveMenteeCount.ToString() },
                ["isAvailable"] = new AttributeValue { S = "true" },
                ["availabilityStatus"] = new AttributeValue { S = "Available" },
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
            };

            // Make one mentor unavailable with a return date (Requirement 33.2 variant)
            if (i == 19) // Last mentor is unavailable
            {
                mentorItem["isAvailable"] = new AttributeValue { S = "false" };
                mentorItem["availabilityStatus"] = new AttributeValue { S = "Unavailable" };
                mentorItem["unavailabilityReason"] = new AttributeValue { S = "Vacation" };
                mentorItem["returnDate"] = new AttributeValue { S = DateTime.UtcNow.AddDays(14).ToString("O") };
                mentorItem["unavailableSince"] = new AttributeValue { S = DateTime.UtcNow.AddDays(-7).ToString("O") };
            }

            batchItems.Add(new WriteRequest
            {
                PutRequest = new PutRequest { Item = mentorItem }
            });

            // Flush batch every 25 items (DynamoDB limit)
            if (batchItems.Count == 25 || i == mentors.Count - 1)
            {
                await _dynamoDbClient.BatchWriteItemAsync(new BatchWriteItemRequest
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        [$"{_tablePrefix}-mentors"] = new List<WriteRequest>(batchItems)
                    }
                });
                batchItems.Clear();
            }
        }

        Console.WriteLine($"   → {count} mentors created (1 unavailable with return date)");
        return mentorIds;
    }

    // =========================================================================
    // Mentees
    // =========================================================================

    private async Task<List<string>> CreateMenteesAsync(int count)
    {
        var faker = AustralianFakers.MenteeFaker.UseSeed(FakerSeed + 1);
        var mentees = faker.Generate(count);
        var menteeIds = new List<string>();

        var batchItems = new List<WriteRequest>();

        for (var i = 0; i < mentees.Count; i++)
        {
            var mentee = mentees[i];
            var menteeId = $"mentee-{i:D4}";
            var userId = $"user-mentee-{i:D4}";
            menteeIds.Add(menteeId);

            // Write user record
            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = $"{_tablePrefix}-users",
                Item = new Dictionary<string, AttributeValue>
                {
                    ["userId"] = new AttributeValue { S = userId },
                    ["email"] = new AttributeValue { S = mentee.Email },
                    ["displayName"] = new AttributeValue { S = mentee.DisplayName },
                    ["activeRole"] = new AttributeValue { S = "Mentee" },
                    ["mentorOnboardingStatus"] = new AttributeValue { S = "NotStarted" },
                    ["menteeOnboardingStatus"] = new AttributeValue { S = "Completed" },
                    ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
                }
            });

            // Build mentee record
            var menteeItem = new Dictionary<string, AttributeValue>
            {
                ["menteeId"] = new AttributeValue { S = menteeId },
                ["userId"] = new AttributeValue { S = userId },
                ["displayName"] = new AttributeValue { S = mentee.DisplayName },
                ["email"] = new AttributeValue { S = mentee.Email },
                ["chapter"] = new AttributeValue { S = mentee.Chapter.ToString() },
                ["city"] = new AttributeValue { S = mentee.City },
                ["skills"] = new AttributeValue { L = mentee.Skills.Select(s => new AttributeValue { S = s }).ToList() },
                ["experienceLevel"] = new AttributeValue { S = mentee.ExperienceLevel.ToString() },
                ["yearsOfExperience"] = new AttributeValue { N = mentee.YearsOfExperience.ToString() },
                ["primaryGoal"] = new AttributeValue { S = mentee.PrimaryGoal.ToString() },
                ["goalDescription"] = new AttributeValue { S = mentee.GoalDescription },
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
            };

            batchItems.Add(new WriteRequest
            {
                PutRequest = new PutRequest { Item = menteeItem }
            });

            // Flush batch every 25 items (DynamoDB limit)
            if (batchItems.Count == 25 || i == mentees.Count - 1)
            {
                await _dynamoDbClient.BatchWriteItemAsync(new BatchWriteItemRequest
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        [$"{_tablePrefix}-mentees"] = new List<WriteRequest>(batchItems)
                    }
                });
                batchItems.Clear();
            }
        }

        Console.WriteLine($"   → {count} mentees created");
        return menteeIds;
    }

    // =========================================================================
    // Sessions
    // =========================================================================

    /// <summary>
    /// Creates sessions with relationships to mentors and mentees.
    /// Returns meetup-aligned session IDs for later linking with meetup events.
    /// </summary>
    private async Task<List<string>> CreateSessionsAsync(List<string> mentorIds, List<string> menteeIds)
    {
        var random = new Random(FakerSeed + 2);
        var sessionBatch = new List<WriteRequest>();
        var meetupAlignedSessionIds = new List<string>();
        var sessionIndex = 0;

        // 15 Active sessions with various checklist completion (10-90%)
        for (var i = 0; i < 15; i++)
        {
            var checklistPercent = 10 + (i * 80 / 14); // Spread from 10% to 90%
            var sessionId = $"session-active-{sessionIndex:D4}";
            var mentorId = mentorIds[i % mentorIds.Count];
            var menteeId = menteeIds[i % menteeIds.Count];
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(7, 60));

            var item = BuildSessionItem(sessionId, mentorId, menteeId, "Active", createdAt, checklistPercent);
            sessionBatch.Add(new WriteRequest { PutRequest = new PutRequest { Item = item } });
            sessionIndex++;
        }

        // 5 Completed sessions
        for (var i = 0; i < 5; i++)
        {
            var sessionId = $"session-completed-{sessionIndex:D4}";
            var mentorId = mentorIds[(i + 5) % mentorIds.Count];
            var menteeId = menteeIds[(i + 15) % menteeIds.Count];
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(30, 90));

            var item = BuildSessionItem(sessionId, mentorId, menteeId, "Completed", createdAt, 100);
            item["menteeCompletedAt"] = new AttributeValue { S = createdAt.AddDays(20).ToString("O") };
            item["mentorCompletedAt"] = new AttributeValue { S = createdAt.AddDays(22).ToString("O") };
            sessionBatch.Add(new WriteRequest { PutRequest = new PutRequest { Item = item } });
            sessionIndex++;
        }

        // 3 Pending sessions (awaiting mentor acceptance)
        for (var i = 0; i < 3; i++)
        {
            var sessionId = $"session-pending-{sessionIndex:D4}";
            var mentorId = mentorIds[(i + 10) % mentorIds.Count];
            var menteeId = menteeIds[(i + 20) % menteeIds.Count];
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(1, 5));

            var item = BuildSessionItem(sessionId, mentorId, menteeId, "PendingAcceptance", createdAt, 0);
            sessionBatch.Add(new WriteRequest { PutRequest = new PutRequest { Item = item } });
            sessionIndex++;
        }

        // 2 Unresolved sessions (escalated)
        for (var i = 0; i < 2; i++)
        {
            var sessionId = $"session-unresolved-{sessionIndex:D4}";
            var mentorId = mentorIds[(i + 15) % mentorIds.Count];
            var menteeId = menteeIds[(i + 25) % menteeIds.Count];
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(20, 40));

            var item = BuildSessionItem(sessionId, mentorId, menteeId, "Unresolved", createdAt, 60);
            item["menteeCompletedAt"] = new AttributeValue { S = createdAt.AddDays(14).ToString("O") };
            sessionBatch.Add(new WriteRequest { PutRequest = new PutRequest { Item = item } });
            sessionIndex++;
        }

        // 3 Sessions aligned to meetup events
        for (var i = 0; i < 3; i++)
        {
            var sessionId = $"session-meetup-{sessionIndex:D4}";
            var mentorId = mentorIds[(i + 2) % mentorIds.Count];
            var menteeId = menteeIds[(i + 5) % menteeIds.Count];
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(3, 10));

            var item = BuildSessionItem(sessionId, mentorId, menteeId, "Active", createdAt, 25 + (i * 20));
            item["alignedToMeetup"] = new AttributeValue { BOOL = true };
            item["meetupEventId"] = new AttributeValue { S = $"meetup-upcoming-{i:D4}" };
            sessionBatch.Add(new WriteRequest { PutRequest = new PutRequest { Item = item } });
            meetupAlignedSessionIds.Add($"meetup-upcoming-{i:D4}");
            sessionIndex++;
        }

        // Write all sessions in batches of 25
        for (var batchStart = 0; batchStart < sessionBatch.Count; batchStart += 25)
        {
            var batch = sessionBatch.Skip(batchStart).Take(25).ToList();
            await _dynamoDbClient.BatchWriteItemAsync(new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [$"{_tablePrefix}-sessions"] = batch
                }
            });
        }

        Console.WriteLine($"   → 15 active, 5 completed, 3 pending, 2 unresolved, 3 meetup-aligned");
        return meetupAlignedSessionIds;
    }

    private static Dictionary<string, AttributeValue> BuildSessionItem(
        string sessionId, string mentorId, string menteeId,
        string status, DateTime createdAt, int checklistPercent)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["sessionId"] = new AttributeValue { S = sessionId },
            ["mentorId"] = new AttributeValue { S = mentorId },
            ["menteeId"] = new AttributeValue { S = menteeId },
            ["status"] = new AttributeValue { S = status },
            ["checklistPercent"] = new AttributeValue { N = checklistPercent.ToString() },
            ["lockId"] = new AttributeValue { S = $"lock-{sessionId}" },
            ["createdAt"] = new AttributeValue { S = createdAt.ToString("O") },
            ["updatedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
        };
    }

    // =========================================================================
    // Job Postings
    // =========================================================================

    private async Task CreateJobPostingsAsync(List<string> mentorIds)
    {
        var faker = AustralianFakers.OpportunityFaker.UseSeed(FakerSeed + 3);
        var opportunities = faker.Generate(15);
        var batchItems = new List<WriteRequest>();

        for (var i = 0; i < opportunities.Count; i++)
        {
            var opp = opportunities[i];
            var postingId = $"posting-{i:D4}";
            var mentorId = mentorIds[i % mentorIds.Count];

            string status;
            DateTime publishedAt;
            DateTime expiresAt;

            if (i < 10)
            {
                // 10 active postings
                status = "Active";
                publishedAt = DateTime.UtcNow.AddDays(-new Random(FakerSeed + i).Next(1, 20));
                expiresAt = publishedAt.AddDays(30);
            }
            else if (i < 13)
            {
                // 3 expired postings
                status = "Expired";
                publishedAt = DateTime.UtcNow.AddDays(-45);
                expiresAt = DateTime.UtcNow.AddDays(-15);
            }
            else
            {
                // 2 archived postings
                status = "Archived";
                publishedAt = DateTime.UtcNow.AddDays(-20);
                expiresAt = DateTime.UtcNow.AddDays(10);
            }

            var item = new Dictionary<string, AttributeValue>
            {
                ["postingId"] = new AttributeValue { S = postingId },
                ["mentorId"] = new AttributeValue { S = mentorId },
                ["title"] = new AttributeValue { S = opp.Title },
                ["type"] = new AttributeValue { S = "Job" },
                ["organisationName"] = new AttributeValue { S = opp.OrganisationName },
                ["description"] = new AttributeValue { S = opp.Description },
                ["location"] = new AttributeValue { S = opp.Location },
                ["requiredSkills"] = new AttributeValue { L = opp.RequiredSkills.Select(s => new AttributeValue { S = s }).ToList() },
                ["requiredExperience"] = new AttributeValue { S = "Intermediate" },
                ["externalUrl"] = new AttributeValue { S = opp.ExternalUrl },
                ["status"] = new AttributeValue { S = status },
                ["publishedAt"] = new AttributeValue { S = publishedAt.ToString("O") },
                ["expiresAt"] = new AttributeValue { S = expiresAt.ToString("O") }
            };

            batchItems.Add(new WriteRequest { PutRequest = new PutRequest { Item = item } });
        }

        // Write in batches of 25
        for (var batchStart = 0; batchStart < batchItems.Count; batchStart += 25)
        {
            var batch = batchItems.Skip(batchStart).Take(25).ToList();
            await _dynamoDbClient.BatchWriteItemAsync(new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [$"{_tablePrefix}-opportunities"] = batch
                }
            });
        }

        Console.WriteLine("   → 10 active, 3 expired, 2 archived job postings");
    }

    // =========================================================================
    // Meetup Events
    // =========================================================================

    private async Task CreateMeetupEventsAsync(List<string> chapterLeadIds, List<string> meetupAlignedIds)
    {
        var faker = AustralianFakers.MeetupEventFaker.UseSeed(FakerSeed + 4);
        var chapters = new[] { "Sydney", "Melbourne", "Brisbane", "Perth", "Adelaide" };
        var batchItems = new List<WriteRequest>();

        // 5 upcoming meetup events across different chapters
        for (var i = 0; i < 5; i++)
        {
            var meetupId = $"meetup-upcoming-{i:D4}";
            var eventData = faker.Generate();
            var eventDate = DateTime.UtcNow.AddDays(7 + (i * 7));
            var createdBy = chapterLeadIds[i % chapterLeadIds.Count];

            var item = new Dictionary<string, AttributeValue>
            {
                ["meetupEventId"] = new AttributeValue { S = meetupId },
                ["chapter"] = new AttributeValue { S = chapters[i] },
                ["title"] = new AttributeValue { S = $"AWS {chapters[i]} Meetup - {eventData.Title}" },
                ["eventDate"] = new AttributeValue { S = eventDate.ToString("O") },
                ["startTime"] = new AttributeValue { S = "18:00" },
                ["endTime"] = new AttributeValue { S = "20:00" },
                ["venueName"] = new AttributeValue { S = eventData.Location.Split(',')[0] },
                ["venueAddress"] = new AttributeValue { S = eventData.Location },
                ["eventUrl"] = new AttributeValue { S = $"https://meetup.com/aws-{chapters[i].ToLower()}/{meetupId}" },
                ["createdBy"] = new AttributeValue { S = createdBy },
                ["isCancelled"] = new AttributeValue { BOOL = false },
                ["maxAttendees"] = new AttributeValue { N = eventData.MaxAttendees.ToString() },
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
            };

            // Add confirmed attendees for the first 3 (aligned to sessions)
            if (i < 3)
            {
                item["confirmedAttendees"] = new AttributeValue
                {
                    L = new List<AttributeValue>
                    {
                        new AttributeValue { S = $"mentor-{i:D4}" },
                        new AttributeValue { S = $"mentor-{(i + 1):D4}" }
                    }
                };
            }

            batchItems.Add(new WriteRequest { PutRequest = new PutRequest { Item = item } });
        }

        // 2 past meetup events
        for (var i = 0; i < 2; i++)
        {
            var meetupId = $"meetup-past-{i:D4}";
            var eventData = faker.Generate();
            var eventDate = DateTime.UtcNow.AddDays(-14 - (i * 14));
            var createdBy = chapterLeadIds[i % chapterLeadIds.Count];

            var item = new Dictionary<string, AttributeValue>
            {
                ["meetupEventId"] = new AttributeValue { S = meetupId },
                ["chapter"] = new AttributeValue { S = chapters[i] },
                ["title"] = new AttributeValue { S = $"AWS {chapters[i]} Meetup (Past) - {eventData.Title}" },
                ["eventDate"] = new AttributeValue { S = eventDate.ToString("O") },
                ["startTime"] = new AttributeValue { S = "18:00" },
                ["endTime"] = new AttributeValue { S = "20:00" },
                ["venueName"] = new AttributeValue { S = eventData.Location.Split(',')[0] },
                ["venueAddress"] = new AttributeValue { S = eventData.Location },
                ["eventUrl"] = new AttributeValue { S = $"https://meetup.com/aws-{chapters[i].ToLower()}/{meetupId}" },
                ["createdBy"] = new AttributeValue { S = createdBy },
                ["isCancelled"] = new AttributeValue { BOOL = false },
                ["maxAttendees"] = new AttributeValue { N = eventData.MaxAttendees.ToString() },
                ["createdAt"] = new AttributeValue { S = eventDate.AddDays(-7).ToString("O") }
            };

            batchItems.Add(new WriteRequest { PutRequest = new PutRequest { Item = item } });
        }

        // Write all meetup events
        await _dynamoDbClient.BatchWriteItemAsync(new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                [$"{_tablePrefix}-meetups"] = batchItems
            }
        });

        Console.WriteLine("   → 5 upcoming (with attendees), 2 past meetup events");
    }

    // =========================================================================
    // Notifications
    // =========================================================================

    private async Task CreateNotificationsAsync(List<string> mentorIds, List<string> menteeIds)
    {
        var notificationTypes = new[] { "RequestSent", "RequestAccepted", "RequestDeclined", "SessionPlanReady", "CompletionMarked", "Reminder" };
        var messages = new Dictionary<string, string>
        {
            ["RequestSent"] = "You have a new mentorship request.",
            ["RequestAccepted"] = "Your mentorship request has been accepted!",
            ["RequestDeclined"] = "Your mentorship request was declined.",
            ["SessionPlanReady"] = "Your session plan has been generated and is ready to view.",
            ["CompletionMarked"] = "A session has been marked as complete. Please confirm.",
            ["Reminder"] = "Reminder: You have an upcoming session scheduled."
        };

        var random = new Random(FakerSeed + 5);
        var batchItems = new List<WriteRequest>();
        var allUserIds = mentorIds.Select(id => $"user-{id}").Concat(menteeIds.Select(id => $"user-{id}")).ToList();

        for (var i = 0; i < 50; i++)
        {
            var notificationType = notificationTypes[i % notificationTypes.Length];
            var recipientUserId = allUserIds[i % allUserIds.Count];
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(0, 30)).AddHours(-random.Next(0, 24));
            var isRead = random.NextDouble() > 0.4; // ~60% read
            var recipientMonth = $"{recipientUserId}#{createdAt:yyyy-MM}";

            var item = new Dictionary<string, AttributeValue>
            {
                ["notificationId"] = new AttributeValue { S = $"notif-{i:D4}" },
                ["recipientUserId"] = new AttributeValue { S = recipientUserId },
                ["recipientMonthKey"] = new AttributeValue { S = recipientMonth },
                ["type"] = new AttributeValue { S = notificationType },
                ["message"] = new AttributeValue { S = messages[notificationType] },
                ["actionUrl"] = new AttributeValue { S = $"/dashboard/notifications/{i}" },
                ["isRead"] = new AttributeValue { BOOL = isRead },
                ["createdAt"] = new AttributeValue { S = createdAt.ToString("O") }
            };

            batchItems.Add(new WriteRequest { PutRequest = new PutRequest { Item = item } });

            // Flush batch every 25 items
            if (batchItems.Count == 25)
            {
                await _dynamoDbClient.BatchWriteItemAsync(new BatchWriteItemRequest
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        [$"{_tablePrefix}-notifications"] = new List<WriteRequest>(batchItems)
                    }
                });
                batchItems.Clear();
            }
        }

        // Flush remaining
        if (batchItems.Count > 0)
        {
            await _dynamoDbClient.BatchWriteItemAsync(new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [$"{_tablePrefix}-notifications"] = batchItems
                }
            });
        }

        Console.WriteLine("   → 50 notifications (all types, varied read/unread)");
    }

    // =========================================================================
    // Dual-Role User
    // =========================================================================

    private async Task CreateDualRoleUserAsync()
    {
        var userId = "user-dualrole-001";
        var mentorId = "mentor-dualrole-001";
        var menteeId = "mentee-dualrole-001";

        // Create user record with both onboardings completed
        await _dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = $"{_tablePrefix}-users",
            Item = new Dictionary<string, AttributeValue>
            {
                ["userId"] = new AttributeValue { S = userId },
                ["email"] = new AttributeValue { S = "dual.role@guidedmentor.dev" },
                ["displayName"] = new AttributeValue { S = "Alex Thompson" },
                ["activeRole"] = new AttributeValue { S = "Mentor" },
                ["mentorOnboardingStatus"] = new AttributeValue { S = "Completed" },
                ["menteeOnboardingStatus"] = new AttributeValue { S = "Completed" },
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
            }
        });

        // Create mentor profile for dual-role user
        await _dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = $"{_tablePrefix}-mentors",
            Item = new Dictionary<string, AttributeValue>
            {
                ["mentorId"] = new AttributeValue { S = mentorId },
                ["userId"] = new AttributeValue { S = userId },
                ["displayName"] = new AttributeValue { S = "Alex Thompson" },
                ["email"] = new AttributeValue { S = "dual.role@guidedmentor.dev" },
                ["chapter"] = new AttributeValue { S = "Sydney" },
                ["city"] = new AttributeValue { S = "Sydney" },
                ["professionalTitle"] = new AttributeValue { S = "Senior Cloud Architect" },
                ["companyName"] = new AttributeValue { S = "Atlassian" },
                ["bio"] = new AttributeValue { S = "Experienced cloud architect mentoring while also learning ML." },
                ["expertiseAreas"] = new AttributeValue { L = new List<AttributeValue>
                    { new() { S = "Serverless" }, new() { S = "Containers" }, new() { S = "DevOps" } } },
                ["certifications"] = new AttributeValue { L = new List<AttributeValue>
                    { new() { S = "AWS Certified Solutions Architect - Professional" }, new() { S = "AWS Certified DevOps Engineer - Professional" } } },
                ["topics"] = new AttributeValue { L = new List<AttributeValue>
                    { new() { S = "Serverless Architecture" }, new() { S = "CI/CD Pipelines" } } },
                ["yearsOfExperience"] = new AttributeValue { N = "12" },
                ["maxMentees"] = new AttributeValue { N = "3" },
                ["activeMenteeCount"] = new AttributeValue { N = "1" },
                ["isAvailable"] = new AttributeValue { S = "true" },
                ["availabilityStatus"] = new AttributeValue { S = "Available" },
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
            }
        });

        // Create mentee profile for dual-role user
        await _dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = $"{_tablePrefix}-mentees",
            Item = new Dictionary<string, AttributeValue>
            {
                ["menteeId"] = new AttributeValue { S = menteeId },
                ["userId"] = new AttributeValue { S = userId },
                ["displayName"] = new AttributeValue { S = "Alex Thompson" },
                ["email"] = new AttributeValue { S = "dual.role@guidedmentor.dev" },
                ["chapter"] = new AttributeValue { S = "Sydney" },
                ["city"] = new AttributeValue { S = "Sydney" },
                ["skills"] = new AttributeValue { L = new List<AttributeValue>
                    { new() { S = "Python" }, new() { S = "Terraform" }, new() { S = "SageMaker" } } },
                ["experienceLevel"] = new AttributeValue { S = "Intermediate" },
                ["yearsOfExperience"] = new AttributeValue { N = "12" },
                ["primaryGoal"] = new AttributeValue { S = "SkillDevelopment" },
                ["goalDescription"] = new AttributeValue { S = "Learning ML/AI on AWS while mentoring others in cloud architecture." },
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
            }
        });

        Console.WriteLine("   → Dual-role user: dual.role@guidedmentor.dev (both onboardings complete)");
    }
}
