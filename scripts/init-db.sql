-- GuidedMentor PostgreSQL Schema
-- Replaces 8 DynamoDB tables with relational tables

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Users table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    profile_photo_url TEXT,
    aws_chapter VARCHAR(50),
    city VARCHAR(100),
    active_role VARCHAR(10), -- 'mentor' | 'mentee' | null
    mentor_onboarding_status VARCHAR(20) DEFAULT 'not_started',
    mentee_onboarding_status VARCHAR(20) DEFAULT 'not_started',
    is_disabled BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Mentors table
CREATE TABLE mentors (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id),
    expertise_areas TEXT[] DEFAULT '{}',
    certifications TEXT[] DEFAULT '{}',
    topics TEXT[] DEFAULT '{}',
    years_of_experience INTEGER DEFAULT 0,
    max_mentees INTEGER DEFAULT 3,
    active_mentee_count INTEGER DEFAULT 0,
    availability JSONB DEFAULT '{}',
    session_formats TEXT[] DEFAULT '{}',
    professional_title VARCHAR(100),
    company_name VARCHAR(100),
    bio TEXT,
    availability_status VARCHAR(20) DEFAULT 'available',
    unavailability_reason VARCHAR(50),
    return_date TIMESTAMPTZ,
    unavailable_since TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Mentees table
CREATE TABLE mentees (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id),
    skills TEXT[] DEFAULT '{}',
    experience_level VARCHAR(20),
    years_of_experience INTEGER DEFAULT 0,
    primary_goal VARCHAR(50),
    goal_description TEXT,
    preferred_duration VARCHAR(20),
    availability JSONB DEFAULT '{}',
    communication_preference VARCHAR(20),
    resume_url TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Sessions table
CREATE TABLE sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    mentee_id UUID NOT NULL REFERENCES mentees(id),
    mentor_id UUID NOT NULL REFERENCES mentors(id),
    status VARCHAR(30) DEFAULT 'pending_acceptance',
    session_plan JSONB,
    checklist_state JSONB DEFAULT '{"prework": [], "followup": []}',
    mentee_completed_at TIMESTAMPTZ,
    mentor_completed_at TIMESTAMPTZ,
    lock_id UUID,
    lock_expires_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Notifications table
CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    recipient_user_id UUID NOT NULL REFERENCES users(id),
    type VARCHAR(50) NOT NULL,
    message TEXT NOT NULL,
    action_url TEXT,
    is_read BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Auth tokens (magic links)
CREATE TABLE auth_tokens (
    token UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) NOT NULL,
    used BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL
);

-- Jobs (opportunities)
CREATE TABLE opportunities (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    mentor_id UUID NOT NULL REFERENCES mentors(id),
    title VARCHAR(200) NOT NULL,
    type VARCHAR(20) NOT NULL, -- job, workshop, event, training
    organisation_name VARCHAR(200),
    description TEXT,
    location VARCHAR(200),
    event_date_time TIMESTAMPTZ,
    employment_type VARCHAR(50),
    required_skills TEXT[] DEFAULT '{}',
    required_experience VARCHAR(20),
    external_url TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    published_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ
);

-- Meetups
CREATE TABLE meetups (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    chapter VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    event_date DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    venue_name VARCHAR(200),
    venue_address TEXT,
    event_url TEXT,
    created_by UUID NOT NULL REFERENCES users(id),
    is_cancelled BOOLEAN DEFAULT FALSE,
    confirmed_attendees UUID[] DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Engagement events (analytics)
CREATE TABLE engagement_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id_hash VARCHAR(64) NOT NULL, -- SHA-256 hashed
    event_type VARCHAR(50) NOT NULL,
    metadata JSONB,
    active_role VARCHAR(10),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_mentors_user_id ON mentors(user_id);
CREATE INDEX idx_mentees_user_id ON mentees(user_id);
CREATE INDEX idx_sessions_mentee ON sessions(mentee_id, created_at DESC);
CREATE INDEX idx_sessions_mentor ON sessions(mentor_id, created_at DESC);
CREATE INDEX idx_notifications_recipient ON notifications(recipient_user_id, created_at DESC);
CREATE INDEX idx_auth_tokens_email ON auth_tokens(email);
CREATE INDEX idx_opportunities_mentor ON opportunities(mentor_id);
CREATE INDEX idx_meetups_chapter ON meetups(chapter, event_date);
CREATE INDEX idx_engagement_user ON engagement_events(user_id_hash, created_at DESC);

-- Auto-cleanup expired auth tokens (use Hangfire job instead of DynamoDB TTL)
-- Hangfire will run: DELETE FROM auth_tokens WHERE expires_at < NOW() every 5 minutes
