import { describe, it, expect } from 'vitest';
import fc from 'fast-check';
import { arbValidPassword, arbInvalidPassword } from './passwords';
import { arbMenteeProfile, arbExperienceLevel, arbPrimaryGoal } from './mentee-profiles';
import { arbMentorProfile, arbAvailableMentor, arbFullCapacityMentor } from './mentor-profiles';
import { arbSessionPlan, arbAgendaItems } from './session-plans';
import { arbAvailabilityMap, arbAwsChapter, arbSkills } from './common';

describe('Password Arbitraries', () => {
  it('arbValidPassword always generates compliant passwords', () => {
    fc.assert(
      fc.property(arbValidPassword(), (password) => {
        expect(password.length).toBeGreaterThanOrEqual(8);
        expect(password).toMatch(/[A-Z]/);
        expect(password).toMatch(/[a-z]/);
        expect(password).toMatch(/[0-9]/);
        expect(password).toMatch(/[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]/);
      }),
      { numRuns: 100 }
    );
  });

  it('arbInvalidPassword always generates non-compliant passwords', () => {
    fc.assert(
      fc.property(arbInvalidPassword(), ({ password, reason }) => {
        const hasUpper = /[A-Z]/.test(password);
        const hasLower = /[a-z]/.test(password);
        const hasDigit = /[0-9]/.test(password);
        const hasSpecial = /[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]/.test(password);
        const isLongEnough = password.length >= 8;

        // At least one constraint must be violated
        const isValid = hasUpper && hasLower && hasDigit && hasSpecial && isLongEnough;
        expect(isValid).toBe(false);
      }),
      { numRuns: 100 }
    );
  });
});

describe('Mentee Profile Arbitraries', () => {
  it('arbMenteeProfile generates profiles within constraints', () => {
    fc.assert(
      fc.property(arbMenteeProfile(), (profile) => {
        expect(profile.skills.length).toBeGreaterThanOrEqual(1);
        expect(profile.skills.length).toBeLessThanOrEqual(10);
        expect(profile.yearsOfExperience).toBeGreaterThanOrEqual(0);
        expect(profile.yearsOfExperience).toBeLessThanOrEqual(50);
        expect(profile.goalDescription.length).toBeGreaterThanOrEqual(50);
        expect(profile.goalDescription.length).toBeLessThanOrEqual(500);
        expect(['beginner', 'intermediate', 'advanced']).toContain(profile.experienceLevel);
        expect(['career_transition', 'skill_development', 'certification_preparation', 'project_guidance']).toContain(profile.primaryGoal);
        expect(['4_weeks', '8_weeks', '12_weeks']).toContain(profile.preferredDuration);
        expect(['video_call', 'voice_call', 'chat']).toContain(profile.communicationPreference);
      }),
      { numRuns: 50 }
    );
  });
});

describe('Mentor Profile Arbitraries', () => {
  it('arbMentorProfile generates profiles within constraints', () => {
    fc.assert(
      fc.property(arbMentorProfile(), (profile) => {
        expect(profile.expertiseAreas.length).toBeGreaterThanOrEqual(1);
        expect(profile.expertiseAreas.length).toBeLessThanOrEqual(10);
        expect(profile.topics.length).toBeGreaterThanOrEqual(1);
        expect(profile.topics.length).toBeLessThanOrEqual(10);
        expect(profile.yearsOfExperience).toBeGreaterThanOrEqual(1);
        expect(profile.yearsOfExperience).toBeLessThanOrEqual(30);
        expect(profile.maxMentees).toBeGreaterThanOrEqual(1);
        expect(profile.maxMentees).toBeLessThanOrEqual(5);
        expect(profile.activeMenteeCount).toBeGreaterThanOrEqual(0);
        expect(profile.activeMenteeCount).toBeLessThanOrEqual(profile.maxMentees);
        expect(profile.bio.length).toBeGreaterThanOrEqual(100);
        expect(profile.bio.length).toBeLessThanOrEqual(1000);
        expect(profile.sessionFormats.length).toBeGreaterThanOrEqual(1);
      }),
      { numRuns: 50 }
    );
  });

  it('arbFullCapacityMentor generates mentors at max capacity', () => {
    fc.assert(
      fc.property(arbFullCapacityMentor(), (profile) => {
        expect(profile.activeMenteeCount).toBe(profile.maxMentees);
        expect(profile.isAvailable).toBe(false);
      }),
      { numRuns: 50 }
    );
  });

  it('arbAvailableMentor generates mentors with available slots', () => {
    fc.assert(
      fc.property(arbAvailableMentor(), (profile) => {
        expect(profile.activeMenteeCount).toBeLessThan(profile.maxMentees);
        expect(profile.isAvailable).toBe(true);
      }),
      { numRuns: 50 }
    );
  });
});

describe('Session Plan Arbitraries', () => {
  it('arbSessionPlan generates plans with agenda summing to 35 minutes', () => {
    fc.assert(
      fc.property(arbSessionPlan(), (plan) => {
        const totalDuration = plan.agenda.reduce((sum: number, item: { durationMinutes: number }) => sum + item.durationMinutes, 0);
        expect(totalDuration).toBe(35);
        expect(plan.agenda.length).toBeGreaterThanOrEqual(3);
        expect(plan.agenda.length).toBeLessThanOrEqual(7);
        for (const item of plan.agenda) {
          expect(item.durationMinutes).toBeGreaterThanOrEqual(3);
          expect(item.description.length).toBeLessThanOrEqual(500);
        }
        expect(plan.preworkTasks.length).toBeGreaterThanOrEqual(2);
        expect(plan.preworkTasks.length).toBeLessThanOrEqual(5);
        expect(plan.followUpTasks.length).toBeGreaterThanOrEqual(2);
        expect(plan.followUpTasks.length).toBeLessThanOrEqual(5);
        for (const task of plan.preworkTasks) {
          expect(task.length).toBeLessThanOrEqual(200);
        }
        for (const task of plan.followUpTasks) {
          expect(task.length).toBeLessThanOrEqual(200);
        }
        expect(plan.sessionTitle.length).toBeLessThanOrEqual(100);
      }),
      { numRuns: 50 }
    );
  });

  it('arbAgendaItems generates items that sum to exactly 35 minutes', () => {
    fc.assert(
      fc.property(arbAgendaItems(), (items) => {
        const total = items.reduce((sum, item) => sum + item.durationMinutes, 0);
        expect(total).toBe(35);
        items.forEach(item => {
          expect(item.durationMinutes).toBeGreaterThanOrEqual(3);
        });
      }),
      { numRuns: 100 }
    );
  });
});

describe('Common Arbitraries', () => {
  it('arbAvailabilityMap generates maps with at least one day', () => {
    fc.assert(
      fc.property(arbAvailabilityMap(), (map) => {
        const keys = Object.keys(map) as Array<keyof typeof map>;
        const filledDays = keys.filter(day => {
          const slots = map[day];
          return Array.isArray(slots) && slots.length > 0;
        });
        expect(filledDays.length).toBeGreaterThanOrEqual(1);
      }),
      { numRuns: 50 }
    );
  });

  it('arbAwsChapter generates from the predefined list', () => {
    fc.assert(
      fc.property(arbAwsChapter(), (chapter) => {
        expect(typeof chapter).toBe('string');
        expect(chapter.startsWith('AWS UG')).toBe(true);
      }),
      { numRuns: 20 }
    );
  });

  it('arbSkills generates between min and max items', () => {
    fc.assert(
      fc.property(arbSkills(2, 5), (skills) => {
        expect(skills.length).toBeGreaterThanOrEqual(2);
        expect(skills.length).toBeLessThanOrEqual(5);
        // All skills should be unique
        expect(new Set(skills).size).toBe(skills.length);
      }),
      { numRuns: 50 }
    );
  });
});
