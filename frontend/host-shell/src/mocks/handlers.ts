import { http, HttpResponse } from 'msw';

/* ─── Mock Data ─── */

const menteeUser = {
  userId: 'user-mentee-0001',
  email: 'james.okonkwo@guidedmentor.dev',
  displayName: 'James Okonkwo',
  profilePhotoUrl: null,
  activeRole: 'mentee' as const,
};

const mentorUser = {
  userId: 'user-mentor-0001',
  email: 'marcus.williams@guidedmentor.dev',
  displayName: 'Marcus Williams',
  profilePhotoUrl: null,
  activeRole: 'mentor' as const,
};

const mentors = [
  {
    mentorId: 'mentor-0001',
    displayName: 'Dr. Sarah Chen',
    initials: 'SC',
    professionalTitle: 'Principal AI/ML Engineer',
    companyName: 'Amazon Web Services',
    chapter: 'GDG Melbourne',
    city: 'Melbourne',
    expertiseAreas: ['Machine Learning', 'PyTorch', 'AWS SageMaker', 'Distributed Systems'],
    compatibilityScore: 94,
    availabilityStatus: 'available',
    activeMenteeCount: 3,
    maxMentees: 5,
  },
  {
    mentorId: 'mentor-0002',
    displayName: 'Fatima Al-Hassan',
    initials: 'FA',
    professionalTitle: 'Staff Software Engineer',
    companyName: 'Google',
    chapter: 'GDG Melbourne',
    city: 'Melbourne',
    expertiseAreas: ['System Design', 'Go', 'Kubernetes', 'gRPC'],
    compatibilityScore: 87,
    availabilityStatus: 'available',
    activeMenteeCount: 2,
    maxMentees: 4,
  },
  {
    mentorId: 'mentor-0003',
    displayName: 'Ben Kensley',
    initials: 'BK',
    professionalTitle: 'Senior Platform Engineer',
    companyName: 'Atlassian',
    chapter: 'GDG Melbourne',
    city: 'Melbourne',
    expertiseAreas: ['Cloud Architecture', 'Terraform', 'CI/CD', 'AWS'],
    compatibilityScore: 76,
    availabilityStatus: 'available',
    activeMenteeCount: 1,
    maxMentees: 3,
  },
  {
    mentorId: 'mentor-0004',
    displayName: 'Marcus Williams',
    initials: 'MW',
    professionalTitle: 'Engineering Manager',
    companyName: 'Canva',
    chapter: 'GDG Melbourne',
    city: 'Melbourne',
    expertiseAreas: ['Leadership', 'Career Growth', 'System Design', 'Interview Prep'],
    compatibilityScore: 91,
    availabilityStatus: 'available',
    activeMenteeCount: 3,
    maxMentees: 5,
  },
  {
    mentorId: 'mentor-0005',
    displayName: 'Priya Sharma',
    initials: 'PS',
    professionalTitle: 'Data Science Lead',
    companyName: 'Seek',
    chapter: 'GDG Melbourne',
    city: 'Melbourne',
    expertiseAreas: ['Data Science', 'Python', 'ML Ops', 'Statistics'],
    compatibilityScore: 58,
    availabilityStatus: 'at_capacity',
    activeMenteeCount: 3,
    maxMentees: 3,
  },
];

const sessionPlan = {
  sessionId: 'session-001',
  status: 'active',
  mentee: {
    displayName: 'James Okonkwo',
    initials: 'JO',
    goal: 'Land a FAANG role',
    skills: ['React', 'TypeScript', 'Node.js'],
  },
  mentor: {
    displayName: 'Dr. Sarah Chen',
    initials: 'SC',
    title: 'Principal AI/ML Engineer',
    company: 'AWS',
  },
  compatibilityScore: 94,
  matchDescription: 'Strong alignment on career transition goals with complementary technical expertise in system design and distributed computing.',
  keyStrengths: ['System Design', 'Distributed Systems', 'Interview Prep'],
  agenda: [
    {
      timeRange: '0:00 – 3:00',
      type: 'intro',
      title: 'Introduction & Goal Alignment',
      description: 'Quick introductions, confirm career goal of landing FAANG role, and align on session objectives.',
    },
    {
      timeRange: '3:00 – 12:00',
      type: 'discussion',
      title: 'System Design Deep Dive',
      description: 'Walk through a real FAANG interview system design question. Focus on clarifying requirements, high-level architecture, and trade-off discussions.',
    },
    {
      timeRange: '12:00 – 22:00',
      type: 'exercise',
      title: 'Mock Interview: Design a URL Shortener',
      description: 'Timed practice with mentor feedback. Cover data modeling, API design, scalability, and caching strategies.',
    },
    {
      timeRange: '22:00 – 30:00',
      type: 'planning',
      title: 'Behavioral Interview Prep',
      description: 'Review STAR method for behavioral questions. Practice one leadership principle story with real-time coaching.',
    },
    {
      timeRange: '30:00 – 35:00',
      type: 'wrap-up',
      title: 'Action Items & Next Steps',
      description: 'Summarize takeaways, assign follow-up tasks, and schedule next session focus area.',
    },
  ],
  followUpTasks: [
    { title: 'Complete 2 system design problems on Excalidraw', priority: 'high' },
    { title: 'Read "Designing Data-Intensive Applications" Ch. 5-6', priority: 'medium' },
    { title: 'Record a 2-minute STAR story for "Tell me about a time you failed"', priority: 'high' },
    { title: 'Review AWS Well-Architected Framework pillars', priority: 'low' },
  ],
};

const menteeDashboard = {
  user: menteeUser,
  topMatch: 94,
  activeSessions: 1,
  goal: {
    title: 'Land a FAANG role',
    description: 'Transition to a senior software engineer position at a top tech company',
    category: 'Career Transition',
    locked: true,
    mentorAssigned: 'Dr. Sarah Chen',
  },
  sessions: [
    {
      sessionId: 'session-001',
      mentor: {
        displayName: 'Dr. Sarah Chen',
        initials: 'SC',
        score: 94,
      },
      status: 'active',
    },
  ],
  mentorPreviews: mentors.slice(0, 3),
};

const mentorDashboard = {
  user: mentorUser,
  menteeCount: 3,
  sessionCount: 3,
  completedCount: 1,
  mentees: [
    {
      menteeId: 'user-mentee-0001',
      displayName: 'James Okonkwo',
      initials: 'JO',
      goal: 'Land a FAANG role',
      score: 94,
      status: 'awaiting_confirmation',
      sessionId: 'session-001',
    },
    {
      menteeId: 'user-mentee-0002',
      displayName: 'Aisha Patel',
      initials: 'AP',
      goal: 'Become a tech lead',
      score: 87,
      status: 'active',
      sessionId: 'session-002',
    },
    {
      menteeId: 'user-mentee-0003',
      displayName: 'Liam Torres',
      initials: 'LT',
      goal: 'Master cloud architecture',
      score: 76,
      status: 'active',
      sessionId: 'session-003',
    },
  ],
};

/* ─── Handlers ─── */

export const handlers = [
  // Auth
  http.post('/v1/auth/refresh', () => {
    const header = btoa(JSON.stringify({ alg: 'RS256', typ: 'JWT' }));
    const payload = btoa(JSON.stringify({
      sub: menteeUser.userId,
      email: menteeUser.email,
      exp: Math.floor(Date.now() / 1000) + 3600,
    }));
    return HttpResponse.json({
      accessToken: `${header}.${payload}.mock-sig`,
      refreshToken: 'mock-refresh-token',
    });
  }),

  http.post('/v1/auth/signin', () => HttpResponse.json({
    accessToken: 'mock-token',
    refreshToken: 'mock-refresh',
    user: menteeUser,
  })),

  http.post('/v1/users/toggle-role', () => HttpResponse.json({ success: true })),

  // Mentee Dashboard
  http.get('/v1/dashboard/mentee', () => HttpResponse.json(menteeDashboard)),

  // Mentor Dashboard
  http.get('/v1/dashboard/mentor', () => HttpResponse.json(mentorDashboard)),

  // Mentors list
  http.get('/v1/mentors', () => HttpResponse.json({
    items: mentors,
    totalCount: mentors.length,
    chapter: 'GDG Melbourne',
  })),

  // Session plan
  http.get('/v1/sessions/:sessionId/plan', () => HttpResponse.json(sessionPlan)),

  // Mark session complete
  http.post('/v1/sessions/:sessionId/complete', () =>
    HttpResponse.json({ success: true })
  ),

  // Onboarding
  http.post('/v1/onboarding/mentee', () => HttpResponse.json({ success: true })),
  http.post('/v1/onboarding/mentor', () => HttpResponse.json({ success: true })),

  // Health
  http.get('/v1/health', () => HttpResponse.json({ status: 'Healthy' })),
];
