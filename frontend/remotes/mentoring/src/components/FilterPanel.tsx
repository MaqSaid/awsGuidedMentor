/**
 * FilterPanel — sidebar filter component for Browse Page.
 * Provides chapter, skills, and availability filters.
 *
 * Requirements: 5.9, 6.1
 */
import { useState } from 'react';
import { Input } from '@guided-mentor/design-system';
import type { BrowseFilters } from '../types';

interface FilterPanelProps {
  filters: BrowseFilters;
  onFiltersChange: (filters: BrowseFilters) => void;
  className?: string;
}

const AUSTRALIAN_CHAPTERS = [
  'Sydney', 'Melbourne', 'Brisbane', 'Perth', 'Adelaide',
  'Canberra', 'Hobart', 'Darwin', 'Gold Coast', 'Newcastle',
  'Wollongong', 'Geelong', 'Townsville',
];

const AWS_SKILLS = [
  'Lambda', 'DynamoDB', 'S3', 'EC2', 'ECS', 'EKS', 'CloudFormation',
  'CDK', 'API Gateway', 'Step Functions', 'EventBridge', 'SQS', 'SNS',
  'RDS', 'Aurora', 'Bedrock', 'SageMaker', 'CloudWatch', 'IAM',
  'VPC', 'Route 53', 'CloudFront', 'Cognito', 'AppSync',
];

export function FilterPanel({ filters, onFiltersChange, className = '' }: FilterPanelProps) {
  const [skillSearch, setSkillSearch] = useState('');

  const filteredSkills = AWS_SKILLS.filter((s) =>
    s.toLowerCase().includes(skillSearch.toLowerCase())
  );

  const handleChapterChange = (chapter: string) => {
    onFiltersChange({
      ...filters,
      chapter: filters.chapter === chapter ? undefined : chapter,
    });
  };

  const handleSkillToggle = (skill: string) => {
    const current = filters.skills || [];
    const updated = current.includes(skill)
      ? current.filter((s) => s !== skill)
      : [...current, skill];
    onFiltersChange({ ...filters, skills: updated.length > 0 ? updated : undefined });
  };

  const handleAvailabilityToggle = () => {
    onFiltersChange({ ...filters, availableOnly: !filters.availableOnly });
  };

  const handleClearAll = () => {
    onFiltersChange({});
  };

  const hasActiveFilters = filters.chapter || filters.skills?.length || filters.availableOnly;

  return (
    <aside
      className={['glass-card p-4 rounded-lg space-y-5', className].join(' ')}
      aria-label="Filter mentors"
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

      {/* Chapter filter */}
      <fieldset>
        <legend className="text-text-secondary text-xs font-medium mb-2">Chapter</legend>
        <div className="space-y-1 max-h-48 overflow-y-auto">
          {AUSTRALIAN_CHAPTERS.map((chapter) => (
            <label
              key={chapter}
              className="flex items-center gap-2 py-1 px-2 rounded hover:bg-white/5 cursor-pointer text-sm"
            >
              <input
                type="radio"
                name="chapter"
                checked={filters.chapter === chapter}
                onChange={() => handleChapterChange(chapter)}
                className="accent-primary"
              />
              <span className="text-text-secondary">{chapter}</span>
            </label>
          ))}
        </div>
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

      {/* Availability filter */}
      <fieldset>
        <legend className="text-text-secondary text-xs font-medium mb-2">Availability</legend>
        <label className="flex items-center gap-2 py-1 px-2 rounded hover:bg-white/5 cursor-pointer text-sm">
          <input
            type="checkbox"
            checked={filters.availableOnly ?? false}
            onChange={handleAvailabilityToggle}
            className="accent-primary"
          />
          <span className="text-text-secondary">Available only</span>
        </label>
      </fieldset>

      {/* Apply hint */}
      {hasActiveFilters && (
        <p className="text-xs text-text-muted">
          Filters are applied automatically.
        </p>
      )}
    </aside>
  );
}
