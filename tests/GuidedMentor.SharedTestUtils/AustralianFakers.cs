using Bogus;

namespace GuidedMentor.SharedTestUtils;

/// <summary>
/// Bogus data generators configured with Australian locale (en_AU) for realistic test data.
/// </summary>
public static class AustralianFakers
{
    private static readonly string[] AwsExpertiseAreas =
    [
        "Serverless", "Containers", "Machine Learning", "Data Analytics",
        "Networking", "Security", "DevOps", "Databases", "IoT",
        "Game Development", "AR/VR", "Blockchain", "Cost Optimisation"
    ];

    private static readonly string[] AwsCertifications =
    [
        "AWS Certified Solutions Architect - Associate",
        "AWS Certified Solutions Architect - Professional",
        "AWS Certified Developer - Associate",
        "AWS Certified SysOps Administrator - Associate",
        "AWS Certified DevOps Engineer - Professional",
        "AWS Certified Cloud Practitioner",
        "AWS Certified Data Analytics - Specialty",
        "AWS Certified Database - Specialty",
        "AWS Certified Machine Learning - Specialty",
        "AWS Certified Security - Specialty",
        "AWS Certified Advanced Networking - Specialty",
        "AWS Certified SAP on AWS - Specialty"
    ];

    private static readonly string[] MenteeSkills =
    [
        "Python", "TypeScript", "C#", "Java", "Go", "Rust",
        "Terraform", "CloudFormation", "CDK", "Serverless Framework",
        "Docker", "Kubernetes", "Lambda", "DynamoDB", "S3",
        "API Gateway", "Step Functions", "EventBridge", "SQS", "SNS"
    ];

    private static readonly string[] MentorTopics =
    [
        "Career Transition to Cloud", "AWS Certification Prep",
        "Serverless Architecture", "Cost Optimisation",
        "Well-Architected Review", "CI/CD Pipelines",
        "Data Lake Design", "ML Ops", "Event-Driven Architecture",
        "Microservices Patterns"
    ];

    /// <summary>
    /// Generates realistic Australian mentor profiles.
    /// </summary>
    public static Faker<MentorFakeData> MentorFaker => new Faker<MentorFakeData>("en_AU")
        .RuleFor(m => m.DisplayName, f => f.Name.FullName())
        .RuleFor(m => m.Email, f => f.Internet.Email())
        .RuleFor(m => m.Chapter, f => f.PickRandom<AustralianChapter>())
        .RuleFor(m => m.City, f => f.Address.City())
        .RuleFor(m => m.ExpertiseAreas, f => f.PickRandom(AwsExpertiseAreas, f.Random.Int(1, 10)).ToList())
        .RuleFor(m => m.Certifications, f => f.PickRandom(AwsCertifications, f.Random.Int(0, 5)).ToList())
        .RuleFor(m => m.Topics, f => f.PickRandom(MentorTopics, f.Random.Int(1, 10)).ToList())
        .RuleFor(m => m.YearsOfExperience, f => f.Random.Int(1, 30))
        .RuleFor(m => m.MaxMentees, f => f.Random.Int(1, 5))
        .RuleFor(m => m.ActiveMenteeCount, (f, m) => f.Random.Int(0, m.MaxMentees))
        .RuleFor(m => m.ProfessionalTitle, f => f.Name.JobTitle())
        .RuleFor(m => m.CompanyName, f => f.Company.CompanyName())
        .RuleFor(m => m.Bio, f => f.Lorem.Sentence(20));

    /// <summary>
    /// Generates realistic Australian mentee profiles.
    /// </summary>
    public static Faker<MenteeFakeData> MenteeFaker => new Faker<MenteeFakeData>("en_AU")
        .RuleFor(m => m.DisplayName, f => f.Name.FullName())
        .RuleFor(m => m.Email, f => f.Internet.Email())
        .RuleFor(m => m.Chapter, f => f.PickRandom<AustralianChapter>())
        .RuleFor(m => m.City, f => f.Address.City())
        .RuleFor(m => m.Skills, f => f.PickRandom(MenteeSkills, f.Random.Int(1, 10)).ToList())
        .RuleFor(m => m.ExperienceLevel, f => f.PickRandom<ExperienceLevel>())
        .RuleFor(m => m.YearsOfExperience, f => f.Random.Int(0, 50))
        .RuleFor(m => m.PrimaryGoal, f => f.PickRandom<PrimaryGoal>())
        .RuleFor(m => m.GoalDescription, f => f.Lorem.Sentence(15));

    /// <summary>
    /// Generates realistic opportunity/job posting data.
    /// </summary>
    public static Faker<OpportunityFakeData> OpportunityFaker => new Faker<OpportunityFakeData>("en_AU")
        .RuleFor(o => o.Title, f => f.Name.JobTitle())
        .RuleFor(o => o.OrganisationName, f => f.Company.CompanyName())
        .RuleFor(o => o.Description, f => f.Lorem.Paragraphs(2))
        .RuleFor(o => o.Location, f => f.Address.City())
        .RuleFor(o => o.RequiredSkills, f => f.PickRandom(MenteeSkills, f.Random.Int(1, 5)).ToList())
        .RuleFor(o => o.ExternalUrl, f => f.Internet.Url());

    /// <summary>
    /// Generates realistic meetup event data.
    /// </summary>
    public static Faker<MeetupEventFakeData> MeetupEventFaker => new Faker<MeetupEventFakeData>("en_AU")
        .RuleFor(m => m.Title, f => $"AWS {f.PickRandom(AwsExpertiseAreas)} Meetup - {f.Address.City()}")
        .RuleFor(m => m.Chapter, f => f.PickRandom<AustralianChapter>())
        .RuleFor(m => m.Description, f => f.Lorem.Paragraphs(2))
        .RuleFor(m => m.Location, f => f.Address.FullAddress())
        .RuleFor(m => m.EventDate, f => f.Date.Future(1))
        .RuleFor(m => m.MaxAttendees, f => f.Random.Int(20, 200))
        .RuleFor(m => m.OrganizerName, f => f.Name.FullName());
}

// --- Test-specific enums (mirrors SharedKernel enums when they are implemented) ---

/// <summary>Australian AWS User Group chapters.</summary>
public enum AustralianChapter
{
    Sydney, Melbourne, Brisbane, Perth, Adelaide, Canberra,
    Hobart, Darwin, GoldCoast, Newcastle, Wollongong, Geelong, Townsville
}

/// <summary>Mentee experience level.</summary>
public enum ExperienceLevel
{
    Beginner, Intermediate, Advanced
}

/// <summary>Mentee primary goal for mentorship.</summary>
public enum PrimaryGoal
{
    CareerTransition, SkillDevelopment, CertificationPreparation, ProjectGuidance
}

// --- Fake data models ---

/// <summary>Fake mentor data record for testing.</summary>
public class MentorFakeData
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AustralianChapter Chapter { get; set; }
    public string City { get; set; } = string.Empty;
    public List<string> ExpertiseAreas { get; set; } = [];
    public List<string> Certifications { get; set; } = [];
    public List<string> Topics { get; set; } = [];
    public int YearsOfExperience { get; set; }
    public int MaxMentees { get; set; }
    public int ActiveMenteeCount { get; set; }
    public string ProfessionalTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
}

/// <summary>Fake mentee data record for testing.</summary>
public class MenteeFakeData
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AustralianChapter Chapter { get; set; }
    public string City { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = [];
    public ExperienceLevel ExperienceLevel { get; set; }
    public int YearsOfExperience { get; set; }
    public PrimaryGoal PrimaryGoal { get; set; }
    public string GoalDescription { get; set; } = string.Empty;
}

/// <summary>Fake opportunity data record for testing.</summary>
public class OpportunityFakeData
{
    public string Title { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> RequiredSkills { get; set; } = [];
    public string ExternalUrl { get; set; } = string.Empty;
}

/// <summary>Fake meetup event data record for testing.</summary>
public class MeetupEventFakeData
{
    public string Title { get; set; } = string.Empty;
    public AustralianChapter Chapter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int MaxAttendees { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
}
