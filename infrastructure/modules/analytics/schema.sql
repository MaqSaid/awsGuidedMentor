-- ==============================================================================
-- GuidedMentor Analytics Database Schema
-- Database: guidedmentor_analytics
-- Engine: Aurora PostgreSQL Serverless v2 (16.4)
--
-- This schema defines denormalised reporting tables replicated from DynamoDB
-- via DynamoDB Streams → Lambda → Aurora pipeline.
-- ==============================================================================

-- Create analytics schema
CREATE SCHEMA IF NOT EXISTS analytics;

-- ==============================================================================
-- Table: analytics.matches
-- Stores compatibility match events between mentees and mentors
-- ==============================================================================

CREATE TABLE analytics.matches (
    match_id UUID PRIMARY KEY,
    mentee_id UUID NOT NULL,
    mentor_id UUID NOT NULL,
    compatibility_score INTEGER NOT NULL CHECK (compatibility_score BETWEEN 0 AND 100),
    chapter_score INTEGER NOT NULL CHECK (chapter_score BETWEEN 0 AND 30),
    skills_overlap INTEGER NOT NULL CHECK (skills_overlap BETWEEN 0 AND 30),
    goal_alignment INTEGER NOT NULL CHECK (goal_alignment BETWEEN 0 AND 25),
    experience_gap INTEGER NOT NULL CHECK (experience_gap BETWEEN 0 AND 15),
    mentee_chapter VARCHAR(50) NOT NULL,
    mentor_chapter VARCHAR(50) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW() NOT NULL
);

COMMENT ON TABLE analytics.matches IS 'Denormalised match events replicated from DynamoDB Sessions table';

-- ==============================================================================
-- Table: analytics.sessions
-- Stores session lifecycle data for reporting and analytics
-- ==============================================================================

CREATE TABLE analytics.sessions (
    session_id UUID PRIMARY KEY,
    mentee_id UUID NOT NULL,
    mentor_id UUID NOT NULL,
    status VARCHAR(30) NOT NULL CHECK (status IN (
        'pending_acceptance', 'pending_plan', 'active',
        'mentee_completed', 'completed', 'unresolved'
    )),
    plan_generated_at TIMESTAMP WITH TIME ZONE,
    mentee_completed_at TIMESTAMP WITH TIME ZONE,
    mentor_completed_at TIMESTAMP WITH TIME ZONE,
    plan_retry_count INTEGER DEFAULT 0 CHECK (plan_retry_count BETWEEN 0 AND 3),
    checklist_total INTEGER DEFAULT 0,
    checklist_completed INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW() NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW() NOT NULL
);

COMMENT ON TABLE analytics.sessions IS 'Session lifecycle tracking replicated from DynamoDB Sessions table';

-- ==============================================================================
-- Table: analytics.users
-- Stores user profile snapshots for cross-entity reporting joins
-- ==============================================================================

CREATE TABLE analytics.users (
    user_id UUID PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    display_name VARCHAR(100),
    active_role VARCHAR(10) CHECK (active_role IN ('mentor', 'mentee')),
    aws_chapter VARCHAR(50),
    city VARCHAR(100),
    mentor_onboarding_completed BOOLEAN DEFAULT FALSE,
    mentee_onboarding_completed BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW() NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW() NOT NULL
);

COMMENT ON TABLE analytics.users IS 'User profile snapshots replicated from DynamoDB Users table';

-- ==============================================================================
-- Table: analytics.engagement_metrics
-- Stores engagement event data for user activity analysis
-- ==============================================================================

CREATE TABLE analytics.engagement_metrics (
    metric_id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    event_type VARCHAR(50) NOT NULL,
    metadata JSONB,
    occurred_at TIMESTAMP WITH TIME ZONE DEFAULT NOW() NOT NULL
);

COMMENT ON TABLE analytics.engagement_metrics IS 'User engagement events for platform analytics';

-- ==============================================================================
-- Indexes for Reporting Queries
-- ==============================================================================

-- Sessions indexes
CREATE INDEX idx_sessions_status ON analytics.sessions(status);
CREATE INDEX idx_sessions_mentor ON analytics.sessions(mentor_id, created_at DESC);
CREATE INDEX idx_sessions_mentee ON analytics.sessions(mentee_id, created_at DESC);
CREATE INDEX idx_sessions_created ON analytics.sessions(created_at DESC);

-- Matches indexes
CREATE INDEX idx_matches_chapter ON analytics.matches(mentee_chapter, mentor_chapter);
CREATE INDEX idx_matches_mentor ON analytics.matches(mentor_id, created_at DESC);
CREATE INDEX idx_matches_mentee ON analytics.matches(mentee_id, created_at DESC);
CREATE INDEX idx_matches_score ON analytics.matches(compatibility_score DESC);

-- Users indexes
CREATE INDEX idx_users_chapter ON analytics.users(aws_chapter);
CREATE INDEX idx_users_role ON analytics.users(active_role);
CREATE INDEX idx_users_created ON analytics.users(created_at DESC);

-- Engagement metrics indexes
CREATE INDEX idx_engagement_user ON analytics.engagement_metrics(user_id, occurred_at DESC);
CREATE INDEX idx_engagement_type ON analytics.engagement_metrics(event_type, occurred_at DESC);

-- ==============================================================================
-- Reporting Views
-- ==============================================================================

-- Match success rates by chapter
CREATE VIEW analytics.match_success_rates AS
SELECT
    mentee_chapter,
    mentor_chapter,
    COUNT(*) AS total_matches,
    COUNT(*) FILTER (WHERE s.status = 'completed') AS completed,
    ROUND(
        COUNT(*) FILTER (WHERE s.status = 'completed')::NUMERIC /
        NULLIF(COUNT(*), 0) * 100, 1
    ) AS completion_rate_pct
FROM analytics.matches m
LEFT JOIN analytics.sessions s ON m.mentee_id = s.mentee_id AND m.mentor_id = s.mentor_id
GROUP BY mentee_chapter, mentor_chapter;

COMMENT ON VIEW analytics.match_success_rates IS 'Match completion rates broken down by mentee and mentor chapters';

-- Platform activity summary
CREATE VIEW analytics.platform_activity_summary AS
SELECT
    DATE_TRUNC('day', created_at) AS activity_date,
    COUNT(*) FILTER (WHERE status = 'active') AS active_sessions,
    COUNT(*) FILTER (WHERE status = 'completed') AS completed_sessions,
    COUNT(*) FILTER (WHERE status = 'pending_acceptance') AS pending_sessions,
    COUNT(*) FILTER (WHERE status = 'unresolved') AS unresolved_sessions
FROM analytics.sessions
GROUP BY DATE_TRUNC('day', created_at)
ORDER BY activity_date DESC;

COMMENT ON VIEW analytics.platform_activity_summary IS 'Daily session activity breakdown for platform health monitoring';

-- Mentor performance view
CREATE VIEW analytics.mentor_performance AS
SELECT
    s.mentor_id,
    u.display_name AS mentor_name,
    u.aws_chapter AS mentor_chapter,
    COUNT(*) AS total_sessions,
    COUNT(*) FILTER (WHERE s.status = 'completed') AS completed_sessions,
    ROUND(
        AVG(EXTRACT(EPOCH FROM (s.mentor_completed_at - s.mentee_completed_at)) / 3600)::NUMERIC,
        1
    ) AS avg_confirmation_hours,
    ROUND(AVG(s.checklist_completed)::NUMERIC / NULLIF(AVG(s.checklist_total), 0) * 100, 1) AS avg_checklist_completion_pct
FROM analytics.sessions s
LEFT JOIN analytics.users u ON s.mentor_id = u.user_id
GROUP BY s.mentor_id, u.display_name, u.aws_chapter;

COMMENT ON VIEW analytics.mentor_performance IS 'Mentor performance metrics for dashboards and admin reporting';

-- User engagement summary
CREATE VIEW analytics.user_engagement_summary AS
SELECT
    user_id,
    COUNT(*) AS total_events,
    COUNT(DISTINCT event_type) AS unique_event_types,
    MIN(occurred_at) AS first_event,
    MAX(occurred_at) AS last_event,
    COUNT(*) FILTER (WHERE occurred_at >= NOW() - INTERVAL '7 days') AS events_last_7_days,
    COUNT(*) FILTER (WHERE occurred_at >= NOW() - INTERVAL '30 days') AS events_last_30_days
FROM analytics.engagement_metrics
GROUP BY user_id;

COMMENT ON VIEW analytics.user_engagement_summary IS 'Per-user engagement summary for retention analysis';
