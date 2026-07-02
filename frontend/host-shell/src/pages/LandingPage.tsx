import { Link } from 'react-router-dom';

export default function LandingPage() {
  return (
    <div className="relative min-h-screen overflow-hidden">
      {/* Decorative glow orbs */}
      <div className="absolute top-20 left-1/4 w-96 h-96 rounded-full bg-violet/20 blur-[128px] pointer-events-none" />
      <div className="absolute bottom-20 right-1/4 w-80 h-80 rounded-full bg-mint/20 blur-[128px] pointer-events-none" />

      {/* Header */}
      <header className="flex items-center justify-between px-4 md:px-6 py-5 relative z-10">
        <div className="text-xl font-bold tracking-tight" style={{ fontFamily: 'Outfit, sans-serif' }}>
          <span className="text-text-primary">Guided</span>
          <span className="gradient-text">Mentor</span>
        </div>
        <Link to="/login" className="btn-ghost text-sm">
          Sign In
        </Link>
      </header>

      {/* Hero */}
      <main className="relative z-10 flex flex-col items-center justify-center text-center px-4 md:px-6 pt-12 md:pt-20 pb-20 md:pb-32">
        <h1 className="text-3xl sm:text-4xl md:text-5xl lg:text-6xl font-extrabold leading-tight max-w-3xl" style={{ fontFamily: 'Outfit, sans-serif' }}>
          Find Your Perfect Mentor.{' '}
          <span className="gradient-text">Powered</span> by AI.
        </h1>
        <p className="mt-4 md:mt-6 text-base md:text-lg text-text-secondary max-w-xl">
          AI-driven matching connects you with the right mentor based on your goals,
          skills, and learning style. Personalized session plans generated in seconds.
        </p>

        {/* CTAs */}
        <div className="flex flex-wrap gap-4 mt-10 justify-center">
          <Link to="/role-select" className="btn-violet inline-flex items-center gap-2">
            I&apos;m a Mentee <span aria-hidden="true">&rarr;</span>
          </Link>
          <Link to="/role-select" className="btn-ghost inline-flex items-center gap-2">
            I&apos;m a Mentor <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        {/* Feature cards */}
        <section className="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6 mt-16 md:mt-24 max-w-4xl w-full" aria-label="Key features">
          <article className="glass-card p-5 md:p-6 text-left transition-all duration-200">
            <div className="w-10 h-10 rounded-lg bg-violet/20 flex items-center justify-center mb-4">
              <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                <path d="M10 2l2.5 5 5.5.8-4 3.9.9 5.3L10 14.5 5.1 17l.9-5.3-4-3.9 5.5-.8L10 2z" stroke="var(--color-violet-light)" strokeWidth="1.5" strokeLinejoin="round" />
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-text-primary mb-2">Smart Matching</h3>
            <p className="text-sm text-text-secondary">
              AI analyzes your goals, experience, and preferences to find your ideal mentor match with a compatibility score.
            </p>
          </article>

          <article className="glass-card p-6 text-left transition-all duration-200">
            <div className="w-10 h-10 rounded-lg bg-mint/20 flex items-center justify-center mb-4">
              <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                <path d="M4 5h12M4 10h8M4 15h10" stroke="var(--color-mint)" strokeWidth="1.5" strokeLinecap="round" />
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-text-primary mb-2">AI Session Plans</h3>
            <p className="text-sm text-text-secondary">
              Personalized 35-minute session agendas generated based on your goals, with follow-up tasks and progress tracking.
            </p>
          </article>

          <article className="glass-card p-6 text-left transition-all duration-200">
            <div className="w-10 h-10 rounded-lg bg-amber/20 flex items-center justify-center mb-4">
              <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                <path d="M6 10l3 3 5-6" stroke="var(--color-amber)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                <circle cx="10" cy="10" r="8" stroke="var(--color-amber)" strokeWidth="1.5" />
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-text-primary mb-2">Two-Party Completion</h3>
            <p className="text-sm text-text-secondary">
              Both mentee and mentor confirm session completion, ensuring accountability and mutual engagement tracking.
            </p>
          </article>
        </section>
      </main>
    </div>
  );
}
