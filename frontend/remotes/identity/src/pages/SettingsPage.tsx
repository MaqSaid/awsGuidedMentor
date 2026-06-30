import { useState, useCallback, useEffect, useRef } from 'react';
import { Button, Input, Skeleton, ConfirmDialog } from '@guided-mentor/design-system';
import { ValidatedInput } from '@guided-mentor/design-system/components';

/**
 * SettingsPage — Tabbed layout for active role editing
 * and read-only inactive role section. Photo/resume upload,
 * save with loading state and success toast. Includes delete profile
 * with confirmation dialog.
 *
 * Requirements: 4.1, 4.7, 13.8, 25.4, 25.7, 25.8, 25.9, 25.10
 */

type Role = 'mentor' | 'mentee';
type Tab = 'profile' | 'other-role';

interface UserProfile {
  displayName: string;
  email: string;
  activeRole: Role | null;
  awsChapter: string;
  city: string;
  profilePhotoUrl: string;
  // Mentee fields
  menteeOnboarded: boolean;
  skills: string[];
  experienceLevel: string;
  yearsOfExperience: number;
  primaryGoal: string;
  goalDescription: string;
  preferredDuration: string;
  communicationPreference: string;
  resumeUrl: string;
  // Mentor fields
  mentorOnboarded: boolean;
  expertiseAreas: string[];
  certifications: string[];
  topics: string[];
  mentorYearsOfExperience: number;
  maxMentees: number;
  sessionFormats: string[];
  professionalTitle: string;
  companyName: string;
  bio: string;
  availabilityStatus: 'available' | 'unavailable';
}

const AUSTRALIAN_CHAPTERS = [
  'Sydney', 'Melbourne', 'Brisbane', 'Perth', 'Adelaide',
  'Canberra', 'Hobart', 'Darwin', 'Gold Coast', 'Newcastle',
  'Wollongong', 'Geelong', 'Townsville',
] as const;

export function SettingsPage() {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [activeTab, setActiveTab] = useState<Tab>('profile');
  const [toast, setToast] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const resumeInputRef = useRef<HTMLInputElement>(null);

  // Fetch profile on mount
  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const response = await fetch('/v1/users/me');
        if (response.ok) {
          const data = (await response.json()) as UserProfile;
          setProfile(data);
        }
      } catch {
        setToast({ type: 'error', message: 'Failed to load profile.' });
      } finally {
        setLoading(false);
      }
    };
    void fetchProfile();
  }, []);

  // Auto-dismiss toast
  useEffect(() => {
    if (!toast) return;
    const timer = setTimeout(() => setToast(null), 5000);
    return () => clearTimeout(timer);
  }, [toast]);

  const updateField = useCallback(<K extends keyof UserProfile>(field: K, value: UserProfile[K]) => {
    setProfile((prev) => prev ? { ...prev, [field]: value } : prev);
    setErrors((prev) => { const next = { ...prev }; delete next[field]; return next; });
  }, []);

  const handleSave = useCallback(async () => {
    if (!profile) return;
    setSaving(true);
    setErrors({});
    try {
      const response = await fetch('/v1/settings/profile', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(profile),
      });
      if (response.ok) {
        setToast({ type: 'success', message: 'Profile saved successfully.' });
      } else {
        const data = (await response.json()) as { fieldErrors?: Record<string, string> };
        if (data.fieldErrors) setErrors(data.fieldErrors);
        setToast({ type: 'error', message: 'Failed to save. Please check the fields.' });
      }
    } catch {
      setToast({ type: 'error', message: 'Unable to connect. Please try again.' });
    } finally {
      setSaving(false);
    }
  }, [profile]);

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
      setToast({ type: 'error', message: 'Photo upload failed.' });
    }
  }, [updateField]);

  const handleResumeUpload = useCallback(async (file: File) => {
    const validTypes = ['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
    if (!validTypes.includes(file.type)) {
      setErrors((prev) => ({ ...prev, resume: 'Only PDF and DOCX accepted.' }));
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
      setToast({ type: 'success', message: 'Resume uploaded.' });
    } catch {
      setToast({ type: 'error', message: 'Resume upload failed.' });
    }
  }, [updateField]);

  const toggleAvailability = useCallback(() => {
    updateField('availabilityStatus', profile?.availabilityStatus === 'available' ? 'unavailable' : 'available');
  }, [profile?.availabilityStatus, updateField]);

  const handleDeleteProfile = useCallback(async () => {
    setDeleteLoading(true);
    try {
      const response = await fetch('/v1/users/me', { method: 'DELETE' });
      if (response.ok) {
        window.location.href = '/login';
      } else {
        setToast({ type: 'error', message: 'Unable to delete profile. Please try again.' });
      }
    } catch {
      setToast({ type: 'error', message: 'Unable to connect. Please try again.' });
    } finally {
      setDeleteLoading(false);
      setShowDeleteConfirm(false);
    }
  }, []);

  if (loading) {
    return (
      <div className="max-w-3xl mx-auto p-6 space-y-6" data-testid="identity-settings-page">
        <Skeleton height="2rem" width="30%" />
        <Skeleton height="3rem" />
        <Skeleton height="3rem" />
        <Skeleton height="3rem" />
        <Skeleton height="3rem" />
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="max-w-3xl mx-auto p-6" data-testid="identity-settings-page">
        <p className="text-text-secondary">Unable to load profile.</p>
      </div>
    );
  }

  const activeRole = profile.activeRole;
  const otherRole: Role | null = activeRole === 'mentor' ? 'mentee' : activeRole === 'mentee' ? 'mentor' : null;
  const hasOtherRole = otherRole === 'mentor' ? profile.mentorOnboarded : otherRole === 'mentee' ? profile.menteeOnboarded : false;

  return (
    <div className="max-w-3xl mx-auto p-6 space-y-6" data-testid="identity-settings-page">
      {/* Toast */}
      {toast && (
        <div
          role="alert"
          aria-live="assertive"
          className={[
            'fixed top-4 right-4 p-4 rounded-md shadow-lg z-[500] glass-card border-l-4',
            toast.type === 'success' ? 'border-l-success' : 'border-l-error',
          ].join(' ')}
        >
          <div className="flex items-center gap-2">
            <span className={`text-sm ${toast.type === 'success' ? 'text-success' : 'text-error'}`}>
              {toast.message}
            </span>
            <button
              type="button"
              onClick={() => setToast(null)}
              aria-label="Dismiss"
              className="text-text-muted hover:text-text-primary ml-2"
            >
              ×
            </button>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-text-primary">Settings</h1>
        <Button
          variant="primary"
          onClick={handleSave}
          loading={saving}
          disabled={activeTab !== 'profile'}
        >
          Save Changes
        </Button>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-[rgba(255,255,255,0.08)]" role="tablist">
        <button
          role="tab"
          aria-selected={activeTab === 'profile'}
          onClick={() => setActiveTab('profile')}
          className={[
            'px-4 py-2 text-sm font-medium transition-colors duration-base border-b-2 -mb-px outline-none',
            'focus-visible:ring-2 focus-visible:ring-primary rounded-t-sm',
            activeTab === 'profile'
              ? 'border-b-primary text-primary'
              : 'border-b-transparent text-text-secondary hover:text-text-primary',
          ].join(' ')}
        >
          Profile ({activeRole ?? 'none'})
        </button>
        {hasOtherRole && (
          <button
            role="tab"
            aria-selected={activeTab === 'other-role'}
            onClick={() => setActiveTab('other-role')}
            className={[
              'px-4 py-2 text-sm font-medium transition-colors duration-base border-b-2 -mb-px outline-none',
              'focus-visible:ring-2 focus-visible:ring-primary rounded-t-sm',
              activeTab === 'other-role'
                ? 'border-b-primary text-primary'
                : 'border-b-transparent text-text-secondary hover:text-text-primary',
            ].join(' ')}
          >
            Other Role ({otherRole})
          </button>
        )}
      </div>

      {/* Active Profile Tab */}
      {activeTab === 'profile' && (
        <div className="glass-card p-6 space-y-6" role="tabpanel" aria-label="Profile settings">
          {/* Photo */}
          <div className="flex items-center gap-4">
            <div className="relative">
              {profile.profilePhotoUrl ? (
                <img src={profile.profilePhotoUrl} alt="Profile" className="h-20 w-20 rounded-full object-cover" />
              ) : (
                <div className="h-20 w-20 rounded-full bg-surface flex items-center justify-center">
                  <svg className="h-8 w-8 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                  </svg>
                </div>
              )}
            </div>
            <div>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => fileInputRef.current?.click()}
              >
                {profile.profilePhotoUrl ? 'Replace Photo' : 'Upload Photo'}
              </Button>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                className="hidden"
                onChange={(e) => { const f = e.target.files?.[0]; if (f) void handlePhotoUpload(f); }}
              />
            </div>
          </div>

          {/* Common fields */}
          <ValidatedInput
            label="Display Name"
            value={profile.displayName}
            onChange={(e) => updateField('displayName', e.target.value)}
            externalError={errors['displayName']}
            tooltip="Your public display name (2-50 characters). This is visible to other users."
            rules={[
              {
                validate: (v) => v.length < 2 ? 'Display name must be at least 2 characters' : null,
                tooltip: 'Minimum 2 characters',
              },
              {
                validate: (v) => v.length > 50 ? 'Display name must not exceed 50 characters' : null,
                tooltip: 'Maximum 50 characters',
              },
            ]}
          />
          <Input
            label="Email"
            value={profile.email}
            disabled
            helpText="Email cannot be changed"
          />
          <div className="flex flex-col gap-1.5">
            <label htmlFor="settings-chapter" className="text-sm font-medium text-text-secondary">AWS Chapter</label>
            <select
              id="settings-chapter"
              value={profile.awsChapter}
              onChange={(e) => updateField('awsChapter', e.target.value)}
              className="w-full px-3 py-2 rounded-md bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-primary"
            >
              <option value="">Select...</option>
              {AUSTRALIAN_CHAPTERS.map((c) => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>
          <Input
            label="City"
            value={profile.city}
            onChange={(e) => updateField('city', e.target.value)}
            error={errors['city']}
          />

          {/* Role-specific fields */}
          {activeRole === 'mentee' && (
            <MenteeProfileFields profile={profile} updateField={updateField} errors={errors} resumeInputRef={resumeInputRef} onResumeUpload={handleResumeUpload} />
          )}
          {activeRole === 'mentor' && (
            <MentorProfileFields profile={profile} updateField={updateField} errors={errors} onToggleAvailability={toggleAvailability} />
          )}
        </div>
      )}

      {/* Other Role Tab (read-only) */}
      {activeTab === 'other-role' && hasOtherRole && (
        <div className="glass-card p-6 space-y-4" role="tabpanel" aria-label="Other role settings">
          <div className="p-3 rounded-md bg-[rgba(255,255,255,0.03)] border border-[rgba(255,255,255,0.08)]">
            <p className="text-sm text-text-secondary">
              This section is read-only. <a href="#" onClick={(e) => { e.preventDefault(); }} className="text-primary">Switch to {otherRole}</a> to edit these fields.
            </p>
          </div>
          {otherRole === 'mentee' && (
            <ReadOnlyMenteeFields profile={profile} />
          )}
          {otherRole === 'mentor' && (
            <ReadOnlyMentorFields profile={profile} />
          )}
        </div>
      )}

      {/* Danger Zone — Delete Profile (Req 25.8) */}
      {activeTab === 'profile' && (
        <div className="glass-card p-6 border border-error/20">
          <h2 className="text-lg font-semibold text-error mb-2">Danger Zone</h2>
          <p className="text-sm text-text-secondary mb-4">
            Permanently delete your account and all associated data. This action cannot be undone.
          </p>
          <Button
            variant="primary"
            size="sm"
            className="bg-error hover:bg-error/90 hover:shadow-none"
            onClick={() => setShowDeleteConfirm(true)}
          >
            Delete My Profile
          </Button>
        </div>
      )}

      {/* Delete Profile Confirmation Dialog (Req 25.8) */}
      <ConfirmDialog
        open={showDeleteConfirm}
        onClose={() => setShowDeleteConfirm(false)}
        onConfirm={handleDeleteProfile}
        title="Delete Your Profile"
        description="This will permanently delete your account, all session history, and any uploaded files. You will not be able to recover this data. Are you sure you want to proceed?"
        confirmLabel="Delete Permanently"
        cancelLabel="Keep My Profile"
        variant="danger"
        loading={deleteLoading}
      />
    </div>
  );
}

/** Mentee profile editable fields */
function MenteeProfileFields({
  profile,
  updateField,
  errors,
  resumeInputRef,
  onResumeUpload,
}: {
  profile: UserProfile;
  updateField: <K extends keyof UserProfile>(field: K, value: UserProfile[K]) => void;
  errors: Record<string, string>;
  resumeInputRef: React.RefObject<HTMLInputElement | null>;
  onResumeUpload: (file: File) => void;
}) {
  return (
    <>
      <Input
        label="Experience Level"
        value={profile.experienceLevel}
        onChange={(e) => updateField('experienceLevel', e.target.value)}
      />
      <Input
        label="Years of Experience"
        type="number"
        value={profile.yearsOfExperience.toString()}
        onChange={(e) => updateField('yearsOfExperience', parseInt(e.target.value) || 0)}
      />
      <Input
        label="Primary Goal"
        value={profile.primaryGoal}
        onChange={(e) => updateField('primaryGoal', e.target.value)}
      />
      <div className="flex flex-col gap-1.5">
        <label className="text-sm font-medium text-text-secondary">Goal Description</label>
        <textarea
          className="w-full px-3 py-2 rounded-md bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-primary"
          rows={3}
          value={profile.goalDescription}
          onChange={(e) => updateField('goalDescription', e.target.value)}
        />
      </div>
      {/* Resume upload */}
      <div className="flex items-center gap-4">
        <div>
          <p className="text-sm font-medium text-text-secondary mb-1">Resume</p>
          <p className="text-xs text-text-muted">
            {profile.resumeUrl ? 'Resume uploaded ✓' : 'No resume uploaded'}
          </p>
        </div>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => resumeInputRef.current?.click()}
        >
          {profile.resumeUrl ? 'Replace' : 'Upload'}
        </Button>
        <input
          ref={resumeInputRef}
          type="file"
          accept=".pdf,.docx"
          className="hidden"
          onChange={(e) => { const f = e.target.files?.[0]; if (f) onResumeUpload(f); }}
        />
      </div>
      {errors['resume'] && <p role="alert" className="text-xs text-error">{errors['resume']}</p>}
    </>
  );
}

/** Mentor profile editable fields */
function MentorProfileFields({
  profile,
  updateField,
  errors,
  onToggleAvailability,
}: {
  profile: UserProfile;
  updateField: <K extends keyof UserProfile>(field: K, value: UserProfile[K]) => void;
  errors: Record<string, string>;
  onToggleAvailability: () => void;
}) {
  return (
    <>
      <Input
        label="Professional Title"
        value={profile.professionalTitle}
        onChange={(e) => updateField('professionalTitle', e.target.value)}
        error={errors['professionalTitle']}
      />
      <Input
        label="Company"
        value={profile.companyName}
        onChange={(e) => updateField('companyName', e.target.value)}
        error={errors['companyName']}
      />
      <div className="flex flex-col gap-1.5">
        <label className="text-sm font-medium text-text-secondary">Bio</label>
        <textarea
          className="w-full px-3 py-2 rounded-md bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-primary"
          rows={4}
          value={profile.bio}
          onChange={(e) => updateField('bio', e.target.value)}
          maxLength={1000}
        />
        <p className="text-xs text-text-muted">{profile.bio.length}/1000</p>
      </div>
      <Input
        label="Max Mentees"
        type="number"
        min={1}
        max={5}
        value={profile.maxMentees.toString()}
        onChange={(e) => updateField('maxMentees', parseInt(e.target.value) || 1)}
        error={errors['maxMentees']}
      />
      {/* Availability Toggle */}
      <div className="flex items-center justify-between p-3 rounded-md bg-surface border border-[rgba(255,255,255,0.08)]">
        <div>
          <p className="text-sm font-medium text-text-primary">Availability Status</p>
          <p className="text-xs text-text-muted">
            {profile.availabilityStatus === 'available' ? 'You are visible to mentees' : 'You are hidden from browse'}
          </p>
        </div>
        <button
          type="button"
          role="switch"
          aria-checked={profile.availabilityStatus === 'available'}
          aria-label="Toggle availability"
          onClick={onToggleAvailability}
          className={[
            'relative inline-flex h-6 w-11 items-center rounded-full transition-colors duration-base',
            profile.availabilityStatus === 'available' ? 'bg-success' : 'bg-[rgba(255,255,255,0.1)]',
          ].join(' ')}
        >
          <span
            className={[
              'inline-block h-4 w-4 rounded-full bg-white transition-transform duration-base',
              profile.availabilityStatus === 'available' ? 'translate-x-6' : 'translate-x-1',
            ].join(' ')}
          />
        </button>
      </div>
    </>
  );
}

/** Read-only mentee fields */
function ReadOnlyMenteeFields({ profile }: { profile: UserProfile }) {
  return (
    <div className="space-y-3 opacity-70">
      <ReadOnlyField label="Skills" value={profile.skills.join(', ') || 'Not set'} />
      <ReadOnlyField label="Experience Level" value={profile.experienceLevel || 'Not set'} />
      <ReadOnlyField label="Primary Goal" value={profile.primaryGoal || 'Not set'} />
      <ReadOnlyField label="Goal Description" value={profile.goalDescription || 'Not set'} />
      <ReadOnlyField label="Preferred Duration" value={profile.preferredDuration || 'Not set'} />
      <ReadOnlyField label="Resume" value={profile.resumeUrl ? 'Uploaded' : 'Not uploaded'} />
    </div>
  );
}

/** Read-only mentor fields */
function ReadOnlyMentorFields({ profile }: { profile: UserProfile }) {
  return (
    <div className="space-y-3 opacity-70">
      <ReadOnlyField label="Professional Title" value={profile.professionalTitle || 'Not set'} />
      <ReadOnlyField label="Company" value={profile.companyName || 'Not set'} />
      <ReadOnlyField label="Bio" value={profile.bio || 'Not set'} />
      <ReadOnlyField label="Expertise" value={profile.expertiseAreas.join(', ') || 'Not set'} />
      <ReadOnlyField label="Topics" value={profile.topics.join(', ') || 'Not set'} />
      <ReadOnlyField label="Max Mentees" value={profile.maxMentees.toString()} />
      <ReadOnlyField label="Certifications" value={profile.certifications.join(', ') || 'None'} />
    </div>
  );
}

function ReadOnlyField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-medium text-text-muted">{label}</p>
      <p className="text-sm text-text-secondary">{value}</p>
    </div>
  );
}

export default SettingsPage;
