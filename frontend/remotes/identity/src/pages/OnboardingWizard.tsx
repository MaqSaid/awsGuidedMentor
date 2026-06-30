import { useState, useCallback, useEffect, useRef } from 'react';
import { Button, Input, ProgressIndicator, Skeleton } from '@guided-mentor/design-system';

/**
 * OnboardingWizard — Multi-step onboarding flow.
 * 4-step mentee / 3-step mentor with visual progress, step persistence,
 * inline validation, aria-live error announcements, photo/resume upload.
 *
 * Requirements: 1.8, 3.1, 3.8, 4.1, 4.7, 13.6, 25.7
 */

type Role = 'mentor' | 'mentee';

interface StepConfig {
  title: string;
  description: string;
}

const MENTEE_STEPS: StepConfig[] = [
  { title: 'Basic Info', description: 'Tell us about yourself' },
  { title: 'Goals & Skills', description: 'What are you looking to achieve?' },
  { title: 'Preferences', description: 'How do you want to connect?' },
  { title: 'Resume', description: 'Upload your resume (optional)' },
];

const MENTOR_STEPS: StepConfig[] = [
  { title: 'Professional Info', description: 'Your expertise and background' },
  { title: 'Mentoring Preferences', description: 'How you want to mentor' },
  { title: 'Availability', description: 'When are you available?' },
];

const AUSTRALIAN_CHAPTERS = [
  'Sydney', 'Melbourne', 'Brisbane', 'Perth', 'Adelaide',
  'Canberra', 'Hobart', 'Darwin', 'Gold Coast', 'Newcastle',
  'Wollongong', 'Geelong', 'Townsville',
] as const;

const EXPERIENCE_LEVELS = ['beginner', 'intermediate', 'advanced'] as const;

const PRIMARY_GOALS = [
  { value: 'career_transition', label: 'Career Transition' },
  { value: 'skill_development', label: 'Skill Development' },
  { value: 'certification_preparation', label: 'Certification Preparation' },
  { value: 'project_guidance', label: 'Project Guidance' },
] as const;

const SESSION_FORMATS = ['video_call', 'voice_call', 'chat'] as const;

const DURATIONS = [
  { value: '4_weeks', label: '4 Weeks' },
  { value: '8_weeks', label: '8 Weeks' },
  { value: '12_weeks', label: '12 Weeks' },
] as const;

interface OnboardingData {
  // Shared
  displayName: string;
  awsChapter: string;
  city: string;
  profilePhotoUrl: string;
  // Mentee
  skills: string[];
  experienceLevel: string;
  yearsOfExperience: number;
  primaryGoal: string;
  goalDescription: string;
  preferredDuration: string;
  communicationPreference: string;
  resumeUrl: string;
  // Mentor
  expertiseAreas: string[];
  certifications: string[];
  topics: string[];
  mentorYearsOfExperience: number;
  maxMentees: number;
  sessionFormats: string[];
  professionalTitle: string;
  companyName: string;
  bio: string;
}

const initialData: OnboardingData = {
  displayName: '',
  awsChapter: '',
  city: '',
  profilePhotoUrl: '',
  skills: [],
  experienceLevel: '',
  yearsOfExperience: 0,
  primaryGoal: '',
  goalDescription: '',
  preferredDuration: '',
  communicationPreference: '',
  resumeUrl: '',
  expertiseAreas: [],
  certifications: [],
  topics: [],
  mentorYearsOfExperience: 0,
  maxMentees: 1,
  sessionFormats: [],
  professionalTitle: '',
  companyName: '',
  bio: '',
};

interface OnboardingWizardProps {
  role?: Role;
}

export function OnboardingWizard({ role: roleProp }: OnboardingWizardProps) {
  // Determine role from prop or URL params
  const resolvedRole: Role = roleProp ?? (
    typeof window !== 'undefined'
      ? (new URLSearchParams(window.location.search).get('role') as Role) ?? 'mentee'
      : 'mentee'
  );

  const steps = resolvedRole === 'mentor' ? MENTOR_STEPS : MENTEE_STEPS;
  const totalSteps = steps.length;

  const [currentStep, setCurrentStep] = useState(1);
  const [data, setData] = useState<OnboardingData>(initialData);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const announceRef = useRef<HTMLDivElement>(null);

  // Fetch saved progress on mount
  useEffect(() => {
    const fetchProgress = async () => {
      try {
        const response = await fetch(`/v1/onboarding/progress?role=${resolvedRole}`);
        if (response.ok) {
          const progress = (await response.json()) as { step: number; data: Partial<OnboardingData> };
          if (progress.step > 0) {
            setCurrentStep(Math.min(progress.step + 1, totalSteps));
          }
          if (progress.data) {
            setData((prev) => ({ ...prev, ...progress.data }));
          }
        }
      } catch {
        // Continue fresh if unable to fetch
      } finally {
        setLoading(false);
      }
    };
    void fetchProgress();
  }, [resolvedRole, totalSteps]);

  // Announce errors for screen readers
  const announceErrors = useCallback((errs: Record<string, string>) => {
    if (announceRef.current && Object.keys(errs).length > 0) {
      announceRef.current.textContent = `Validation errors: ${Object.values(errs).join('. ')}`;
    }
  }, []);

  const updateField = useCallback(<K extends keyof OnboardingData>(field: K, value: OnboardingData[K]) => {
    setData((prev) => ({ ...prev, [field]: value }));
    setErrors((prev) => {
      const next = { ...prev };
      delete next[field];
      return next;
    });
  }, []);

  const validateStep = useCallback((): boolean => {
    const newErrors: Record<string, string> = {};

    if (resolvedRole === 'mentee') {
      if (currentStep === 1) {
        if (!data.displayName || data.displayName.length < 2) newErrors['displayName'] = 'Name must be at least 2 characters';
        if (!data.awsChapter) newErrors['awsChapter'] = 'Please select a chapter';
        if (!data.city) newErrors['city'] = 'City is required';
      } else if (currentStep === 2) {
        if (data.skills.length === 0) newErrors['skills'] = 'Add at least one skill';
        if (!data.experienceLevel) newErrors['experienceLevel'] = 'Select experience level';
        if (!data.primaryGoal) newErrors['primaryGoal'] = 'Select a primary goal';
        if (!data.goalDescription || data.goalDescription.length < 50) newErrors['goalDescription'] = 'Goal description must be at least 50 characters';
      } else if (currentStep === 3) {
        if (!data.preferredDuration) newErrors['preferredDuration'] = 'Select preferred duration';
        if (!data.communicationPreference) newErrors['communicationPreference'] = 'Select communication preference';
      }
      // Step 4 (resume) is optional
    } else {
      if (currentStep === 1) {
        if (!data.displayName || data.displayName.length < 2) newErrors['displayName'] = 'Name must be at least 2 characters';
        if (!data.professionalTitle) newErrors['professionalTitle'] = 'Professional title is required';
        if (!data.companyName) newErrors['companyName'] = 'Company name is required';
        if (!data.awsChapter) newErrors['awsChapter'] = 'Please select a chapter';
        if (!data.bio || data.bio.length < 100) newErrors['bio'] = 'Bio must be at least 100 characters';
        if (data.expertiseAreas.length === 0) newErrors['expertiseAreas'] = 'Add at least one expertise area';
      } else if (currentStep === 2) {
        if (data.topics.length === 0) newErrors['topics'] = 'Add at least one topic';
        if (data.maxMentees < 1 || data.maxMentees > 5) newErrors['maxMentees'] = 'Max mentees must be 1-5';
        if (data.sessionFormats.length === 0) newErrors['sessionFormats'] = 'Select at least one format';
      }
      // Step 3 (availability) is more flexible
    }

    setErrors(newErrors);
    announceErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [resolvedRole, currentStep, data, announceErrors]);

  const saveStepProgress = useCallback(async () => {
    setSaving(true);
    try {
      await fetch('/v1/onboarding/step', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role: resolvedRole, step: currentStep, data }),
      });
    } catch {
      // Silent fail — user can still proceed
    } finally {
      setSaving(false);
    }
  }, [resolvedRole, currentStep, data]);

  const handleNext = useCallback(async () => {
    if (!validateStep()) return;
    await saveStepProgress();
    if (currentStep < totalSteps) {
      setCurrentStep((s) => s + 1);
    }
  }, [validateStep, saveStepProgress, currentStep, totalSteps]);

  const handleBack = useCallback(() => {
    if (currentStep > 1) {
      setCurrentStep((s) => s - 1);
      setErrors({});
    }
  }, [currentStep]);

  const handleSubmitAll = useCallback(async () => {
    if (!validateStep()) return;
    setSubmitting(true);
    try {
      await saveStepProgress();
      const response = await fetch('/v1/onboarding/step', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role: resolvedRole, step: totalSteps, data, complete: true }),
      });
      if (response.ok) {
        window.location.href = '/dashboard';
      }
    } catch {
      setErrors({ submit: 'Unable to complete onboarding. Please try again.' });
    } finally {
      setSubmitting(false);
    }
  }, [validateStep, saveStepProgress, resolvedRole, totalSteps, data]);

  const handlePhotoUpload = useCallback(async (file: File) => {
    try {
      const urlRes = await fetch('/v1/uploads/resume-url', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ fileName: file.name, fileType: file.type, uploadType: 'photo' }),
      });
      if (!urlRes.ok) return;
      const { uploadUrl, publicUrl } = (await urlRes.json()) as { uploadUrl: string; publicUrl: string };
      await fetch(uploadUrl, { method: 'PUT', body: file, headers: { 'Content-Type': file.type } });
      updateField('profilePhotoUrl', publicUrl);
    } catch {
      setErrors((prev) => ({ ...prev, photo: 'Upload failed. Please try again.' }));
    }
  }, [updateField]);

  const handleResumeUpload = useCallback(async (file: File) => {
    const validTypes = ['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
    if (!validTypes.includes(file.type)) {
      setErrors((prev) => ({ ...prev, resume: 'Only PDF and DOCX files are accepted.' }));
      return;
    }
    try {
      const urlRes = await fetch('/v1/uploads/resume-url', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ fileName: file.name, fileType: file.type, uploadType: 'resume' }),
      });
      if (!urlRes.ok) return;
      const { uploadUrl, publicUrl } = (await urlRes.json()) as { uploadUrl: string; publicUrl: string };
      await fetch(uploadUrl, { method: 'PUT', body: file, headers: { 'Content-Type': file.type } });
      updateField('resumeUrl', publicUrl);
    } catch {
      setErrors((prev) => ({ ...prev, resume: 'Upload failed. Please try again.' }));
    }
  }, [updateField]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen" data-testid="identity-onboarding-wizard">
        <div className="glass-card p-8 w-full max-w-2xl space-y-6">
          <Skeleton height="1.5rem" width="40%" />
          <Skeleton height="0.5rem" />
          <Skeleton height="3rem" />
          <Skeleton height="3rem" />
          <Skeleton height="3rem" />
        </div>
      </div>
    );
  }

  const currentStepConfig = steps[currentStep - 1];
  const isLastStep = currentStep === totalSteps;

  return (
    <div
      className="flex items-center justify-center min-h-screen px-4 py-8"
      data-testid="identity-onboarding-wizard"
    >
      <div className="glass-card p-8 w-full max-w-2xl space-y-6">
        {/* Progress Indicator */}
        <ProgressIndicator
          currentStep={currentStep}
          totalSteps={totalSteps}
          label={currentStepConfig?.title}
        />

        {/* Step Header */}
        <div>
          <h1 className="text-xl font-semibold text-text-primary">
            {currentStepConfig?.title}
          </h1>
          <p className="text-sm text-text-secondary mt-1">
            {currentStepConfig?.description}
          </p>
        </div>

        {/* Submit error */}
        {errors['submit'] && (
          <div role="alert" aria-live="assertive" className="p-3 rounded-md bg-error/10 border border-error/30 text-sm text-error">
            {errors['submit']}
          </div>
        )}

        {/* Step Content */}
        <div className="space-y-4">
          {resolvedRole === 'mentee' ? (
            <MenteeStep step={currentStep} data={data} errors={errors} updateField={updateField} onPhotoUpload={handlePhotoUpload} onResumeUpload={handleResumeUpload} />
          ) : (
            <MentorStep step={currentStep} data={data} errors={errors} updateField={updateField} onPhotoUpload={handlePhotoUpload} />
          )}
        </div>

        {/* Navigation */}
        <div className="flex items-center justify-between pt-4 border-t border-[rgba(255,255,255,0.08)]">
          <Button
            variant="ghost"
            onClick={handleBack}
            disabled={currentStep === 1}
          >
            Back
          </Button>

          <div className="flex items-center gap-2">
            {saving && <span className="text-xs text-text-muted">Saving...</span>}
            {isLastStep ? (
              <Button
                variant="primary"
                onClick={handleSubmitAll}
                loading={submitting}
              >
                Complete Onboarding
              </Button>
            ) : (
              <Button
                variant="primary"
                onClick={handleNext}
              >
                Next
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* SR-only live region for validation announcements */}
      <div
        ref={announceRef}
        aria-live="assertive"
        aria-atomic="true"
        style={{ position: 'absolute', width: '1px', height: '1px', overflow: 'hidden', clip: 'rect(0,0,0,0)' }}
      />
    </div>
  );
}

/** Mentee onboarding steps */
interface StepProps {
  step: number;
  data: OnboardingData;
  errors: Record<string, string>;
  updateField: <K extends keyof OnboardingData>(field: K, value: OnboardingData[K]) => void;
  onPhotoUpload: (file: File) => void;
  onResumeUpload?: (file: File) => void;
}

function MenteeStep({ step, data, errors, updateField, onPhotoUpload, onResumeUpload }: StepProps) {
  if (step === 1) {
    return (
      <>
        <Input
          label="Display Name"
          required
          value={data.displayName}
          onChange={(e) => updateField('displayName', e.target.value)}
          error={errors['displayName']}
          aria-describedby="displayName-help"
          helpText="This will be visible to mentors."
        />
        <SelectField
          label="AWS Chapter"
          required
          value={data.awsChapter}
          onChange={(v) => updateField('awsChapter', v)}
          options={AUSTRALIAN_CHAPTERS.map((c) => ({ value: c, label: c }))}
          error={errors['awsChapter']}
        />
        <Input
          label="City"
          required
          value={data.city}
          onChange={(e) => updateField('city', e.target.value)}
          error={errors['city']}
        />
        <PhotoUploadZone
          currentUrl={data.profilePhotoUrl}
          onUpload={onPhotoUpload}
          error={errors['photo']}
        />
      </>
    );
  }

  if (step === 2) {
    return (
      <>
        <TagInput
          label="Skills"
          required
          tags={data.skills}
          onChange={(tags) => updateField('skills', tags)}
          error={errors['skills']}
          helpText="Add 1-10 skills (press Enter to add)"
          maxTags={10}
        />
        <SelectField
          label="Experience Level"
          required
          value={data.experienceLevel}
          onChange={(v) => updateField('experienceLevel', v)}
          options={EXPERIENCE_LEVELS.map((l) => ({ value: l, label: l.charAt(0).toUpperCase() + l.slice(1) }))}
          error={errors['experienceLevel']}
        />
        <Input
          label="Years of Experience"
          type="number"
          min={0}
          max={50}
          value={data.yearsOfExperience.toString()}
          onChange={(e) => updateField('yearsOfExperience', parseInt(e.target.value) || 0)}
        />
        <SelectField
          label="Primary Goal"
          required
          value={data.primaryGoal}
          onChange={(v) => updateField('primaryGoal', v)}
          options={PRIMARY_GOALS.map((g) => ({ value: g.value, label: g.label }))}
          error={errors['primaryGoal']}
        />
        <div className="flex flex-col gap-1.5">
          <label className="text-sm font-medium text-text-secondary">
            Goal Description <span className="text-error" aria-hidden="true">*</span>
          </label>
          <textarea
            className={[
              'w-full px-3 py-2 rounded-md bg-surface border text-text-primary placeholder:text-text-muted transition-all duration-base outline-none',
              'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
              errors['goalDescription'] ? 'border-error' : 'border-[rgba(255,255,255,0.08)] hover:border-[rgba(255,255,255,0.2)]',
            ].join(' ')}
            rows={3}
            value={data.goalDescription}
            onChange={(e) => updateField('goalDescription', e.target.value)}
            aria-invalid={!!errors['goalDescription']}
            aria-describedby={errors['goalDescription'] ? 'goalDescription-error' : undefined}
          />
          {errors['goalDescription'] && (
            <p id="goalDescription-error" role="alert" className="text-xs text-error">{errors['goalDescription']}</p>
          )}
          <p className="text-xs text-text-muted">{data.goalDescription.length}/500 characters (min 50)</p>
        </div>
      </>
    );
  }

  if (step === 3) {
    return (
      <>
        <SelectField
          label="Preferred Mentoring Duration"
          required
          value={data.preferredDuration}
          onChange={(v) => updateField('preferredDuration', v)}
          options={DURATIONS.map((d) => ({ value: d.value, label: d.label }))}
          error={errors['preferredDuration']}
        />
        <SelectField
          label="Communication Preference"
          required
          value={data.communicationPreference}
          onChange={(v) => updateField('communicationPreference', v)}
          options={SESSION_FORMATS.map((f) => ({ value: f, label: f.replace('_', ' ').replace(/\b\w/g, (c) => c.toUpperCase()) }))}
          error={errors['communicationPreference']}
        />
      </>
    );
  }

  if (step === 4) {
    return (
      <>
        <div className="text-center space-y-2 py-4">
          <p className="text-text-secondary text-sm">
            Upload your resume to help mentors understand your background better.
          </p>
          <p className="text-xs text-text-muted">This is optional — you can add it later in settings.</p>
        </div>
        <FileUploadZone
          label="Resume"
          accept=".pdf,.docx"
          currentUrl={data.resumeUrl}
          onUpload={onResumeUpload!}
          error={errors['resume']}
          helpText="PDF or DOCX (max 10MB)"
        />
        {!data.resumeUrl && (
          <p className="text-center">
            <button
              type="button"
              className="text-sm text-text-muted hover:text-text-secondary transition-colors duration-fast"
              onClick={() => { /* No-op: user can just click Complete */ }}
            >
              Skip for now →
            </button>
          </p>
        )}
      </>
    );
  }

  return null;
}

/** Mentor onboarding steps */
function MentorStep({ step, data, errors, updateField, onPhotoUpload }: Omit<StepProps, 'onResumeUpload'>) {
  if (step === 1) {
    return (
      <>
        <Input
          label="Display Name"
          required
          value={data.displayName}
          onChange={(e) => updateField('displayName', e.target.value)}
          error={errors['displayName']}
        />
        <Input
          label="Professional Title"
          required
          value={data.professionalTitle}
          onChange={(e) => updateField('professionalTitle', e.target.value)}
          error={errors['professionalTitle']}
          helpText="e.g. Senior Solutions Architect"
        />
        <Input
          label="Company"
          required
          value={data.companyName}
          onChange={(e) => updateField('companyName', e.target.value)}
          error={errors['companyName']}
        />
        <SelectField
          label="AWS Chapter"
          required
          value={data.awsChapter}
          onChange={(v) => updateField('awsChapter', v)}
          options={AUSTRALIAN_CHAPTERS.map((c) => ({ value: c, label: c }))}
          error={errors['awsChapter']}
        />
        <TagInput
          label="Expertise Areas"
          required
          tags={data.expertiseAreas}
          onChange={(tags) => updateField('expertiseAreas', tags)}
          error={errors['expertiseAreas']}
          helpText="Add 1-10 areas (press Enter to add)"
          maxTags={10}
        />
        <div className="flex flex-col gap-1.5">
          <label className="text-sm font-medium text-text-secondary">
            Bio <span className="text-error" aria-hidden="true">*</span>
          </label>
          <textarea
            className={[
              'w-full px-3 py-2 rounded-md bg-surface border text-text-primary placeholder:text-text-muted transition-all duration-base outline-none',
              'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
              errors['bio'] ? 'border-error' : 'border-[rgba(255,255,255,0.08)] hover:border-[rgba(255,255,255,0.2)]',
            ].join(' ')}
            rows={4}
            value={data.bio}
            onChange={(e) => updateField('bio', e.target.value)}
            aria-invalid={!!errors['bio']}
            aria-describedby={errors['bio'] ? 'bio-error' : undefined}
            maxLength={1000}
          />
          {errors['bio'] && (
            <p id="bio-error" role="alert" className="text-xs text-error">{errors['bio']}</p>
          )}
          <p className="text-xs text-text-muted">{data.bio.length}/1000 characters (min 100)</p>
        </div>
        <PhotoUploadZone
          currentUrl={data.profilePhotoUrl}
          onUpload={onPhotoUpload}
          error={errors['photo']}
        />
      </>
    );
  }

  if (step === 2) {
    return (
      <>
        <TagInput
          label="Mentoring Topics"
          required
          tags={data.topics}
          onChange={(tags) => updateField('topics', tags)}
          error={errors['topics']}
          helpText="Topics you can mentor on (1-10)"
          maxTags={10}
        />
        <Input
          label="Years of Experience"
          type="number"
          min={1}
          max={30}
          value={data.mentorYearsOfExperience.toString()}
          onChange={(e) => updateField('mentorYearsOfExperience', parseInt(e.target.value) || 0)}
        />
        <Input
          label="Maximum Mentees"
          type="number"
          min={1}
          max={5}
          required
          value={data.maxMentees.toString()}
          onChange={(e) => updateField('maxMentees', parseInt(e.target.value) || 1)}
          error={errors['maxMentees']}
          helpText="1-5 mentees at a time"
        />
        <CheckboxGroup
          label="Session Formats"
          required
          options={SESSION_FORMATS.map((f) => ({ value: f, label: f.replace('_', ' ').replace(/\b\w/g, (c) => c.toUpperCase()) }))}
          selected={data.sessionFormats}
          onChange={(v) => updateField('sessionFormats', v)}
          error={errors['sessionFormats']}
        />
        <TagInput
          label="Certifications"
          tags={data.certifications}
          onChange={(tags) => updateField('certifications', tags)}
          helpText="AWS certifications (optional, max 15)"
          maxTags={15}
        />
      </>
    );
  }

  if (step === 3) {
    return (
      <div className="space-y-4">
        <p className="text-sm text-text-secondary">
          Set your general availability. You can refine this later in settings.
        </p>
        <Input
          label="City"
          value={data.city}
          onChange={(e) => updateField('city', e.target.value)}
          helpText="Helps with timezone matching"
        />
        <p className="text-xs text-text-muted">
          Detailed availability scheduling will be available in your settings after onboarding.
        </p>
      </div>
    );
  }

  return null;
}

/** Reusable select field */
function SelectField({
  label,
  value,
  onChange,
  options,
  required,
  error,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
  required?: boolean;
  error?: string;
}) {
  const id = label.toLowerCase().replace(/\s+/g, '-');
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-text-secondary">
        {label}
        {required && <span className="text-error ml-1" aria-hidden="true">*</span>}
      </label>
      <select
        id={id}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        aria-invalid={!!error}
        className={[
          'w-full px-3 py-2 rounded-md bg-surface border text-text-primary transition-all duration-base outline-none',
          'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
          error ? 'border-error' : 'border-[rgba(255,255,255,0.08)] hover:border-[rgba(255,255,255,0.2)]',
        ].join(' ')}
      >
        <option value="">Select...</option>
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>
      {error && <p role="alert" className="text-xs text-error">{error}</p>}
    </div>
  );
}

/** Tag input for multi-value fields */
function TagInput({
  label,
  tags,
  onChange,
  required,
  error,
  helpText,
  maxTags = 10,
}: {
  label: string;
  tags: string[];
  onChange: (tags: string[]) => void;
  required?: boolean;
  error?: string;
  helpText?: string;
  maxTags?: number;
}) {
  const [inputValue, setInputValue] = useState('');
  const id = label.toLowerCase().replace(/\s+/g, '-');

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      const trimmed = inputValue.trim();
      if (trimmed && tags.length < maxTags && !tags.includes(trimmed)) {
        onChange([...tags, trimmed]);
        setInputValue('');
      }
    }
  };

  const removeTag = (index: number) => {
    onChange(tags.filter((_, i) => i !== index));
  };

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-text-secondary">
        {label}
        {required && <span className="text-error ml-1" aria-hidden="true">*</span>}
      </label>
      {tags.length > 0 && (
        <div className="flex flex-wrap gap-1.5" aria-label={`${label} tags`}>
          {tags.map((tag, i) => (
            <span key={tag} className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs bg-primary/10 text-primary border border-primary/20">
              {tag}
              <button
                type="button"
                onClick={() => removeTag(i)}
                aria-label={`Remove ${tag}`}
                className="hover:text-error transition-colors"
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}
      <input
        id={id}
        type="text"
        value={inputValue}
        onChange={(e) => setInputValue(e.target.value)}
        onKeyDown={handleKeyDown}
        disabled={tags.length >= maxTags}
        aria-invalid={!!error}
        className={[
          'w-full px-3 py-2 rounded-md bg-surface border text-text-primary placeholder:text-text-muted transition-all duration-base outline-none',
          'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
          error ? 'border-error' : 'border-[rgba(255,255,255,0.08)] hover:border-[rgba(255,255,255,0.2)]',
        ].join(' ')}
        placeholder={tags.length >= maxTags ? 'Maximum reached' : 'Type and press Enter'}
      />
      {helpText && !error && <p className="text-xs text-text-muted">{helpText}</p>}
      {error && <p role="alert" className="text-xs text-error">{error}</p>}
    </div>
  );
}

/** Photo upload with drag-and-drop zone */
function PhotoUploadZone({
  currentUrl,
  onUpload,
  error,
}: {
  currentUrl: string;
  onUpload: (file: File) => void;
  error?: string;
}) {
  const [dragOver, setDragOver] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    const file = e.dataTransfer.files[0];
    if (file && file.type.startsWith('image/')) {
      onUpload(file);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) onUpload(file);
  };

  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-sm font-medium text-text-secondary">Profile Photo (optional)</label>
      <div
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        onClick={() => inputRef.current?.click()}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') inputRef.current?.click(); }}
        aria-label="Upload profile photo. Drag and drop or click to select."
        className={[
          'flex items-center justify-center gap-3 p-4 rounded-md border-2 border-dashed cursor-pointer transition-all duration-base',
          dragOver ? 'border-primary bg-primary/5' : 'border-[rgba(255,255,255,0.12)] hover:border-[rgba(255,255,255,0.25)]',
        ].join(' ')}
      >
        {currentUrl ? (
          <img src={currentUrl} alt="Profile photo preview" className="h-16 w-16 rounded-full object-cover" />
        ) : (
          <svg className="h-10 w-10 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        )}
        <span className="text-sm text-text-muted">
          {currentUrl ? 'Click to replace' : 'Drag & drop or click to upload'}
        </span>
        <input ref={inputRef} type="file" accept="image/*" className="hidden" onChange={handleChange} />
      </div>
      {error && <p role="alert" className="text-xs text-error">{error}</p>}
    </div>
  );
}

/** File upload zone for resume */
function FileUploadZone({
  label,
  accept,
  currentUrl,
  onUpload,
  error,
  helpText,
}: {
  label: string;
  accept: string;
  currentUrl: string;
  onUpload: (file: File) => void;
  error?: string;
  helpText?: string;
}) {
  const [dragOver, setDragOver] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    const file = e.dataTransfer.files[0];
    if (file) onUpload(file);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) onUpload(file);
  };

  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-sm font-medium text-text-secondary">{label}</label>
      <div
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        onClick={() => inputRef.current?.click()}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') inputRef.current?.click(); }}
        aria-label={`Upload ${label}. Drag and drop or click to select.`}
        className={[
          'flex flex-col items-center justify-center gap-2 p-6 rounded-md border-2 border-dashed cursor-pointer transition-all duration-base',
          dragOver ? 'border-primary bg-primary/5' : 'border-[rgba(255,255,255,0.12)] hover:border-[rgba(255,255,255,0.25)]',
        ].join(' ')}
      >
        <svg className="h-8 w-8 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
        </svg>
        <span className="text-sm text-text-muted">
          {currentUrl ? 'File uploaded ✓ Click to replace' : 'Drag & drop or click to select'}
        </span>
        <input ref={inputRef} type="file" accept={accept} className="hidden" onChange={handleChange} />
      </div>
      {helpText && !error && <p className="text-xs text-text-muted">{helpText}</p>}
      {error && <p role="alert" className="text-xs text-error">{error}</p>}
    </div>
  );
}

/** Checkbox group */
function CheckboxGroup({
  label,
  options,
  selected,
  onChange,
  required,
  error,
}: {
  label: string;
  options: { value: string; label: string }[];
  selected: string[];
  onChange: (values: string[]) => void;
  required?: boolean;
  error?: string;
}) {
  const toggle = (value: string) => {
    if (selected.includes(value)) {
      onChange(selected.filter((v) => v !== value));
    } else {
      onChange([...selected, value]);
    }
  };

  return (
    <fieldset className="flex flex-col gap-1.5">
      <legend className="text-sm font-medium text-text-secondary">
        {label}
        {required && <span className="text-error ml-1" aria-hidden="true">*</span>}
      </legend>
      <div className="flex flex-wrap gap-2">
        {options.map((opt) => (
          <label
            key={opt.value}
            className={[
              'inline-flex items-center gap-2 px-3 py-1.5 rounded-md border cursor-pointer transition-all duration-base text-sm',
              selected.includes(opt.value)
                ? 'border-primary bg-primary/10 text-primary'
                : 'border-[rgba(255,255,255,0.08)] text-text-secondary hover:border-[rgba(255,255,255,0.2)]',
            ].join(' ')}
          >
            <input
              type="checkbox"
              checked={selected.includes(opt.value)}
              onChange={() => toggle(opt.value)}
              className="sr-only"
            />
            {opt.label}
          </label>
        ))}
      </div>
      {error && <p role="alert" className="text-xs text-error">{error}</p>}
    </fieldset>
  );
}

export default OnboardingWizard;
