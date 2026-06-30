/**
 * OpportunityPostingForm — Create/edit form for mentors.
 * Type selector, organisation (free text), description,
 * date picker (events only), employment type (jobs only).
 *
 * Requirements: 28.1, 28.13
 */
import { useState, useCallback, useEffect, type FormEvent } from 'react';
import { Button, Input, Modal } from '@guided-mentor/design-system';
import { useCreateOpportunity, useUpdateOpportunity } from '../api/mentoring-api';
import type { OpportunityType, EmploymentType, ExperienceLevel, CreateOpportunityDto, OpportunityPosting } from '../types';

interface OpportunityPostingFormProps {
  open: boolean;
  onClose: () => void;
  editPosting?: OpportunityPosting | null;
}

const OPPORTUNITY_TYPES: { value: OpportunityType; label: string }[] = [
  { value: 'job', label: 'Job' },
  { value: 'workshop', label: 'Workshop' },
  { value: 'event', label: 'Event' },
  { value: 'training', label: 'Training' },
];

const EMPLOYMENT_TYPES: { value: EmploymentType; label: string }[] = [
  { value: 'full-time', label: 'Full-time' },
  { value: 'part-time', label: 'Part-time' },
  { value: 'contract', label: 'Contract' },
  { value: 'internship', label: 'Internship' },
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

const INITIAL_FORM: CreateOpportunityDto = {
  title: '',
  type: 'job',
  organisationName: '',
  description: '',
  location: '',
  eventDateTime: undefined,
  employmentType: undefined,
  requiredSkills: [],
  experienceLevel: 'any',
  externalUrl: '',
};

export function OpportunityPostingForm({ open, onClose, editPosting }: OpportunityPostingFormProps) {
  const [form, setForm] = useState<CreateOpportunityDto>(INITIAL_FORM);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [skillSearch, setSkillSearch] = useState('');

  const createOpportunity = useCreateOpportunity();
  const updateOpportunity = useUpdateOpportunity();

  const isEditing = !!editPosting;
  const isPending = createOpportunity.isPending || updateOpportunity.isPending;

  // Pre-fill form for editing
  useEffect(() => {
    if (editPosting) {
      setForm({
        title: editPosting.title,
        type: editPosting.type,
        organisationName: editPosting.organisationName,
        description: editPosting.description,
        location: editPosting.location,
        eventDateTime: editPosting.eventDateTime,
        employmentType: editPosting.employmentType,
        requiredSkills: editPosting.requiredSkills,
        experienceLevel: editPosting.experienceLevel,
        externalUrl: editPosting.externalUrl,
      });
    } else {
      setForm(INITIAL_FORM);
    }
    setErrors({});
  }, [editPosting, open]);

  const updateField = useCallback(<K extends keyof CreateOpportunityDto>(field: K, value: CreateOpportunityDto[K]) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    setErrors((prev) => { const next = { ...prev }; delete next[field]; return next; });
  }, []);

  const validate = useCallback((): boolean => {
    const newErrors: Record<string, string> = {};
    if (!form.title.trim()) newErrors['title'] = 'Title is required';
    if (!form.organisationName.trim()) newErrors['organisationName'] = 'Organisation is required';
    if (!form.description.trim()) newErrors['description'] = 'Description is required';
    if (!form.location.trim()) newErrors['location'] = 'Location is required';
    if (!form.externalUrl.trim()) newErrors['externalUrl'] = 'External URL is required';
    if (form.type === 'event' && !form.eventDateTime) {
      newErrors['eventDateTime'] = 'Event date is required for events';
    }
    if (form.type === 'job' && !form.employmentType) {
      newErrors['employmentType'] = 'Employment type is required for jobs';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [form]);

  const handleSubmit = useCallback(
    (e: FormEvent) => {
      e.preventDefault();
      if (!validate()) return;

      const dto: CreateOpportunityDto = {
        ...form,
        eventDateTime: form.type === 'event' ? form.eventDateTime : undefined,
        employmentType: form.type === 'job' ? form.employmentType : undefined,
      };

      if (isEditing && editPosting) {
        updateOpportunity.mutate(
          { id: editPosting.postingId, dto },
          { onSuccess: () => onClose() }
        );
      } else {
        createOpportunity.mutate(dto, { onSuccess: () => onClose() });
      }
    },
    [form, validate, isEditing, editPosting, createOpportunity, updateOpportunity, onClose]
  );

  const handleSkillToggle = (skill: string) => {
    const current = form.requiredSkills;
    const updated = current.includes(skill)
      ? current.filter((s) => s !== skill)
      : [...current, skill];
    updateField('requiredSkills', updated);
  };

  const filteredSkills = AWS_SKILLS.filter((s) =>
    s.toLowerCase().includes(skillSearch.toLowerCase())
  );

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={isEditing ? 'Edit Opportunity' : 'Post an Opportunity'}
    >
      <form onSubmit={handleSubmit} className="space-y-4 max-h-[70vh] overflow-y-auto px-1">
        {/* Type selector */}
        <fieldset>
          <legend className="text-text-secondary text-xs font-medium mb-2">Type</legend>
          <div className="flex flex-wrap gap-2">
            {OPPORTUNITY_TYPES.map(({ value, label }) => (
              <button
                key={value}
                type="button"
                onClick={() => updateField('type', value)}
                className={[
                  'px-3 py-1.5 text-sm rounded-md border transition-colors duration-base',
                  'focus-visible:ring-2 focus-visible:ring-primary outline-none',
                  form.type === value
                    ? 'bg-primary/15 text-primary border-primary/40'
                    : 'bg-surface text-text-secondary border-[rgba(255,255,255,0.08)] hover:border-primary/30',
                ].join(' ')}
              >
                {label}
              </button>
            ))}
          </div>
        </fieldset>

        {/* Title */}
        <Input
          label="Title"
          value={form.title}
          onChange={(e) => updateField('title', e.target.value)}
          error={errors['title']}
          placeholder="e.g. Senior Cloud Engineer"
        />

        {/* Organisation */}
        <Input
          label="Organisation"
          value={form.organisationName}
          onChange={(e) => updateField('organisationName', e.target.value)}
          error={errors['organisationName']}
          placeholder="Company or organisation name"
        />

        {/* Description */}
        <div className="flex flex-col gap-1.5">
          <label htmlFor="opp-description" className="text-sm font-medium text-text-secondary">
            Description
          </label>
          <textarea
            id="opp-description"
            className={[
              'w-full px-3 py-2 rounded-md bg-surface border text-text-primary outline-none',
              'focus-visible:ring-2 focus-visible:ring-primary resize-y',
              errors['description'] ? 'border-error' : 'border-[rgba(255,255,255,0.08)]',
            ].join(' ')}
            rows={4}
            value={form.description}
            onChange={(e) => updateField('description', e.target.value)}
            placeholder="Describe the opportunity..."
          />
          {errors['description'] && (
            <p role="alert" className="text-xs text-error">{errors['description']}</p>
          )}
        </div>

        {/* Location */}
        <Input
          label="Location"
          value={form.location}
          onChange={(e) => updateField('location', e.target.value)}
          error={errors['location']}
          placeholder="e.g. Sydney, Remote, Hybrid"
        />

        {/* Event date (events only) */}
        {form.type === 'event' && (
          <Input
            label="Event Date"
            type="datetime-local"
            value={form.eventDateTime ?? ''}
            onChange={(e) => updateField('eventDateTime', e.target.value || undefined)}
            error={errors['eventDateTime']}
          />
        )}

        {/* Employment type (jobs only) */}
        {form.type === 'job' && (
          <fieldset>
            <legend className="text-text-secondary text-xs font-medium mb-2">Employment Type</legend>
            <div className="flex flex-wrap gap-2">
              {EMPLOYMENT_TYPES.map(({ value, label }) => (
                <button
                  key={value}
                  type="button"
                  onClick={() => updateField('employmentType', value)}
                  className={[
                    'px-3 py-1.5 text-sm rounded-md border transition-colors duration-base',
                    'focus-visible:ring-2 focus-visible:ring-primary outline-none',
                    form.employmentType === value
                      ? 'bg-primary/15 text-primary border-primary/40'
                      : 'bg-surface text-text-secondary border-[rgba(255,255,255,0.08)] hover:border-primary/30',
                  ].join(' ')}
                >
                  {label}
                </button>
              ))}
            </div>
            {errors['employmentType'] && (
              <p role="alert" className="text-xs text-error mt-1">{errors['employmentType']}</p>
            )}
          </fieldset>
        )}

        {/* Experience level */}
        <fieldset>
          <legend className="text-text-secondary text-xs font-medium mb-2">Experience Level</legend>
          <div className="flex flex-wrap gap-2">
            {EXPERIENCE_LEVELS.map(({ value, label }) => (
              <button
                key={value}
                type="button"
                onClick={() => updateField('experienceLevel', value)}
                className={[
                  'px-3 py-1.5 text-sm rounded-md border transition-colors duration-base',
                  'focus-visible:ring-2 focus-visible:ring-primary outline-none',
                  form.experienceLevel === value
                    ? 'bg-primary/15 text-primary border-primary/40'
                    : 'bg-surface text-text-secondary border-[rgba(255,255,255,0.08)] hover:border-primary/30',
                ].join(' ')}
              >
                {label}
              </button>
            ))}
          </div>
        </fieldset>

        {/* Required skills */}
        <fieldset>
          <legend className="text-text-secondary text-xs font-medium mb-2">
            Required Skills ({form.requiredSkills.length} selected)
          </legend>
          <Input
            label="Search skills"
            type="text"
            placeholder="Search skills..."
            value={skillSearch}
            onChange={(e) => setSkillSearch(e.target.value)}
            className="mb-2 text-sm"
          />
          <div className="space-y-1 max-h-32 overflow-y-auto">
            {filteredSkills.map((skill) => (
              <label
                key={skill}
                className="flex items-center gap-2 py-1 px-2 rounded hover:bg-white/5 cursor-pointer text-sm"
              >
                <input
                  type="checkbox"
                  checked={form.requiredSkills.includes(skill)}
                  onChange={() => handleSkillToggle(skill)}
                  className="accent-primary"
                />
                <span className="text-text-secondary">{skill}</span>
              </label>
            ))}
          </div>
        </fieldset>

        {/* External URL */}
        <Input
          label="External URL"
          type="url"
          value={form.externalUrl}
          onChange={(e) => updateField('externalUrl', e.target.value)}
          error={errors['externalUrl']}
          placeholder="https://..."
        />

        {/* Actions */}
        <div className="flex gap-3 justify-end pt-4 border-t border-[rgba(255,255,255,0.08)]">
          <Button variant="ghost" onClick={onClose} disabled={isPending}>
            Cancel
          </Button>
          <Button variant="primary" type="submit" loading={isPending}>
            {isEditing ? 'Save Changes' : 'Post Opportunity'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
