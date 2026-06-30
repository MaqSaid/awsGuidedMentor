/**
 * OpportunityFilterPanel — Filters for the opportunities browse page.
 * Provides type, location, skills, and experience level filters.
 *
 * Requirements: 28.6
 */
import { useState } from 'react';
import { Input } from '@guided-mentor/design-system';
import type { OpportunityFilters, OpportunityType, ExperienceLevel } from '../types';

interface OpportunityFilterPanelProps {
  filters: OpportunityFilters;
  onFiltersChange: (filters: OpportunityFilters) => void;
  className?: string;
}

const OPPORTUNITY_TYPES: { value: OpportunityType; label: string }[] = [
  { value: 'job', label: 'Jobs' },
  { value: 'workshop', label: 'Workshops' },
  { value: 'event', label: 'Events' },
  { value: 'training', label: 'Training' },
];

const EXPERIENCE_LEVELS: { value: ExperienceLevel; label: string }[] = [
  { value: 'any', label: 'Any Level' },
  { value: 'beginner', label: 'Beginner' },
  { value: 'intermediate', label: 'Intermediate' },
  { value: 'advanced', label: 'Advanced' },
];

const AWS_SKILLS = [
  'Lambda', 'DynamoDB', 'S3', 'EC2', 'ECS', 'EKS', 'CloudFormation',
  'CDK', 'API Gateway', 'Step Functions', 'EventBridge', 'SQS', 'SNS',
  'RDS', 'Aurora', 'Bedrock', 'SageMaker', 'CloudWatch', 'IAM',
  'VPC', 'Route 53', 'CloudFront', 'Cognito', 'AppSync',
];

export function OpportunityFilterPanel({
  filters,
  onFiltersChange,
  className = '',
}: OpportunityFilterPanelProps) {
  const [skillSearch, setSkillSearch] = useState('');

  const filteredSkills = AWS_SKILLS.filter((s) =>
    s.toLowerCase().includes(skillSearch.toLowerCase())
  );

  const handleTypeChange = (type: OpportunityType) => {
    onFiltersChange({
      ...filters,
      type: filters.type === type ? undefined : type,
    });
  };

  const handleExperienceChange = (level: ExperienceLevel) => {
    onFiltersChange({
      ...filters,
      experienceLevel: filters.experienceLevel === level ? undefined : level,
    });
  };

  const handleSkillToggle = (skill: string) => {
    const current = filters.skills || [];
    const updated = current.includes(skill)
      ? current.filter((s) => s !== skill)
      : [...current, skill];
    onFiltersChange({ ...filters, skills: updated.length > 0 ? updated : undefined });
  };

  const handleLocationChange = (location: string) => {
    onFiltersChange({ ...filters, location: location || undefined });
  };

  const handleClearAll = () => {
    onFiltersChange({});
  };

  const hasActiveFilters = filters.type || filters.location || filters.skills?.length || filters.experienceLevel;

  return (
    <aside
      className={['glass-card p-4 rounded-lg space-y-5', className].join(' ')}
      aria-label="Filter opportunities"
      role="complementary"
    >
      <div className="flex items-center justify-between">
        <h3 className="text-text-primary font-semibold text-sm">Filters</h3>
        {hasActiveFilters && (
          <button
            onClick={handleClearAll}
            className="text-xs text-primary hover:underline focus-visible:ring-2 focus-visible:ring-primary outline-none rounded"
          >
            Clear all
          </button>
        )}
      </div>

      {/* Type filter */}
      <fieldset>
        <legend className="text-text-secondary text-xs font-medium mb-2">Type</legend>
        <div className="space-y-1">
          {OPPORTUNITY_TYPES.map(({ value, label }) => (
            <label
              key={value}
              className="flex items-center gap-2 py-1 px-2 rounded hover:bg-white/5 cursor-pointer text-sm"
            >
              <input
                type="radio"
                name="opportunity-type"
                checked={filters.type === value}
                onChange={() => handleTypeChange(value)}
                className="accent-primary"
              />
              <span className="text-text-secondary">{label}</span>
            </label>
          ))}
        </div>
      </fieldset>

      {/* Location filter */}
      <fieldset>
        <legend className="text-text-secondary text-xs font-medium mb-2">Location</legend>
        <Input
          label="Location"
          type="text"
          placeholder="City or Remote..."
          value={filters.location ?? ''}
          onChange={(e) => handleLocationChange(e.target.value)}
          className="text-sm"
        />
      </fieldset>

      {/* Skills filter */}
      <fieldset>
        <legend className="text-text-secondary text-xs font-medium mb-2">Skills</legend>
        <Input
          label="Search skills"
          type="text"
          placeholder="Search skills..."
          value={skillSearch}
          onChange={(e) => setSkillSearch(e.target.value)}
          className="mb-2 text-sm"
        />
        <div className="space-y-1 max-h-40 overflow-y-auto">
          {filteredSkills.map((skill) => (
            <label
              key={skill}
              className="flex items-center gap-2 py-1 px-2 rounded hover:bg-white/5 cursor-pointer text-sm"
            >
              <input
                type="checkbox"
                checked={filters.skills?.includes(skill) ?? false}
                onChange={() => handleSkillToggle(skill)}
                className="accent-primary"
              />
              <span className="text-text-secondary">{skill}</span>
            </label>
          ))}
        </div>
      </fieldset>

      {/* Experience level filter */}
      <fieldset>
        <legend className="text-text-secondary text-xs font-medium mb-2">Experience Level</legend>
        <div className="space-y-1">
          {EXPERIENCE_LEVELS.map(({ value, label }) => (
            <label
              key={value}
              className="flex items-center gap-2 py-1 px-2 rounded hover:bg-white/5 cursor-pointer text-sm"
            >
              <input
                type="radio"
                name="experience-level"
                checked={filters.experienceLevel === value}
                onChange={() => handleExperienceChange(value)}
                className="accent-primary"
              />
              <span className="text-text-secondary">{label}</span>
            </label>
          ))}
        </div>
      </fieldset>

      {hasActiveFilters && (
        <p className="text-xs text-text-muted">Filters are applied automatically.</p>
      )}
    </aside>
  );
}
