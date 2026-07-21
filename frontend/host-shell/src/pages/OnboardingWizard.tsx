import { useState, useActionState, useContext, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';
import { apiFetch } from '../lib/api';

type FormActionState =
  | { status: 'idle' }
  | { status: 'success'; redirectTo?: string }
  | { status: 'error'; message: string };

const goalCategories = [
  'Career Transition',
  'Technical Skills',
  'Interview Prep',
  'Leadership',
  'System Design',
  'Open Source',
];

export default function OnboardingWizard() {
  const auth = useContext(AuthContext);
  const role = auth?.user?.activeRole ?? 'mentee';
  const totalSteps = role === 'mentee' ? 4 : 3;
  const [step, setStep] = useState(1);
  const navigate = useNavigate();

  // Mentee form state
  const [name, setName] = useState(auth?.user?.displayName ?? '');
  const [careerGoal, setCareerGoal] = useState('');
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  const [targetRole, setTargetRole] = useState('');

  // Final step submission via useActionState
  async function onboardingAction(prev: FormActionState, formData: FormData): Promise<FormActionState> {
    try {
      const payload = {
        name: formData.get('name') as string,
        role,
        careerGoal: formData.get('careerGoal') as string,
        categories: formData.getAll('categories') as string[],
        targetRole: formData.get('targetRole') as string,
      };

      const response = await apiFetch('/v1/onboarding/complete', {
        method: 'POST',
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        return { status: 'error', message: 'Something went wrong. Please try again.' };
      }

      return { status: 'success', redirectTo: '/dashboard' };
    } catch {
      return { status: 'error', message: 'Network error. Please check your connection and try again.' };
    }
  }

  const [submissionState, formAction, isPending] = useActionState(onboardingAction, { status: 'idle' });

  // Navigate on successful submission
  useEffect(() => {
    if (submissionState.status === 'success' && submissionState.redirectTo) {
      // Signal the OnboardingTour to activate on dashboard
      sessionStorage.setItem('gm_just_onboarded', 'true');
      navigate(submissionState.redirectTo);
    }
  }, [submissionState, navigate]);

  function toggleCategory(cat: string) {
    setSelectedCategories((prev) =>
      prev.includes(cat) ? prev.filter((c) => c !== cat) : [...prev, cat]
    );
  }

  function handleNext() {
    if (step < totalSteps) {
      setStep(step + 1);
    }
  }

  function handleBack() {
    if (step > 1) setStep(step - 1);
  }

  const stepLabels =
    role === 'mentee'
      ? ['Personal', 'Goal', 'Skills', 'Review']
      : ['Personal', 'Expertise', 'Review'];

  return (
    <div className="min-h-screen flex flex-col items-center px-6 py-12 relative">
      {/* Glow orb */}
      <div className="absolute top-10 right-1/3 w-80 h-80 rounded-full bg-violet/15 blur-[120px] pointer-events-none" />

      {/* Progress bar */}
      <nav className="flex items-center gap-2 mb-10 relative z-10" aria-label="Onboarding progress">
        {stepLabels.map((label, i) => {
          const stepNum = i + 1;
          const isActive = stepNum === step;
          const isCompleted = stepNum < step;
          return (
            <div key={label} className="flex items-center gap-2">
              <div
                className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold transition-colors ${
                  isActive
                    ? 'bg-violet text-white'
                    : isCompleted
                      ? 'bg-violet/30 text-violet-light'
                      : 'bg-white/5 text-text-muted'
                }`}
                aria-current={isActive ? 'step' : undefined}
              >
                {stepNum}
              </div>
              <span className={`text-sm hidden sm:inline ${isActive ? 'text-text-primary font-medium' : 'text-text-muted'}`}>
                {label}
              </span>
              {i < stepLabels.length - 1 && (
                <div className={`w-8 h-px ${isCompleted ? 'bg-violet' : 'bg-border'}`} />
              )}
            </div>
          );
        })}
      </nav>

      {/* Form card */}
      <div className="glass-card p-8 max-w-lg w-full relative z-10">
        {/* Step 1: Personal */}
        {step === 1 && (
          <div>
            <h2 className="text-2xl font-bold mb-6" style={{ fontFamily: 'Outfit, sans-serif' }}>
              Personal Details
            </h2>
            <label className="block mb-4">
              <span className="text-sm text-text-secondary mb-1 block">Full Name</span>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full px-4 py-3 rounded-xl bg-white/5 border border-border text-text-primary placeholder:text-text-muted focus:outline-none focus:border-violet transition-colors"
                placeholder="Your full name"
              />
            </label>
            <label className="block mb-4">
              <span className="text-sm text-text-secondary mb-1 block">Email</span>
              <input
                type="email"
                defaultValue={auth?.user?.email ?? ''}
                disabled
                className="w-full px-4 py-3 rounded-xl bg-white/5 border border-border text-text-muted cursor-not-allowed"
              />
            </label>
          </div>
        )}

        {/* Step 2: Goal (mentee) or Expertise (mentor) */}
        {step === 2 && role === 'mentee' && (
          <div>
            <h2 className="text-2xl font-bold mb-6" style={{ fontFamily: 'Outfit, sans-serif' }}>
              Your Career Goal
            </h2>
            <label className="block mb-4">
              <span className="text-sm text-text-secondary mb-1 block">What do you want to achieve?</span>
              <textarea
                value={careerGoal}
                onChange={(e) => setCareerGoal(e.target.value)}
                rows={3}
                className="w-full px-4 py-3 rounded-xl bg-white/5 border border-border text-text-primary placeholder:text-text-muted focus:outline-none focus:border-violet transition-colors resize-none"
                placeholder="e.g., Land a senior role at a FAANG company"
              />
            </label>
            <div className="mb-4">
              <span className="text-sm text-text-secondary mb-2 block">Goal Category</span>
              <div className="flex flex-wrap gap-2" role="group" aria-label="Goal categories">
                {goalCategories.map((cat) => (
                  <button
                    key={cat}
                    type="button"
                    onClick={() => toggleCategory(cat)}
                    aria-pressed={selectedCategories.includes(cat)}
                    className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors ${
                      selectedCategories.includes(cat)
                        ? 'bg-violet text-white'
                        : 'bg-white/5 text-text-secondary border border-border hover:border-violet'
                    }`}
                  >
                    {cat}
                  </button>
                ))}
              </div>
            </div>
            <label className="block">
              <span className="text-sm text-text-secondary mb-1 block">Target Role</span>
              <input
                type="text"
                value={targetRole}
                onChange={(e) => setTargetRole(e.target.value)}
                className="w-full px-4 py-3 rounded-xl bg-white/5 border border-border text-text-primary placeholder:text-text-muted focus:outline-none focus:border-violet transition-colors"
                placeholder="e.g., Senior Software Engineer"
              />
            </label>
          </div>
        )}

        {step === 2 && role === 'mentor' && (
          <div>
            <h2 className="text-2xl font-bold mb-6" style={{ fontFamily: 'Outfit, sans-serif' }}>
              Your Expertise
            </h2>
            <label className="block mb-4">
              <span className="text-sm text-text-secondary mb-1 block">Professional Title</span>
              <input
                type="text"
                className="w-full px-4 py-3 rounded-xl bg-white/5 border border-border text-text-primary placeholder:text-text-muted focus:outline-none focus:border-violet transition-colors"
                placeholder="e.g., Principal Engineer at AWS"
              />
            </label>
            <label className="block">
              <span className="text-sm text-text-secondary mb-1 block">Areas of Expertise</span>
              <textarea
                rows={3}
                className="w-full px-4 py-3 rounded-xl bg-white/5 border border-border text-text-primary placeholder:text-text-muted focus:outline-none focus:border-violet transition-colors resize-none"
                placeholder="e.g., System Design, Distributed Systems, Interview Coaching"
              />
            </label>
          </div>
        )}

        {/* Step 3: Skills (mentee) or Review (mentor) */}
        {step === 3 && role === 'mentee' && (
          <div>
            <h2 className="text-2xl font-bold mb-6" style={{ fontFamily: 'Outfit, sans-serif' }}>
              Your Skills
            </h2>
            <label className="block mb-4">
              <span className="text-sm text-text-secondary mb-1 block">Current Skills</span>
              <input
                type="text"
                className="w-full px-4 py-3 rounded-xl bg-white/5 border border-border text-text-primary placeholder:text-text-muted focus:outline-none focus:border-violet transition-colors"
                placeholder="e.g., React, TypeScript, Node.js"
              />
            </label>
            <label className="block">
              <span className="text-sm text-text-secondary mb-1 block">Experience Level</span>
              <select className="w-full px-4 py-3 rounded-xl bg-white/5 border border-border text-text-primary focus:outline-none focus:border-violet transition-colors">
                <option value="junior">Junior (0-2 years)</option>
                <option value="mid">Mid-level (2-5 years)</option>
                <option value="senior">Senior (5+ years)</option>
              </select>
            </label>
          </div>
        )}

        {/* Review step — uses form action for final submission */}
        {((step === 4 && role === 'mentee') || (step === 3 && role === 'mentor')) && (
          <form action={formAction}>
            <h2 className="text-2xl font-bold mb-6" style={{ fontFamily: 'Outfit, sans-serif' }}>
              Review &amp; Confirm
            </h2>

            {/* Hidden fields carry wizard data for the form action */}
            <input type="hidden" name="name" value={name} />
            <input type="hidden" name="careerGoal" value={careerGoal} />
            {selectedCategories.map((cat) => (
              <input key={cat} type="hidden" name="categories" value={cat} />
            ))}
            <input type="hidden" name="targetRole" value={targetRole} />

            <div className="space-y-3 text-sm">
              <div className="flex justify-between py-2 border-b border-border">
                <span className="text-text-secondary">Name</span>
                <span className="text-text-primary">{name || 'Not provided'}</span>
              </div>
              {role === 'mentee' && (
                <>
                  <div className="flex justify-between py-2 border-b border-border">
                    <span className="text-text-secondary">Goal</span>
                    <span className="text-text-primary">{careerGoal || 'Not provided'}</span>
                  </div>
                  <div className="flex justify-between py-2 border-b border-border">
                    <span className="text-text-secondary">Target Role</span>
                    <span className="text-text-primary">{targetRole || 'Not provided'}</span>
                  </div>
                </>
              )}
              <div className="flex justify-between py-2">
                <span className="text-text-secondary">Role</span>
                <span className={`capitalize font-medium ${role === 'mentor' ? 'text-violet-light' : 'text-mint'}`}>
                  {role}
                </span>
              </div>
            </div>

            {submissionState.status === 'error' && (
              <p className="mt-4 text-sm text-rose" role="alert" aria-live="polite">
                {submissionState.message}
              </p>
            )}

            {/* Navigation buttons */}
            <div className="flex justify-between mt-8">
              <button type="button" onClick={handleBack} className="btn-ghost text-sm">
                Back
              </button>
              <button type="submit" disabled={isPending} className="btn-violet text-sm disabled:opacity-50 disabled:cursor-not-allowed">
                {isPending ? (
                  <span className="flex items-center gap-2">
                    <span className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" aria-hidden="true" />
                    Submitting...
                  </span>
                ) : (
                  'Complete'
                )}
              </button>
            </div>
          </form>
        )}

        {/* Navigation buttons for non-final steps */}
        {!((step === 4 && role === 'mentee') || (step === 3 && role === 'mentor')) && (
          <div className="flex justify-between mt-8">
            {step > 1 ? (
              <button onClick={handleBack} className="btn-ghost text-sm">
                Back
              </button>
            ) : (
              <div />
            )}
            <button onClick={handleNext} className="btn-violet text-sm">
              Continue
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
