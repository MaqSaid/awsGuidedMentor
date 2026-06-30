import { useNavigate } from 'react-router-dom';
import { useContext } from 'react';
import { AuthContext } from '../providers/AuthProvider';

export default function RoleSelector() {
  const navigate = useNavigate();
  const auth = useContext(AuthContext);

  function handleSelect(role: 'mentee' | 'mentor') {
    if (auth) {
      auth.updateUser({ activeRole: role });
    }
    navigate('/onboarding');
  }

  return (
    <div className="min-h-screen flex items-center justify-center px-6">
      <div className="glass-card p-10 max-w-lg w-full text-center">
        <h1 className="text-3xl font-bold mb-2" style={{ fontFamily: 'Outfit, sans-serif' }}>
          Choose Your Role
        </h1>
        <p className="text-text-secondary mb-8">
          Select how you want to use GuidedMentor
        </p>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <button
            onClick={() => handleSelect('mentee')}
            className="glass-card p-6 text-left cursor-pointer border-2 border-transparent hover:border-mint transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-mint focus:ring-offset-2 focus:ring-offset-bg-primary rounded-2xl"
            aria-label="Select mentee role"
          >
            <div className="w-12 h-12 rounded-full bg-mint/20 flex items-center justify-center mb-4">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M12 12a5 5 0 1 0 0-10 5 5 0 0 0 0 10ZM20 21a8 8 0 1 0-16 0" stroke="var(--color-mint)" strokeWidth="1.5" strokeLinecap="round" />
              </svg>
            </div>
            <h2 className="text-xl font-semibold text-text-primary mb-1">Mentee</h2>
            <p className="text-sm text-text-secondary">
              Find a mentor, set goals, and accelerate your career growth
            </p>
          </button>

          <button
            onClick={() => handleSelect('mentor')}
            className="glass-card p-6 text-left cursor-pointer border-2 border-transparent hover:border-violet transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-violet focus:ring-offset-2 focus:ring-offset-bg-primary rounded-2xl"
            aria-label="Select mentor role"
          >
            <div className="w-12 h-12 rounded-full bg-violet/20 flex items-center justify-center mb-4">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM20 8v6M23 11h-6" stroke="var(--color-violet-light)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </div>
            <h2 className="text-xl font-semibold text-text-primary mb-1">Mentor</h2>
            <p className="text-sm text-text-secondary">
              Share your expertise, guide mentees, and give back to the community
            </p>
          </button>
        </div>
      </div>
    </div>
  );
}
