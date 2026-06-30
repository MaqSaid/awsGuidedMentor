import fc from 'fast-check';
import type { DayOfWeek, AvailabilityMap, TimeSlot } from '@guided-mentor/shared-types';

/** Predefined AWS User Group chapters */
export const AWS_CHAPTERS = [
  'AWS UG Sydney',
  'AWS UG Melbourne',
  'AWS UG Brisbane',
  'AWS UG Perth',
  'AWS UG Auckland',
  'AWS UG London',
  'AWS UG Manchester',
  'AWS UG Berlin',
  'AWS UG Munich',
  'AWS UG New York',
  'AWS UG San Francisco',
  'AWS UG Seattle',
  'AWS UG Toronto',
  'AWS UG Singapore',
  'AWS UG Tokyo',
] as const;

/** Chapter-to-city mapping for matching algorithm */
export const CHAPTER_CITY_MAP: Record<string, string> = {
  'AWS UG Sydney': 'Sydney',
  'AWS UG Melbourne': 'Melbourne',
  'AWS UG Brisbane': 'Brisbane',
  'AWS UG Perth': 'Perth',
  'AWS UG Auckland': 'Auckland',
  'AWS UG London': 'London',
  'AWS UG Manchester': 'Manchester',
  'AWS UG Berlin': 'Berlin',
  'AWS UG Munich': 'Munich',
  'AWS UG New York': 'New York',
  'AWS UG San Francisco': 'San Francisco',
  'AWS UG Seattle': 'Seattle',
  'AWS UG Toronto': 'Toronto',
  'AWS UG Singapore': 'Singapore',
  'AWS UG Tokyo': 'Tokyo',
};

/** Predefined AWS skills/expertise areas */
export const AWS_SKILLS = [
  'EC2',
  'S3',
  'Lambda',
  'DynamoDB',
  'API Gateway',
  'CloudFormation',
  'CDK',
  'ECS',
  'EKS',
  'RDS',
  'Aurora',
  'SQS',
  'SNS',
  'Step Functions',
  'Bedrock',
  'SageMaker',
  'CloudFront',
  'Route 53',
  'IAM',
  'VPC',
] as const;

/** Predefined AWS certifications */
export const AWS_CERTIFICATIONS = [
  'Cloud Practitioner',
  'Solutions Architect Associate',
  'Solutions Architect Professional',
  'Developer Associate',
  'SysOps Administrator Associate',
  'DevOps Engineer Professional',
  'Database Specialty',
  'Security Specialty',
  'Machine Learning Specialty',
  'Data Analytics Specialty',
  'Advanced Networking Specialty',
  'SAP on AWS Specialty',
] as const;

/** Predefined mentoring topics */
export const MENTORING_TOPICS = [
  'career guidance',
  'interview prep',
  'resume review',
  'hands-on labs',
  'code review',
  'architecture',
  'certification study',
  'exam prep',
  'practice tests',
  'project planning',
] as const;

const DAYS_OF_WEEK: DayOfWeek[] = [
  'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday',
];

/** Generates a valid HH:MM time string */
export const arbTimeString = (): fc.Arbitrary<string> =>
  fc.tuple(
    fc.integer({ min: 0, max: 23 }),
    fc.integer({ min: 0, max: 59 }),
  ).map(([h, m]) => `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`);

/** Generates a valid time slot where end > start */
export const arbTimeSlot = (): fc.Arbitrary<TimeSlot> =>
  fc.tuple(
    fc.integer({ min: 0, max: 22 }),
    fc.integer({ min: 0, max: 59 }),
    fc.integer({ min: 30, max: 120 }), // duration in minutes
  ).map(([startHour, startMin, durationMin]) => {
    const endTotalMin = Math.min(startHour * 60 + startMin + durationMin, 23 * 60 + 59);
    const endHour = Math.floor(endTotalMin / 60);
    const endMin = endTotalMin % 60;
    return {
      start: `${startHour.toString().padStart(2, '0')}:${startMin.toString().padStart(2, '0')}`,
      end: `${endHour.toString().padStart(2, '0')}:${endMin.toString().padStart(2, '0')}`,
    };
  });

/** Generates a valid availability map (1+ days with 1+ slots each) */
export const arbAvailabilityMap = (): fc.Arbitrary<AvailabilityMap> =>
  fc.record(
    Object.fromEntries(
      DAYS_OF_WEEK.map(day => [
        day,
        fc.option(fc.array(arbTimeSlot(), { minLength: 1, maxLength: 3 }), { nil: undefined }),
      ])
    ) as Record<DayOfWeek, fc.Arbitrary<TimeSlot[] | undefined>>
  ).filter(map => {
    // At least one day must have slots
    return Object.values(map).some(slots => slots !== undefined && slots.length > 0);
  }).map(map => {
    // Remove undefined entries
    const result: AvailabilityMap = {};
    for (const [day, slots] of Object.entries(map)) {
      if (slots !== undefined) {
        result[day as DayOfWeek] = slots;
      }
    }
    return result;
  });

/** Generates a valid UUID string */
export const arbUuid = (): fc.Arbitrary<string> => fc.uuid();

/** Generates a valid ISO 8601 timestamp string */
export const arbIsoTimestamp = (): fc.Arbitrary<string> =>
  fc.date({ min: new Date('2024-01-01'), max: new Date('2025-12-31') })
    .map(d => d.toISOString());

/** Generates an AWS chapter from the predefined list */
export const arbAwsChapter = (): fc.Arbitrary<string> =>
  fc.constantFrom(...AWS_CHAPTERS);

/** Generates skills from the predefined list (1-10 items) */
export const arbSkills = (min = 1, max = 10): fc.Arbitrary<string[]> =>
  fc.shuffledSubarray([...AWS_SKILLS], { minLength: min, maxLength: max });

/** Generates a valid email address */
export const arbEmail = (): fc.Arbitrary<string> =>
  fc.tuple(
    fc.stringOf(fc.constantFrom(...'abcdefghijklmnopqrstuvwxyz0123456789'.split('')), { minLength: 3, maxLength: 15 }),
    fc.constantFrom('gmail.com', 'outlook.com', 'yahoo.com', 'example.com', 'aws.dev'),
  ).map(([local, domain]) => `${local}@${domain}`);

/** Generates a string of specified length constraints */
export const arbString = (minLength: number, maxLength: number): fc.Arbitrary<string> =>
  fc.string({ minLength, maxLength }).filter(s => s.trim().length >= minLength);

/** Generates an alphanumeric string of specified length */
export const arbAlphanumericString = (minLength: number, maxLength: number): fc.Arbitrary<string> =>
  fc.stringOf(
    fc.constantFrom(...'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 '.split('')),
    { minLength, maxLength }
  ).filter(s => s.trim().length >= minLength);
