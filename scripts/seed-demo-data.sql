-- GuidedMentor Demo Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING

-- ============ USERS ============
INSERT INTO users (id, email, display_name, profile_photo_url, aws_chapter, city, active_role, mentor_onboarding_status, mentee_onboarding_status, is_disabled) VALUES
('11111111-1111-1111-1111-111111111111', 'sarah.chen@example.com', 'Sarah Chen', NULL, 'Sydney', 'Sydney', 'mentor', 'completed', 'not_started', false),
('22222222-2222-2222-2222-222222222222', 'james.nguyen@example.com', 'James Nguyen', NULL, 'Melbourne', 'Melbourne', 'mentor', 'completed', 'not_started', false),
('33333333-3333-3333-3333-333333333333', 'priya.sharma@example.com', 'Priya Sharma', NULL, 'Brisbane', 'Brisbane', 'mentor', 'completed', 'completed', false),
('44444444-4444-4444-4444-444444444444', 'david.kim@example.com', 'David Kim', NULL, 'Perth', 'Perth', 'mentor', 'completed', 'not_started', false),
('55555555-5555-5555-5555-555555555555', 'emma.wilson@example.com', 'Emma Wilson', NULL, 'Adelaide', 'Adelaide', 'mentor', 'completed', 'not_started', false),
('66666666-6666-6666-6666-666666666666', 'alex.patel@example.com', 'Alex Patel', NULL, 'Sydney', 'Sydney', 'mentee', 'not_started', 'completed', false),
('77777777-7777-7777-7777-777777777777', 'mia.johnson@example.com', 'Mia Johnson', NULL, 'Melbourne', 'Melbourne', 'mentee', 'not_started', 'completed', false),
('88888888-8888-8888-8888-888888888888', 'liam.brown@example.com', 'Liam Brown', NULL, 'Brisbane', 'Brisbane', 'mentee', 'not_started', 'completed', false),
('99999999-9999-9999-9999-999999999999', 'zara.khan@example.com', 'Zara Khan', NULL, 'Sydney', 'Sydney', 'mentee', 'not_started', 'completed', false),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'noah.garcia@example.com', 'Noah Garcia', NULL, 'Perth', 'Perth', 'mentee', 'not_started', 'completed', false),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'olivia.taylor@example.com', 'Olivia Taylor', NULL, 'Adelaide', 'Adelaide', 'mentee', 'not_started', 'completed', false),
('cccccccc-cccc-cccc-cccc-cccccccccccc', 'ethan.lee@example.com', 'Ethan Lee', NULL, 'Melbourne', 'Melbourne', 'mentee', 'not_started', 'completed', false),
('dddddddd-dddd-dddd-dddd-dddddddddddd', 'sophie.martinez@example.com', 'Sophie Martinez', NULL, 'Brisbane', 'Brisbane', 'mentee', 'not_started', 'completed', false)
ON CONFLICT (id) DO NOTHING;

-- ============ MENTORS ============
INSERT INTO mentors (id, user_id, expertise_areas, certifications, topics, years_of_experience, max_mentees, active_mentee_count, availability, session_formats, professional_title, company_name, bio, availability_status) VALUES
('a1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111',
 ARRAY['Cloud Architecture', 'Serverless', 'DevOps'], ARRAY['AWS Solutions Architect Professional', 'AWS DevOps Engineer'],
 ARRAY['Well-Architected Framework', 'Lambda', 'CDK'], 12, 3, 1,
 '{"monday": ["09:00-12:00"], "wednesday": ["14:00-17:00"]}',
 ARRAY['Video Call', 'In Person'], 'Principal Cloud Architect', 'AWS', 'Helping engineers build scalable cloud solutions for 12+ years.', 'available'),

('a2222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222',
 ARRAY['Machine Learning', 'Data Engineering', 'AI'], ARRAY['AWS Machine Learning Specialty', 'AWS Data Analytics Specialty'],
 ARRAY['SageMaker', 'Bedrock', 'Glue', 'EMR'], 8, 4, 2,
 '{"tuesday": ["10:00-13:00"], "thursday": ["15:00-18:00"]}',
 ARRAY['Video Call'], 'Senior ML Engineer', 'Atlassian', 'Passionate about democratising ML and making AI accessible to all developers.', 'available'),

('a3333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333',
 ARRAY['Security', 'Compliance', 'Networking'], ARRAY['AWS Security Specialty', 'CISSP'],
 ARRAY['IAM', 'VPC', 'GuardDuty', 'ISO 27001'], 15, 2, 1,
 '{"monday": ["13:00-16:00"], "friday": ["09:00-12:00"]}',
 ARRAY['Video Call', 'In Person'], 'Head of Cloud Security', 'Commonwealth Bank', 'Enterprise security leader focused on building secure-by-default architectures.', 'available'),

('a4444444-4444-4444-4444-444444444444', '44444444-4444-4444-4444-444444444444',
 ARRAY['Containers', 'Kubernetes', 'Microservices'], ARRAY['AWS Solutions Architect Associate', 'CKA'],
 ARRAY['EKS', 'ECS', 'Fargate', 'Service Mesh'], 6, 3, 0,
 '{"wednesday": ["09:00-12:00"], "saturday": ["10:00-13:00"]}',
 ARRAY['Video Call'], 'Platform Engineer', 'Canva', 'Building container platforms at scale. Love teaching Kubernetes to newcomers.', 'available'),

('a5555555-5555-5555-5555-555555555555', '55555555-5555-5555-5555-555555555555',
 ARRAY['Databases', 'Data Modelling', 'Migration'], ARRAY['AWS Database Specialty'],
 ARRAY['Aurora', 'DynamoDB', 'RDS', 'Migration Strategies'], 10, 3, 1,
 '{"tuesday": ["08:00-11:00"], "thursday": ["13:00-16:00"]}',
 ARRAY['Video Call', 'In Person'], 'Database Architect', 'REA Group', 'Database specialist helping teams choose the right data store for their workload.', 'available')
ON CONFLICT (id) DO NOTHING;

-- ============ MENTEES ============
INSERT INTO mentees (id, user_id, skills, experience_level, years_of_experience, primary_goal, goal_description, preferred_duration, availability, communication_preference) VALUES
('b6666666-6666-6666-6666-666666666666', '66666666-6666-6666-6666-666666666666',
 ARRAY['Python', 'Docker', 'Terraform'], 'intermediate', 3, 'certification_preparation',
 'Preparing for AWS Solutions Architect Professional exam', '3_months',
 '{"monday": ["18:00-20:00"], "wednesday": ["18:00-20:00"]}', 'video_call'),

('b7777777-7777-7777-7777-777777777777', '77777777-7777-7777-7777-777777777777',
 ARRAY['JavaScript', 'React', 'Node.js'], 'beginner', 1, 'career_transition',
 'Transitioning from frontend to full-stack cloud development', '6_months',
 '{"tuesday": ["19:00-21:00"], "saturday": ["10:00-12:00"]}', 'video_call'),

('b8888888-8888-8888-8888-888888888888', '88888888-8888-8888-8888-888888888888',
 ARRAY['Java', 'Spring Boot', 'AWS Lambda'], 'intermediate', 4, 'skill_development',
 'Want to master serverless architectures on AWS', '3_months',
 '{"wednesday": ["17:00-19:00"], "friday": ["17:00-19:00"]}', 'video_call'),

('b9999999-9999-9999-9999-999999999999', '99999999-9999-9999-9999-999999999999',
 ARRAY['Python', 'Machine Learning', 'TensorFlow'], 'intermediate', 2, 'skill_development',
 'Learning to deploy ML models on AWS SageMaker', '3_months',
 '{"monday": ["19:00-21:00"], "thursday": ["19:00-21:00"]}', 'video_call'),

('caaaaaa0-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
 ARRAY['C#', '.NET', 'Azure'], 'advanced', 7, 'career_transition',
 'Moving from Azure to AWS ecosystem', '3_months',
 '{"tuesday": ["12:00-14:00"], "thursday": ["12:00-14:00"]}', 'video_call'),

('cbbbbb00-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 ARRAY['SQL', 'PostgreSQL', 'Data Modelling'], 'beginner', 1, 'certification_preparation',
 'Preparing for AWS Database Specialty certification', '6_months',
 '{"wednesday": ["18:00-20:00"], "saturday": ["09:00-11:00"]}', 'video_call'),

('cccccc00-cccc-cccc-cccc-cccccccccccc', 'cccccccc-cccc-cccc-cccc-cccccccccccc',
 ARRAY['Kubernetes', 'Docker', 'CI/CD'], 'intermediate', 3, 'project_guidance',
 'Need guidance on migrating monolith to EKS microservices', '3_months',
 '{"monday": ["20:00-22:00"], "friday": ["20:00-22:00"]}', 'video_call'),

('cddddd00-dddd-dddd-dddd-dddddddddddd', 'dddddddd-dddd-dddd-dddd-dddddddddddd',
 ARRAY['Security', 'IAM', 'Compliance'], 'beginner', 1, 'skill_development',
 'Learning cloud security fundamentals for SOC analyst role', '6_months',
 '{"tuesday": ["17:00-19:00"], "thursday": ["17:00-19:00"]}', 'video_call')
ON CONFLICT (id) DO NOTHING;

-- ============ SESSIONS ============
INSERT INTO sessions (id, mentee_id, mentor_id, status, session_plan, checklist_state) VALUES
('d1111111-1111-1111-1111-111111111111', 'b6666666-6666-6666-6666-666666666666', 'a1111111-1111-1111-1111-111111111111',
 'active',
 '{"sessionTitle":"AWS Solutions Architect Pro - Study Strategy","agenda":[{"title":"Exam Overview","durationMinutes":5,"description":"Review certification exam domains"},{"title":"Knowledge Assessment","durationMinutes":10,"description":"Identify strengths and gaps"},{"title":"Study Plan Creation","durationMinutes":12,"description":"Build a week-by-week schedule"},{"title":"Resources Review","durationMinutes":8,"description":"Identify study materials"}],"preworkTasks":["Review the official exam guide","Take a practice assessment"],"followUpTasks":["Register for exam date","Complete first study module"]}',
 '{"prework": [{"id": "pw1", "title": "Review exam guide", "isCompleted": true}], "followup": []}'),

('d2222222-2222-2222-2222-222222222222', 'b7777777-7777-7777-7777-777777777777', 'a2222222-2222-2222-2222-222222222222',
 'active',
 '{"sessionTitle":"Career Transition: Frontend to Cloud","agenda":[{"title":"Background Review","durationMinutes":5,"description":"Discuss current skills"},{"title":"Skills Gap Analysis","durationMinutes":12,"description":"Identify what to learn"},{"title":"Learning Path","durationMinutes":10,"description":"Create timeline"},{"title":"Next Steps","durationMinutes":8,"description":"Set first goals"}],"preworkTasks":["List target roles","Identify 3 skills to develop"],"followUpTasks":["Set up AWS free tier","Complete first tutorial"]}',
 '{"prework": [], "followup": []}'),

('d3333333-3333-3333-3333-333333333333', 'b9999999-9999-9999-9999-999999999999', 'a2222222-2222-2222-2222-222222222222',
 'mentee_completed',
 '{"sessionTitle":"SageMaker Deployment Deep Dive","agenda":[{"title":"Current Progress","durationMinutes":5,"description":"Review what was learned"},{"title":"Model Deployment","durationMinutes":12,"description":"Hands-on SageMaker endpoints"},{"title":"Best Practices","durationMinutes":10,"description":"Production ML patterns"},{"title":"Action Items","durationMinutes":8,"description":"Next experiments"}],"preworkTasks":["Train a simple model locally","Read SageMaker docs"],"followUpTasks":["Deploy first endpoint","Set up CloudWatch monitoring"]}',
 '{"prework": [{"id": "pw1", "title": "Train a simple model", "isCompleted": true}], "followup": [{"id": "fu1", "title": "Deploy endpoint", "isCompleted": false}]}')
ON CONFLICT (id) DO NOTHING;

-- Update mentee_completed_at for the third session
UPDATE sessions SET mentee_completed_at = NOW() - INTERVAL '3 days' WHERE id = 'd3333333-3333-3333-3333-333333333333';

-- ============ NOTIFICATIONS ============
INSERT INTO notifications (id, recipient_user_id, type, message, action_url, is_read) VALUES
('f1111111-1111-1111-1111-111111111111', '66666666-6666-6666-6666-666666666666', 'session_accepted', 'Sarah Chen accepted your mentoring request!', '/sessions/s1111111-1111-1111-1111-111111111111', true),
('f2222222-2222-2222-2222-222222222222', '77777777-7777-7777-7777-777777777777', 'session_accepted', 'James Nguyen accepted your mentoring request!', '/sessions/s2222222-2222-2222-2222-222222222222', true),
('f3333333-3333-3333-3333-333333333333', '99999999-9999-9999-9999-999999999999', 'plan_generated', 'Your session plan is ready: SageMaker Deployment Deep Dive', '/sessions/s3333333-3333-3333-3333-333333333333/plan', true),
('f4444444-4444-4444-4444-444444444444', '66666666-6666-6666-6666-666666666666', 'opportunity_posted', 'New opportunity: Cloud Engineer at Canva', '/opportunities', false),
('f5555555-5555-5555-5555-555555555555', '77777777-7777-7777-7777-777777777777', 'meetup_reminder', 'Upcoming meetup: AWS Melbourne Monthly - Tomorrow at 6pm', '/meetups', false)
ON CONFLICT (id) DO NOTHING;

-- ============ OPPORTUNITIES ============
INSERT INTO opportunities (id, mentor_id, title, type, organisation_name, description, location, event_date_time, employment_type, required_skills, required_experience, external_url, is_active, published_at, expires_at) VALUES
('01111111-1111-1111-1111-111111111111', 'a4444444-4444-4444-4444-444444444444',
 'Senior Cloud Engineer', 'job', 'Canva',
 'Join our platform team to build and maintain Kubernetes infrastructure at scale. You will work on EKS clusters serving millions of users, implement service mesh patterns, and contribute to our internal developer platform.',
 'Sydney', NULL, 'full_time',
 ARRAY['Kubernetes', 'Docker', 'Terraform', 'Go'], 'intermediate',
 'https://canva.com/careers', true, NOW() - INTERVAL '5 days', NOW() + INTERVAL '25 days'),

('02222222-2222-2222-2222-222222222222', 'a3333333-3333-3333-3333-333333333333',
 'AWS Security Workshop - Zero Trust Architecture', 'workshop', 'AWS User Group Brisbane',
 'Hands-on workshop covering Zero Trust principles on AWS. Learn to implement least-privilege IAM, VPC endpoints, and GuardDuty threat detection. Bring your laptop with AWS free tier account.',
 'Brisbane', NOW() + INTERVAL '14 days', NULL,
 ARRAY['IAM', 'VPC', 'Security'], 'beginner',
 'https://meetup.com/aws-brisbane', true, NOW() - INTERVAL '3 days', NOW() + INTERVAL '14 days')
ON CONFLICT (id) DO NOTHING;

-- ============ MEETUPS ============
INSERT INTO meetups (id, chapter, title, event_date, start_time, end_time, venue_name, venue_address, event_url, created_by, is_cancelled, confirmed_attendees) VALUES
('ae111111-1111-1111-1111-111111111111', 'Melbourne', 'AWS Melbourne Monthly - Serverless Edition',
 CURRENT_DATE + INTERVAL '7 days', '18:00', '20:30',
 'Atlassian Sydney Tower', '341 George St, Sydney NSW 2000',
 'https://meetup.com/aws-melbourne', '22222222-2222-2222-2222-222222222222', false,
 ARRAY['77777777-7777-7777-7777-777777777777'::UUID, 'cccccccc-cccc-cccc-cccc-cccccccccccc'::UUID])
ON CONFLICT (id) DO NOTHING;

