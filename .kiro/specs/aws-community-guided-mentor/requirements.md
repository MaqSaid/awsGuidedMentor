# Requirements Document

## Introduction

GuidedMentor is an AI-powered mentorship platform for AWS Community Builders and AWS User Group communities across Australia. The platform connects developers seeking career guidance with experienced AWS professionals who volunteer their time. It uses a rule-based compatibility algorithm displaying percentage match scores and Amazon Bedrock (Claude Sonnet 4) to generate personalised session plans for matched pairs.

The platform is built on a microservices architecture using .NET 10 (C#) with Native AOT Lambda functions, React 19.2 with Module Federation (micro-frontends), DynamoDB for application data, Aurora PostgreSQL Serverless v2 for analytics, and Terraform for infrastructure. It follows API-first design (OpenAPI 3.1), Domain-Driven Design with bounded contexts (Identity, Mentoring, Content, Engagement), CQRS with MediatR, and Clean Architecture. The frontend uses Vercel AI SDK 6 for AI features, TailwindCSS 4 with a dark glassmorphism design system, and targets desktop web browsers only (mobile is a future enhancement).

Key differentiators: cross-city Australia matching (all chapters visible, same-city bonus but no exclusion), role toggle (users switch between mentor/mentee at any time), AI Help Assistant (floating chat), guided onboarding tour, and comprehensive observability with OpenTelemetry.

## Glossary

- **GuidedMentor_Platform**: The complete AI-powered mentorship web application including all micro-frontends and backend microservices
- **Authentication_Service**: The Amazon Cognito-based service handling user sign-up, sign-in, JWT issuance, Google OAuth, and account lockout
- **Role_Toggle_Service**: The service managing active role switching between mentor and mentee, ensuring only one role is active at a time
- **Onboarding_Engine**: The multi-step wizard that collects role-specific profile information from new users with progress persistence
- **Matching_Algorithm**: The rule-based scoring engine that computes 0-100 percentage compatibility scores between mentees and mentors across four dimensions
- **Session_Plan_Generator**: The service that invokes Claude Sonnet 4 via Amazon Bedrock Converse API to produce structured mentorship session agendas
- **Mentee_Dashboard**: The personalised view for mentees showing active sessions, recommendations, and progress
- **Mentor_Dashboard**: The personalised view for mentors showing pending requests, active mentees, and schedule
- **Browse_Page**: The page where mentees view available mentors from all Australian chapters with live percentage match scores
- **Session_Plan_Page**: The page displaying the timed agenda, pre-work items, follow-up tasks, and progress bar for a matched pair
- **Notification_Service**: The real-time notification delivery system using AWS AppSync GraphQL subscriptions
- **Completion_Flow**: The two-party mutual confirmation mechanism for marking sessions as complete
- **Locking_Mechanism**: The system that prevents multiple mentees from requesting the same mentor simultaneously using DynamoDB conditional writes
- **AI_Help_Assistant**: The floating chat interface powered by Vercel AI SDK useChat() and Bedrock Claude for contextual platform help
- **Onboarding_Tour**: The step-by-step overlay walkthrough for first-time users explaining platform features
- **API_Gateway**: The Amazon API Gateway serving as the entry point for all RESTful API calls with Cognito authorizer
- **Identity_Context**: The DDD bounded context handling authentication, authorisation, user profiles, and role management
- **Mentoring_Context**: The DDD bounded context handling matching, sessions, completion flows, and mentor-mentee relationships
- **Content_Context**: The DDD bounded context handling session plans, AI generation, and educational content
- **Engagement_Context**: The DDD bounded context handling notifications, onboarding tours, help assistant, and user engagement features
- **Users_Table**: The DynamoDB table storing core user records including active role and both role profiles
- **Mentors_Table**: The DynamoDB table storing mentor-specific profile and availability data
- **Mentees_Table**: The DynamoDB table storing mentee-specific goals and preferences
- **Sessions_Table**: The DynamoDB table storing session plan data, checklist state, and completion status
- **Notifications_Table**: The DynamoDB table storing notification records with read/unread status
- **Analytics_Database**: Aurora PostgreSQL Serverless v2 storing reporting data, cross-entity joins, and aggregated metrics
- **Resume_Storage**: The Amazon S3 bucket for optional resume uploads with CloudFront distribution
- **Compatibility_Score**: A numeric value from 0 to 100 representing the percentage match quality between a mentee and a mentor
- **Feature_Flag_Service**: AWS AppConfig-based feature flag system for progressive rollouts
- **Observability_Stack**: The combination of Serilog structured logging, OpenTelemetry distributed tracing, and CloudWatch metrics/alarms
- **Circuit_Breaker**: Polly v8-based resilience pattern that prevents cascading failures by short-circuiting requests to failing dependencies
- **Module_Federation_Host**: The shell React application that orchestrates micro-frontend loading per bounded context
- **Australian_Chapters**: The predefined list of AWS User Group chapters: Sydney, Melbourne, Brisbane, Perth, Adelaide, Canberra, Hobart, Darwin, Gold Coast, Newcastle, Wollongong, Geelong, Townsville
- **Job_Board**: The Opportunities Board feature allowing mentors to post jobs, workshops, events, or training opportunities (from any company or organisation) visible to mentees across the platform, with skill-matched notifications
- **Meetup_Calendar**: The calendar of upcoming AWS User Group meetup events used for session scheduling alignment
- **Engagement_Analytics**: The anonymous user activity tracking system that captures interactions, navigation patterns, and accessibility metrics to improve the platform experience
- **Chapter_Lead**: A mentor with elevated permissions to create and manage meetup events for their AWS chapter
- **Super_Admin**: A platform administrator with elevated permissions to manage all users, view platform-wide analytics, enable/disable platform features, and place the platform in maintenance mode during critical outages
- **Mentor_Availability_Status**: A toggle allowing mentors to set themselves as temporarily unavailable (vacation, personal commitment) without losing their profile or active sessions
- **Seed_Data_Generator**: The automated data seeding system that populates the platform with realistic simulation data for demo and testing purposes

## Requirements

### Requirement 1: User Authentication with Amazon Cognito

**User Story:** As a community member, I want to sign up and sign in using my Google account or email/password, so that I can securely access the platform without friction.

#### Acceptance Criteria

1. WHEN a new user initiates sign-up with Google OAuth, THE Authentication_Service SHALL create a new user record in the Cognito User Pool, issue a JWT access token (15-minute expiry) and rotating refresh token (7-day expiry), and redirect the user to role selection within 3 seconds
2. WHEN a new user initiates sign-up with email and password, THE Authentication_Service SHALL validate that the password is at least 12 characters long and contains at least one uppercase letter, one lowercase letter, one number, and one special character, send an email verification code that expires after 10 minutes, and create the user record in a pending state
3. WHEN a user provides a valid verification code within 10 minutes of issuance and within 5 attempts, THE Authentication_Service SHALL activate the user account and redirect to role selection
4. WHEN a returning user signs in with valid credentials, THE Authentication_Service SHALL issue a JWT access token (15-minute expiry) and rotating refresh token (7-day expiry), and redirect to the dashboard corresponding to the user active role within 2 seconds
5. IF an authentication attempt fails due to invalid credentials, THEN THE Authentication_Service SHALL display a generic error message stating "Email or password is incorrect" without specifying which field failed, and increment the failed attempt counter
6. IF the failed attempt counter reaches 5 within 15 minutes, THEN THE Authentication_Service SHALL lock the account for 30 minutes and notify the user via email with account recovery instructions
7. WHEN a user signs out, THE Authentication_Service SHALL invalidate the current access token and refresh token, clear all client-side session data, and redirect to the landing page
8. IF Google OAuth sign-up or sign-in fails due to user denial or provider error, THEN THE Authentication_Service SHALL redirect the user to the sign-in page and display an error message indicating that authentication with Google was not completed
9. IF a user exceeds 5 verification code attempts or the code has expired, THEN THE Authentication_Service SHALL invalidate the code and allow the user to request a new verification code
10. WHEN a JWT access token expires, THE Authentication_Service SHALL use the refresh token to obtain a new access token without requiring the user to re-authenticate

### Requirement 2: Role Selection and Role Toggle

**User Story:** As a user, I want to select my initial role and toggle between mentor and mentee roles at any time, so that I can participate in the community in both capacities.

#### Acceptance Criteria

1. WHEN a new user completes authentication for the first time, THE GuidedMentor_Platform SHALL present a role selection screen with two options: Mentor and Mentee
2. WHEN the user selects an initial role, THE Role_Toggle_Service SHALL persist the active role in the Users_Table and route the user to the corresponding onboarding flow within 2 seconds
3. WHILE a user has not completed role selection, THE GuidedMentor_Platform SHALL redirect all navigation attempts back to the role selection screen
4. WHEN a user activates the role toggle control, THE Role_Toggle_Service SHALL switch the active role to the alternate role and update the Users_Table within 2 seconds
5. WHEN a user toggles to a role for the first time (no completed onboarding for that role), THE Role_Toggle_Service SHALL route the user to the onboarding flow for the new role
6. WHEN a user toggles to a role they have previously completed onboarding for, THE Role_Toggle_Service SHALL load the persisted profile for that role and display the corresponding dashboard
7. THE Role_Toggle_Service SHALL ensure only one role is active at any time, and all UI components, navigation, and functionality SHALL adapt to reflect the active role
8. THE Role_Toggle_Service SHALL persist both mentor and mentee profiles independently, so toggling does not overwrite or delete the inactive role profile
9. IF the role toggle persistence to the Users_Table fails, THEN THE Role_Toggle_Service SHALL revert the toggle to the previous active role and display an error message indicating the role could not be switched

### Requirement 3: Mentee Onboarding

**User Story:** As a new mentee, I want to provide my goals, skills, and preferences through a guided wizard, so that the platform can find compatible mentors for me.

#### Acceptance Criteria

1. WHEN a mentee begins onboarding, THE Onboarding_Engine SHALL display a 4-step wizard with a visual progress indicator showing the current step and remaining steps
2. THE Onboarding_Engine SHALL collect the following in Step 1 (Profile): full name (2-100 characters), optional profile photo (JPEG or PNG format, maximum 2 MB — can be skipped and uploaded later from Settings), AWS User Group chapter (selected from Australian_Chapters predefined list), and city
3. THE Onboarding_Engine SHALL collect the following in Step 2 (Skills): current AWS skills (multi-select from predefined list, minimum 1, maximum 10 selections), experience level (beginner, intermediate, advanced), and years of experience (integer from 0 to 50)
4. THE Onboarding_Engine SHALL collect the following in Step 3 (Goals): primary goal (career transition, skill development, certification preparation, project guidance), goal description (free text, 50-500 characters), and preferred mentorship duration (4 weeks, 8 weeks, 12 weeks)
5. THE Onboarding_Engine SHALL collect the following in Step 4 (Preferences): availability (days and time slots), preferred communication method (video call, voice call, chat), and optional resume upload (PDF or DOCX format, maximum 5 MB)
6. WHEN the mentee completes all 4 steps and all required fields pass validation, THE Onboarding_Engine SHALL persist the profile data to the Mentees_Table, set onboardingStatus to completed, and redirect to the Mentee_Dashboard within 3 seconds
7. IF the mentee navigates away before completing onboarding, THEN THE Onboarding_Engine SHALL save the current progress and resume from the last completed step on next visit
8. IF the mentee submits a step with one or more invalid or missing required fields, THEN THE Onboarding_Engine SHALL remain on the current step, highlight each invalid field with inline error messages, and announce errors via aria-live region for screen readers
9. WHEN a resume file is provided, THE Onboarding_Engine SHALL validate the file format and size, store it in Resume_Storage, and associate the S3 key with the mentee record

### Requirement 4: Mentor Onboarding

**User Story:** As a new mentor, I want to provide my expertise and availability through a guided wizard, so that the platform can match me with compatible mentees.

#### Acceptance Criteria

1. WHEN a mentor begins onboarding, THE Onboarding_Engine SHALL display a 3-step wizard with a visual progress indicator showing the current step and remaining steps
2. THE Onboarding_Engine SHALL collect the following in Step 1 (Profile): full name (2-100 characters), optional profile photo (JPEG or PNG format, maximum 2 MB — can be skipped and uploaded later from Settings), AWS User Group chapter (selected from Australian_Chapters predefined list), professional title (2-100 characters), and company name (2-100 characters)
3. THE Onboarding_Engine SHALL collect the following in Step 2 (Expertise): AWS expertise areas (multi-select from predefined list, minimum 1, maximum 10 selections), years of AWS experience (integer from 1 to 30), AWS certifications held (multi-select from predefined list, zero or more), and topics willing to mentor on (multi-select from predefined list, minimum 1, maximum 10 selections)
4. THE Onboarding_Engine SHALL collect the following in Step 3 (Availability): maximum number of active mentees (1-5), availability (one or more days of the week with one or more time slots per day), preferred session format (video call, voice call, chat — one or more selections), and a short bio (100-1000 characters)
5. WHEN the mentor completes all 3 steps and all fields pass validation, THE Onboarding_Engine SHALL persist the profile data to the Mentors_Table, set onboardingStatus to completed, and redirect to the Mentor_Dashboard within 3 seconds
6. IF the mentor navigates away before completing onboarding, THEN THE Onboarding_Engine SHALL save the current progress and resume from the last completed step on next visit
7. IF the mentor submits a step with one or more invalid or missing required fields, THEN THE Onboarding_Engine SHALL remain on the current step, highlight each invalid field with inline error messages, and announce errors via aria-live region for screen readers

### Requirement 5: Matching Algorithm

**User Story:** As a mentee, I want to see mentors ranked by percentage match score from all Australian chapters, so that I can choose the best-suited mentor regardless of location.

#### Acceptance Criteria

1. WHEN a mentee navigates to the Browse_Page, THE Matching_Algorithm SHALL compute a Compatibility_Score for every available mentor across all Australian_Chapters and display ranked results within 3 seconds
2. THE Matching_Algorithm SHALL calculate the chapter dimension: +30 points when the mentee and mentor belong to the same chapter, +15 points when the mentee and mentor are in the same city but different chapters, +0 points otherwise (cross-city matches are included but receive no chapter bonus)
3. THE Matching_Algorithm SHALL calculate the skills overlap dimension: round((number of skills present in both the mentee skills list and the mentor expertise areas list / total number of mentee skills) multiplied by 30) to the nearest integer
4. THE Matching_Algorithm SHALL calculate the goal-topic alignment dimension: round((number of mentor topics that match the mentee primary goal category / total number of mentor topics related to goal categories) multiplied by 25) to the nearest integer
5. THE Matching_Algorithm SHALL calculate the experience level dimension: +15 points when the mentor has at least 2 more years of experience than the mentee, +10 points when the mentor has exactly 1 more year, +5 points when experience years are equal, +0 points when the mentee has more experience than the mentor
6. IF the mentee has zero skills listed, THEN THE Matching_Algorithm SHALL assign 0 points for the skills overlap dimension
7. IF the mentee has zero goals listed, THEN THE Matching_Algorithm SHALL assign 0 points for the goal-topic alignment dimension
8. THE Matching_Algorithm SHALL produce a final Compatibility_Score by summing all four dimension scores, producing a value between 0 and 100, displayed as a percentage (for example "87% match")
9. THE Matching_Algorithm SHALL sort mentors in descending order of Compatibility_Score, and WHEN two or more mentors have an equal Compatibility_Score, THE Matching_Algorithm SHALL sort them alphabetically by display name
10. WHILE a mentor has reached their maximum active mentee count, THE Matching_Algorithm SHALL exclude that mentor from the Browse_Page results
11. IF no available mentors exist after filtering, THEN THE Matching_Algorithm SHALL display an empty state with guidance message and a call-to-action to adjust filters or check back later

### Requirement 6: Mentor Browse and Selection

**User Story:** As a mentee, I want to browse available mentors with their percentage match scores and lock my preferred choice, so that I can initiate a mentorship relationship.

#### Acceptance Criteria

1. WHEN the mentee views the Browse_Page, THE GuidedMentor_Platform SHALL display mentors in pages of 12 per page, showing each mentor card with profile photo, name, title, chapter, expertise areas (up to 5 visible with overflow indicator), availability summary, and Compatibility_Score displayed as percentage (for example "87% match")
2. WHEN the mentee selects a mentor, THE Locking_Mechanism SHALL place a 15-minute hold on that mentor-mentee pairing using a DynamoDB conditional write, preventing other mentees from requesting the same mentor during that period
3. IF the mentee already has an active lock on another mentor, THEN THE Locking_Mechanism SHALL reject the new lock request and display a message indicating the mentee must release or confirm their current selection first
4. IF the mentee does not confirm the request within 15 minutes, THEN THE Locking_Mechanism SHALL release the hold automatically via a TTL-based expiration and make the mentor available to other mentees
5. WHEN the mentee confirms the mentor selection, THE GuidedMentor_Platform SHALL create a pending session record in the Sessions_Table and notify the mentor via the Notification_Service
6. WHEN the mentee cancels the lock before the 15-minute hold expires, THE Locking_Mechanism SHALL release the hold immediately and make the mentor available to other mentees
7. IF another mentee attempts to request a locked mentor, THEN THE GuidedMentor_Platform SHALL display a message indicating the mentor is temporarily unavailable and visually distinguish the locked mentor card with a muted overlay
8. THE Browse_Page SHALL support keyboard navigation between mentor cards, with each card focusable and actionable via Enter key

### Requirement 7: AI Session Plan Generation via Amazon Bedrock

**User Story:** As a matched mentee-mentor pair, I want an AI-generated personalised session plan, so that our mentorship sessions have clear structure, goals, and follow-up actions.

#### Acceptance Criteria

1. WHEN a mentor accepts a mentorship request, THE Session_Plan_Generator SHALL invoke Claude Sonnet 4 via Amazon Bedrock Converse API with the mentee profile, mentor profile, and mentee goals as structured input (no RAG, no knowledge bases — direct inference only)
2. THE Session_Plan_Generator SHALL produce a structured JSON response containing: session title (maximum 100 characters), 35-minute timed agenda (list of 3-7 items each with title, duration in minutes with a minimum of 3 minutes per item, and description of up to 500 characters), pre-work tasks for the mentee (list of 2-5 items each up to 200 characters), and follow-up tasks (list of 2-5 items each up to 200 characters)
3. THE Session_Plan_Generator SHALL validate that the generated response conforms to the expected JSON schema and that the sum of all agenda item durations equals exactly 35 minutes
4. IF the generated response fails schema validation or the agenda durations do not sum to 35 minutes, THEN THE Session_Plan_Generator SHALL discard the response and re-invoke the API, counting the attempt toward the maximum retry limit
5. IF the Bedrock Converse API call fails or times out after 30 seconds, THEN THE Session_Plan_Generator SHALL retry the request up to 3 times with exponential backoff (2-second, 4-second, 8-second delays) using the Circuit_Breaker pattern
6. IF all retry attempts fail, THEN THE Session_Plan_Generator SHALL store the session in a pending-plan state, publish a retry event to EventBridge for asynchronous processing, and notify both parties that the plan is being generated
7. WHEN the session plan is successfully generated, THE Session_Plan_Generator SHALL persist the plan to the Sessions_Table and notify both parties via the Notification_Service
8. THE Session_Plan_Generator SHALL use Microsoft.Extensions.AI IChatClient abstraction with Semantic Kernel plugins for prompt construction and response parsing on the backend
9. THE Session_Plan_Generator SHALL log Bedrock token usage (input tokens, output tokens) as custom CloudWatch metrics for cost monitoring
10. THE Session_Plan_Generator SHALL sanitize all user-provided input (mentee goals, descriptions) before including them in the Bedrock prompt to prevent prompt injection attacks, by stripping control characters, escaping special delimiters, and enforcing a maximum input length of 2000 characters per field
11. THE Session_Plan_Generator SHALL configure Amazon Bedrock Guardrails with: content filters blocking harmful/toxic content, denied topic filters preventing off-platform discussion, PII redaction (automatically mask email addresses, phone numbers, and physical addresses from AI outputs), and word filters blocking profanity
12. THE Session_Plan_Generator SHALL validate all AI-generated output before persisting or displaying, ensuring no PII is present in the session plan, no harmful content is included, and the response conforms strictly to the expected JSON schema

### Requirement 8: Session Plan Page

**User Story:** As a mentee or mentor, I want to view my session plan with a timed agenda and checklist items, so that I can prepare for and track progress through our session.

#### Acceptance Criteria

1. WHEN a user navigates to the Session_Plan_Page, THE GuidedMentor_Platform SHALL display the session title, timed agenda with duration labels, pre-work checklist, and follow-up checklist
2. THE GuidedMentor_Platform SHALL render each agenda item with its title, allocated time in minutes, and description in a visually distinct card format
3. WHEN a user checks or unchecks a pre-work or follow-up item, THE GuidedMentor_Platform SHALL persist the completion state to the Sessions_Table within 2 seconds and display a visual confirmation
4. IF the persistence of a checklist item state fails, THEN THE GuidedMentor_Platform SHALL revert the checkbox to its previous state and display an error message with a retry button
5. THE GuidedMentor_Platform SHALL display a progress bar showing the percentage of completed checklist items calculated as (total checked items across both pre-work and follow-up checklists / total number of checklist items) multiplied by 100, rounded to the nearest integer
6. IF a user navigates to the Session_Plan_Page for a session in pending-plan state, THEN THE GuidedMentor_Platform SHALL display a loading skeleton with a message indicating the session plan is being generated
7. THE Session_Plan_Page SHALL stream the session plan content to the frontend using Vercel AI SDK useObject() hook when the plan is being generated in real-time, displaying incremental content as it arrives

### Requirement 9: Two-Party Completion Flow

**User Story:** As a mentee or mentor, I want a mutual completion mechanism so that both parties confirm the session was held and the mentorship relationship has accountability.

#### Acceptance Criteria

1. WHEN a mentee marks a session as complete, THE Completion_Flow SHALL record the mentee completion timestamp in the Sessions_Table and notify the mentor to confirm via the Notification_Service
2. WHEN the mentor confirms session completion after the mentee has marked it complete, THE Completion_Flow SHALL set the session status to completed, record the mentor completion timestamp, and publish a session-completed event to EventBridge
3. IF only one party marks the session as complete and the other party does not confirm within 7 days, THEN THE Completion_Flow SHALL send a reminder notification to the non-confirming party
4. IF the non-confirming party does not respond within 14 days of the initial completion mark, THEN THE Completion_Flow SHALL escalate the session to an unresolved state and notify both parties
5. IF a mentor attempts to mark a session as complete before the mentee has done so, THEN THE Completion_Flow SHALL reject the action and display a message indicating the mentee must mark completion first
6. WHEN a mentee marks a session as complete, THE Completion_Flow SHALL prevent the mentee from retracting the completion mark
7. WHEN a session reaches completed status, THE Completion_Flow SHALL update the mentor activeMenteeCount (decrement by 1) and make the mentor available for new mentees if below capacity

### Requirement 10: Mentee Dashboard

**User Story:** As a mentee, I want a personalised dashboard showing my active sessions, recommended mentors, and progress, so that I can manage my mentorship journey.

#### Acceptance Criteria

1. WHEN a mentee navigates to the Mentee_Dashboard, THE GuidedMentor_Platform SHALL display: active session cards (mentor name, session title, next incomplete follow-up task, and progress percentage), top 3 recommended mentors ranked by Compatibility_Score, and progress statistics (completed sessions count, total checklist items completed, overall completion percentage)
2. THE Mentee_Dashboard SHALL display a count of completed sessions, in-progress sessions, and pending requests in a summary bar
3. WHEN a mentee has no active sessions, THE Mentee_Dashboard SHALL display an empty state with a call-to-action prompting the mentee to browse available mentors
4. WHEN a mentee navigates to the Mentee_Dashboard, THE GuidedMentor_Platform SHALL load and display all dashboard data within 3 seconds without requiring manual refresh
5. IF the Mentee_Dashboard fails to retrieve session, recommendation, or progress data, THEN THE GuidedMentor_Platform SHALL display an error message for the failed section with a retry button while preserving any successfully loaded sections

### Requirement 11: Mentor Dashboard

**User Story:** As a mentor, I want a personalised dashboard showing pending requests, active mentees, and my schedule, so that I can manage my mentoring commitments.

#### Acceptance Criteria

1. WHEN a mentor navigates to the Mentor_Dashboard, THE GuidedMentor_Platform SHALL display: pending mentorship requests (ordered by request date, oldest first) with mentee names, goals, and Compatibility_Scores; active mentee cards with session status and progress; and the current capacity indicator (active mentees / maximum capacity)
2. WHEN a mentor receives a new mentorship request, THE Mentor_Dashboard SHALL display the request with accept and decline action buttons and the mentee Compatibility_Score as a percentage
3. WHEN the mentor accepts a request and the mentor active mentee count is below maximum capacity, THE Mentor_Dashboard SHALL trigger session plan generation and update the request status to active
4. IF the mentor attempts to accept a request while their active mentee count equals their maximum capacity, THEN THE Mentor_Dashboard SHALL prevent the acceptance and display a message indicating the mentor has reached maximum mentee limit
5. WHEN the mentor declines a request, THE GuidedMentor_Platform SHALL notify the mentee via the Notification_Service, release the mentor slot, and remove the request from the dashboard
6. THE Mentor_Dashboard SHALL display the current active mentee count relative to the mentor maximum capacity as a visual indicator
7. IF the Mentor_Dashboard fails to load data from the backend, THEN THE GuidedMentor_Platform SHALL display an error message with a retry button

### Requirement 12: Real-Time Notifications via AppSync

**User Story:** As a user, I want to receive real-time notifications about mentorship events, so that I can respond to requests and updates promptly.

#### Acceptance Criteria

1. WHEN a mentorship event occurs (request sent, request accepted, request declined, session plan ready, completion marked, reminder due), THE Notification_Service SHALL deliver a real-time notification to the relevant recipient via AWS AppSync GraphQL subscription within 5 seconds
2. THE Notification_Service SHALL persist all notifications in the Notifications_Table with: notificationId (partition key), recipientUserId (GSI), timestamp, type (enum), message (maximum 500 characters), read status (default unread), and actionUrl (deep link to relevant page)
3. WHEN a user views their notification panel, THE Notification_Service SHALL display the most recent 50 notifications in reverse chronological order, with unread items distinguished by bold title and unread indicator
4. WHEN a user clicks a notification, THE Notification_Service SHALL mark it as read and navigate to the associated page via the actionUrl
5. THE Notification_Service SHALL display an unread notification count badge on the navigation bar, showing the exact count for values 1 through 99 and displaying "99+" for counts exceeding 99
6. IF the AppSync subscription connection is lost, THEN THE Notification_Service SHALL attempt automatic reconnection with exponential backoff and deliver missed notifications when the connection is re-established
7. THE Notification_Service SHALL support batch mark-as-read for clearing all unread notifications

### Requirement 13: User Settings and Profile Management

**User Story:** As a user, I want to edit my profile information and manage my account settings, so that I can keep my information current.

#### Acceptance Criteria

1. WHEN a user navigates to the settings page, THE GuidedMentor_Platform SHALL display editable fields for all profile information collected during onboarding for the currently active role
2. WHEN a user updates profile fields and saves, THE GuidedMentor_Platform SHALL validate the inputs against the same rules enforced during onboarding (using FluentValidation on the backend) and persist changes to the appropriate DynamoDB table within 2 seconds
3. IF validation fails on any field during profile update, THEN THE GuidedMentor_Platform SHALL highlight the invalid fields with inline error messages and prevent the save operation
4. WHEN a user uploads a new resume, THE GuidedMentor_Platform SHALL replace the existing file in Resume_Storage and update the file reference in the user record
5. IF the resume upload fails due to network or S3 error, THEN THE GuidedMentor_Platform SHALL retain the previous resume file unchanged and display an error message with a retry button
6. THE GuidedMentor_Platform SHALL allow mentors to update their maximum active mentee count only to a value equal to or greater than their current active mentee count
7. IF a user changes their AWS User Group chapter, THEN THE Matching_Algorithm SHALL recalculate Compatibility_Scores for that user on subsequent Browse_Page visits
8. THE GuidedMentor_Platform SHALL display the inactive role profile (if onboarded) as read-only on the settings page with a prompt to toggle roles to edit

### Requirement 14: AI Help Assistant

**User Story:** As a user, I want a floating AI chat assistant that can answer questions about the platform, so that I can get contextual help without leaving my current page.

#### Acceptance Criteria

1. THE AI_Help_Assistant SHALL render as a floating chat bubble in the bottom-right corner of every authenticated page, accessible via click or keyboard shortcut (Ctrl+H)
2. WHEN a user opens the AI_Help_Assistant, THE GuidedMentor_Platform SHALL display a chat interface implemented with Vercel AI SDK useChat() hook, supporting streaming responses from Claude Sonnet 4 via Amazon Bedrock
3. THE AI_Help_Assistant SHALL answer questions about platform features, navigation, onboarding steps, matching algorithm explanation, and general platform usage based on a system prompt containing platform documentation
4. THE AI_Help_Assistant SHALL maintain conversation context within the current browser session (cleared on page refresh or sign-out)
5. IF the Bedrock API call for the AI_Help_Assistant fails, THEN THE GuidedMentor_Platform SHALL display a friendly error message and offer to retry the request
6. THE AI_Help_Assistant SHALL limit responses to platform-related topics and politely redirect off-topic questions back to platform help
7. THE AI_Help_Assistant SHALL be dismissible (closeable) and remember the collapsed/expanded state during the session
8. THE AI_Help_Assistant SHALL support keyboard navigation and be accessible via screen readers with appropriate ARIA labels on the chat container and messages
9. THE AI_Help_Assistant SHALL sanitize all user messages before sending to Bedrock to prevent prompt injection, by stripping system prompt override attempts, control characters, and enforcing a maximum message length of 1000 characters
10. THE AI_Help_Assistant SHALL configure Amazon Bedrock Guardrails to prevent: disclosure of system prompts, generation of harmful content, responses about topics unrelated to the platform, and leakage of other users' data
11. THE AI_Help_Assistant SHALL implement rate limiting of 20 messages per minute per user to prevent AI abuse

### Requirement 15: Serverless API Layer (.NET 10 Microservices)

**User Story:** As a developer, I want a serverless API built with .NET 10 microservices using Native AOT Lambda and API-first design, so that the platform scales automatically with minimal cold starts and costs are usage-based.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL expose RESTful API endpoints via Amazon API Gateway with OpenAPI 3.1 specifications defined first (API-first design) for all operations across Identity, Mentoring, Content, and Engagement bounded contexts
2. WHEN an API request is received, THE API_Gateway SHALL authenticate the request using the Cognito JWT access token via a Lambda authorizer and reject unauthorized requests with a 401 status code
3. WHEN an API request targets a resource owned by a different user, THE GuidedMentor_Platform SHALL reject the request with a 403 status code (resource-based access control)
4. THE GuidedMentor_Platform SHALL implement each API endpoint as an individual .NET 10 Lambda function compiled with Native AOT for sub-100ms cold starts, with maximum execution timeout of 30 seconds
5. THE GuidedMentor_Platform SHALL implement CQRS using MediatR, separating command handlers (write operations) from query handlers (read operations) within each microservice
6. IF a Lambda function encounters an unhandled exception, THEN THE GuidedMentor_Platform SHALL return a 500 status code with a generic error message (no stack traces or internal details) and log the full error with correlation ID to CloudWatch via Serilog
7. IF an API request contains an invalid or malformed payload, THEN THE GuidedMentor_Platform SHALL return a 400 status code with structured validation errors from FluentValidation indicating which fields are invalid
8. THE GuidedMentor_Platform SHALL enforce API rate limiting of 100 requests per minute per authenticated user, returning a 429 status code with Retry-After header when the limit is exceeded
9. THE GuidedMentor_Platform SHALL enforce a maximum request body size of 1 MB on all API endpoints
10. THE GuidedMentor_Platform SHALL include a correlation ID (X-Correlation-Id header) in all API responses for distributed tracing via OpenTelemetry
11. THE GuidedMentor_Platform SHALL enforce CORS policies on API Gateway allowing only the registered frontend origin (guidedmentor.dev and localhost for development), rejecting requests from all other origins

### Requirement 16: Data Model (DynamoDB + Aurora PostgreSQL)

**User Story:** As a developer, I want a well-designed data model with DynamoDB for application data and Aurora PostgreSQL for analytics, so that the platform supports efficient queries for all access patterns and cross-entity reporting.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL maintain five DynamoDB tables (on-demand capacity): Users_Table, Mentors_Table, Mentees_Table, Sessions_Table, and Notifications_Table
2. THE Users_Table SHALL store: userId (partition key), email, activeRole (enum: mentor, mentee), mentorOnboardingStatus (enum: not_started, in_progress, completed), menteeOnboardingStatus (enum: not_started, in_progress, completed), displayName, profilePhotoUrl, awsChapter, city, createdAt, and updatedAt
3. THE Mentors_Table SHALL store: mentorId (partition key), userId (GSI partition key), expertiseAreas (list, maximum 10 items), certifications (list), topics (list, maximum 10 items), maxMentees (integer, 1-5), activeMenteeCount (integer), availability (map), bio (string, 100-1000 characters), professionalTitle, companyName, yearsOfExperience (integer, 1-30), and onboardingStatus
4. THE Mentees_Table SHALL store: menteeId (partition key), userId (GSI partition key), skills (list, maximum 10 items), experienceLevel (enum: beginner, intermediate, advanced), yearsOfExperience (integer, 0-50), primaryGoal (enum: career_transition, skill_development, certification_preparation, project_guidance), goalDescription (string, 50-500 characters), preferredDuration (enum: 4_weeks, 8_weeks, 12_weeks), availability (map), communicationPreference (enum: video_call, voice_call, chat), resumeUrl (optional), and onboardingStatus
5. THE Sessions_Table SHALL store: sessionId (partition key), menteeId (GSI partition key), mentorId (GSI partition key), status (enum: pending_acceptance, pending_plan, active, mentee_completed, completed, unresolved), sessionPlan (map containing title, agenda, prework, followup), checklistState (map), menteeCompletedAt (timestamp), mentorCompletedAt (timestamp), createdAt, and updatedAt
6. THE Notifications_Table SHALL store: notificationId (partition key), recipientUserId (GSI partition key with sortKey of timestamp), type (enum), message (string, maximum 500 characters), readStatus (boolean), actionUrl (string), and createdAt
7. THE Analytics_Database (Aurora PostgreSQL Serverless v2) SHALL store denormalised reporting data replicated from DynamoDB via DynamoDB Streams and Lambda, supporting cross-entity joins for: match success rates, session completion rates by chapter, mentor utilisation, and platform engagement metrics
8. THE GuidedMentor_Platform SHALL enable DynamoDB point-in-time recovery (PITR) on all five tables

### Requirement 17: AI Agent Architecture (Microsoft.Extensions.AI + Semantic Kernel)

**User Story:** As a developer, I want a structured AI integration layer using Microsoft.Extensions.AI and Semantic Kernel, so that AI capabilities are abstracted, testable, and maintainable.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL use Microsoft.Extensions.AI IChatClient interface as the abstraction layer for all Bedrock Claude interactions, enabling dependency injection and testability
2. THE GuidedMentor_Platform SHALL implement Semantic Kernel plugins for: session plan prompt construction (SessionPlanPlugin), help assistant system prompt management (HelpAssistantPlugin), and response validation (ValidationPlugin)
3. THE Session_Plan_Generator SHALL construct prompts using Semantic Kernel prompt templates with typed input variables (mentee profile, mentor profile, goals) and structured output parsing
4. THE AI_Help_Assistant SHALL use a dedicated Semantic Kernel plugin that maintains platform documentation as a system prompt and constrains responses to platform-relevant topics
5. THE GuidedMentor_Platform SHALL implement the IChatClient with Amazon Bedrock Converse API as the backing provider, configurable via dependency injection for unit testing with mock implementations
6. IF the AI integration layer detects a response that does not conform to the expected schema, THEN THE GuidedMentor_Platform SHALL log the malformed response for debugging and trigger the retry mechanism
7. THE GuidedMentor_Platform SHALL version all prompt templates and store them as embedded resources within the Content_Context microservice

### Requirement 18: Frontend Design System (React 19.2, Module Federation, Accessibility)

**User Story:** As a developer, I want a modular frontend with a dark glassmorphism design system, micro-frontend architecture, and WCAG 2.1 AA compliance, so that the UI is consistent, scalable, and accessible to all users.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL implement the frontend using React 19.2 with TypeScript, TailwindCSS 4, Vite 6, and React Compiler for automatic memoization
2. THE Module_Federation_Host SHALL load micro-frontends per bounded context: Identity (auth, onboarding, settings), Mentoring (browse, matching, sessions), Content (session plans, AI features), and Engagement (notifications, help assistant, onboarding tour)
3. THE GuidedMentor_Platform SHALL implement a dark glassmorphism design system with: semi-transparent cards with backdrop blur, gradient accents, consistent border-radius (12px), and smooth transitions respecting prefers-reduced-motion
4. THE GuidedMentor_Platform SHALL target desktop web browsers only (minimum viewport width 1024px) with responsive behaviour down to 1024px; mobile viewports SHALL display a message indicating mobile support is a future enhancement
5. THE GuidedMentor_Platform SHALL comply with WCAG 2.1 AA: keyboard navigation for all interactive elements, ARIA labels on all form controls and interactive components, focus management in modals and wizards, colour contrast minimum 4.5:1 for normal text and 3:1 for large text
6. THE GuidedMentor_Platform SHALL implement a skip-navigation link as the first focusable element on every page
7. THE GuidedMentor_Platform SHALL support text scaling at 200% zoom without content overflow or loss of functionality
8. THE GuidedMentor_Platform SHALL use aria-live regions to announce dynamic content changes (form errors, notification counts, loading states) to screen readers
9. THE GuidedMentor_Platform SHALL use Vercel AI SDK 6 (useChat for help assistant, useObject for session plan streaming) for all frontend AI interactions
10. THE GuidedMentor_Platform SHALL enforce axe-core accessibility checks in CI, blocking pull requests if the accessibility score falls below 90

### Requirement 19: File Storage (S3, Optional Resume)

**User Story:** As a user, I want to optionally upload my resume during onboarding, so that my mentor has additional context about my background.

#### Acceptance Criteria

1. WHEN a user uploads a resume file, THE GuidedMentor_Platform SHALL validate that the file is PDF or DOCX format and does not exceed 5 MB before uploading to Resume_Storage
2. THE GuidedMentor_Platform SHALL generate a pre-signed S3 upload URL (valid for 5 minutes) for client-side direct upload, avoiding passing file content through Lambda
3. THE GuidedMentor_Platform SHALL store resume files in S3 with server-side encryption (AES-256) and enable S3 versioning for file recovery
4. WHEN a mentor views a matched mentee profile, THE GuidedMentor_Platform SHALL generate a pre-signed S3 download URL (valid for 15 minutes) for the resume if one exists
5. IF a resume upload fails due to network interruption, THEN THE GuidedMentor_Platform SHALL display an error message and allow the user to retry without losing other form data
6. THE GuidedMentor_Platform SHALL serve the SPA static assets (HTML, JS, CSS) via CloudFront with S3 origin, with cache invalidation on deployments
7. THE Resume_Storage SHALL enforce a bucket policy that denies all public access and allows access only via pre-signed URLs or CloudFront OAI

### Requirement 20: Background Jobs (EventBridge)

**User Story:** As a developer, I want scheduled and event-driven background jobs, so that the platform handles asynchronous operations like reminders, retries, and data synchronisation reliably.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL use Amazon EventBridge as the event bus for all asynchronous operations across bounded contexts
2. WHEN a session completion reminder is due (7 days after first-party completion), THE GuidedMentor_Platform SHALL publish a reminder event to EventBridge, which triggers a Lambda function to send the notification
3. WHEN a session plan generation fails all retries, THE GuidedMentor_Platform SHALL publish a retry event to EventBridge with a scheduled delay of 5 minutes for asynchronous retry processing
4. THE GuidedMentor_Platform SHALL use EventBridge rules to trigger DynamoDB-to-Aurora data replication Lambda functions when DynamoDB Stream events indicate data changes relevant to analytics
5. WHEN the 14-day escalation deadline is reached for unconfirmed session completions, THE GuidedMentor_Platform SHALL publish an escalation event to EventBridge that triggers status update and notification delivery
6. THE GuidedMentor_Platform SHALL implement dead-letter queues (SQS) for all EventBridge rule targets to capture and retain failed event deliveries for investigation
7. THE GuidedMentor_Platform SHALL use EventBridge Scheduler for recurring jobs: lock expiration cleanup (every 5 minutes), analytics aggregation (hourly), and notification digest (daily at 9:00 AM AEST)

### Requirement 21: Security and Access Control (AppSec, OWASP)

**User Story:** As a platform operator, I want comprehensive security controls addressing OWASP Top 10 and data protection, so that user data is protected and the platform resists common attack vectors.

#### Acceptance Criteria

1. THE Authentication_Service SHALL issue JWT access tokens with 15-minute expiry and rotating refresh tokens with 7-day expiry, using Cognito-managed token signing
2. THE GuidedMentor_Platform SHALL implement resource-based access control on all API endpoints, ensuring users can only access their own data (userId in JWT must match resource owner)
3. THE GuidedMentor_Platform SHALL validate all API request inputs using FluentValidation on every endpoint, rejecting requests with invalid data before processing
4. THE GuidedMentor_Platform SHALL enforce Content Security Policy (CSP) headers, X-Content-Type-Options: nosniff, X-Frame-Options: DENY, and Strict-Transport-Security (HSTS max-age 31536000) on all responses
5. THE GuidedMentor_Platform SHALL enforce HTTPS (TLS 1.3) for all communications and reject HTTP connections
6. THE GuidedMentor_Platform SHALL implement CSRF protection using SameSite=Strict cookie attribute and Origin header validation
7. THE GuidedMentor_Platform SHALL store all secrets (API keys, database credentials, Bedrock model IDs) in AWS Secrets Manager and retrieve them at Lambda cold start
8. THE GuidedMentor_Platform SHALL enable encryption at rest for DynamoDB (AWS-managed KMS), S3 (AES-256), and Aurora PostgreSQL (KMS)
9. THE GuidedMentor_Platform SHALL implement audit logging for every state change, recording userId, timestamp, action, resource, and correlationId to a dedicated CloudWatch log group
10. THE GuidedMentor_Platform SHALL run OWASP ZAP automated security scans against API endpoints in CI and block deployments if high-severity vulnerabilities are detected
11. THE GuidedMentor_Platform SHALL run Dependabot and NuGet Audit in CI to detect known vulnerabilities in dependencies
12. THE GuidedMentor_Platform SHALL deploy AWS WAF (Web Application Firewall) in front of both API Gateway and CloudFront with: AWS Managed Rules for common threats (SQL injection, XSS, known bad inputs), rate-based rules (auto-block IPs exceeding 2000 requests per 5 minutes), bot control rules (block known bad bots), and geographic restrictions (allow only Australian IP ranges for initial launch with ability to expand)
13. THE GuidedMentor_Platform SHALL use AWS KMS Customer-Managed Keys (CMK) for encryption of all customer-sensitive data: DynamoDB tables (Users, Mentors, Mentees, Sessions), S3 resume storage, and Aurora PostgreSQL, with automatic annual key rotation enabled
14. THE GuidedMentor_Platform SHALL implement Zero-Trust IAM with: permission boundaries on all Lambda execution roles, no IAM users with console access (SSO via AWS Organizations only), explicit deny policies for cross-account access, and Lambda function code signing to prevent unauthorized code deployment
15. THE GuidedMentor_Platform SHALL align with the Australian Essential Eight maturity model by implementing: application control via Lambda code signing and container image scanning, automated patch management via Dependabot with weekly review cadence, restricted administrative privileges via permission boundaries and SCP guardrails, and multi-factor authentication enforced via Cognito for all users
16. THE GuidedMentor_Platform SHALL maintain architectural alignment with ISO/IEC 27001 (Information Security Management) by implementing: information classification (public, internal, confidential, restricted) applied to all data stores, access control registers documenting who can access what data, data retention policies (user data retained 3 years after last activity, deleted on request within 30 days), and incident response procedures documented in runbooks
17. THE GuidedMentor_Platform SHALL maintain architectural alignment with ISO/IEC 42001 (AI Management System) by implementing: AI risk assessment documentation for session plan generation and help assistant, model version tracking (record which Bedrock model version produced each session plan), human oversight capability (admin can review and flag AI-generated session plans), and AI decision audit trail logging every Bedrock invocation with input hash, output hash, model version, and latency

### Requirement 22: Observability and Monitoring

**User Story:** As a platform operator, I want comprehensive observability with structured logging, distributed tracing, and alerting, so that I can detect, diagnose, and resolve issues quickly.

#### Acceptance Criteria

1. THE Observability_Stack SHALL implement structured logging using Serilog with JSON output to CloudWatch Logs, including correlationId, userId, requestPath, duration, and log level on every log entry
2. THE Observability_Stack SHALL implement distributed tracing using OpenTelemetry SDK with X-Ray exporter, propagating trace context across all Lambda invocations, API Gateway, and AppSync calls
3. THE Observability_Stack SHALL publish custom CloudWatch metrics for: API latency percentiles (p50, p95, p99), error rates per endpoint, Bedrock token usage (input and output tokens per invocation), matching algorithm computation time, and DynamoDB consumed capacity units
4. THE Observability_Stack SHALL configure CloudWatch alarms that trigger when: error rate exceeds 1% over 5 minutes, API latency p99 exceeds 5 seconds, Bedrock API failures exceed 3 in 10 minutes, or DynamoDB throttled requests exceed 0
5. THE Observability_Stack SHALL publish alarm notifications to an SNS topic for operational alerting
6. THE Observability_Stack SHALL implement health check endpoints (/health) on each microservice that return 200 when the service and its dependencies are operational
7. THE Observability_Stack SHALL tag all AWS resources with cost allocation tags (Environment, Service, BoundedContext) for per-service cost attribution
8. THE Observability_Stack SHALL configure AWS Budget alerts at 50%, 80%, and 100% of monthly budget threshold

### Requirement 23: CI/CD and Deployment (GitHub Actions + Terraform)

**User Story:** As a developer, I want automated CI/CD pipelines with infrastructure-as-code using Terraform, so that deployments are repeatable, auditable, and safe.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL define all AWS infrastructure using Terraform with modular structure (one module per bounded context), workspaces for environment separation (dev, staging, prod), and remote state stored in S3 with DynamoDB state locking
2. THE GuidedMentor_Platform SHALL implement GitHub Actions reusable workflow templates for: build and test (.NET + React), infrastructure plan and apply (Terraform), frontend deployment (S3 + CloudFront invalidation), and Lambda deployment (Native AOT publish + zip)
3. WHEN a pull request is opened, THE GuidedMentor_Platform SHALL run: .NET build and unit tests (xUnit), FsCheck property-based tests, React build and component tests, axe-core accessibility checks (block if score below 90), OWASP ZAP security scan, Terraform plan (for infrastructure changes), and code coverage report (block if below 80% for Lambda handlers or 95% for pure logic)
4. WHEN a pull request is merged to main, THE GuidedMentor_Platform SHALL automatically deploy to the staging environment and run Playwright E2E tests against staging
5. WHEN staging E2E tests pass, THE GuidedMentor_Platform SHALL require manual approval before promoting to production
6. THE GuidedMentor_Platform SHALL use AWS AppConfig feature flags for progressive rollouts of new features, enabling canary deployments (1% → 10% → 50% → 100%)
7. THE GuidedMentor_Platform SHALL implement rollback capability: Terraform state rollback for infrastructure, Lambda version aliases for instant function rollback, and S3 versioning for frontend rollback

### Requirement 24: HA/DR and Resilience

**User Story:** As a platform operator, I want high availability, disaster recovery, and resilience patterns, so that the platform remains operational during failures and recovers quickly from disasters.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL deploy all compute and data services across multiple Availability Zones: DynamoDB (automatic multi-AZ), Aurora PostgreSQL (multi-AZ with read replicas), and Lambda (automatic multi-AZ)
2. THE GuidedMentor_Platform SHALL enable DynamoDB point-in-time recovery (PITR) on all five tables with 35-day retention
3. THE GuidedMentor_Platform SHALL enable S3 versioning on Resume_Storage for file recovery and maintain lifecycle rules to transition non-current versions to Glacier after 30 days
4. THE Analytics_Database SHALL have automated backups with 35-day retention and support point-in-time restore
5. THE GuidedMentor_Platform SHALL implement Circuit_Breaker pattern (Polly v8) on all external service calls (Bedrock, DynamoDB, S3, AppSync) with configurable thresholds: open after 5 failures in 30 seconds, half-open after 60 seconds
6. THE GuidedMentor_Platform SHALL implement retry policies (Polly v8) with exponential backoff and jitter for all transient failures, with maximum 3 retries
7. THE GuidedMentor_Platform SHALL implement timeout policies: 30 seconds for Bedrock calls, 5 seconds for DynamoDB operations, 10 seconds for Aurora queries
8. THE GuidedMentor_Platform SHALL achieve RTO (Recovery Time Objective) of less than 4 hours and RPO (Recovery Point Objective) of less than 1 hour for full platform recovery
9. IF the Circuit_Breaker opens for the Bedrock service, THEN THE GuidedMentor_Platform SHALL gracefully degrade by queuing session plan requests for later processing and informing users that AI features are temporarily unavailable

### Requirement 25: Guided Help Flow and Accessibility

**User Story:** As a first-time user, I want a guided onboarding tour and contextual help throughout the platform, so that I can learn how to use all features effectively regardless of my abilities.

#### Acceptance Criteria

1. WHEN a user logs in for the first time (or activates a new role for the first time), THE Onboarding_Tour SHALL display a step-by-step overlay walkthrough highlighting key UI elements with descriptive tooltips
2. THE Onboarding_Tour SHALL be dismissible at any step, with the option to restart from the settings page
3. THE Onboarding_Tour SHALL be fully keyboard-navigable (Tab to advance, Escape to dismiss) and announce each step via aria-live region for screen readers
4. THE GuidedMentor_Platform SHALL display contextual tooltips (triggered by hover or focus) on all form fields explaining expected input format and constraints
5. THE GuidedMentor_Platform SHALL display empty state guidance with clear instructions and prominent call-to-action buttons when any page or section has no content
6. THE GuidedMentor_Platform SHALL display progress indicators on all multi-step flows (onboarding, session completion) showing current step and total steps
7. THE GuidedMentor_Platform SHALL implement inline real-time validation on all form fields, displaying validation state (valid/invalid) as the user types with a 300ms debounce
8. THE GuidedMentor_Platform SHALL display confirmation dialogs for all destructive actions (decline mentee, cancel session, delete profile) with clear consequence description and a cancel option
9. THE GuidedMentor_Platform SHALL display friendly, non-technical error messages for all failure states with a retry button and optional "Learn more" link
10. THE GuidedMentor_Platform SHALL implement a retry mechanism with visual feedback (spinner) on all failure states across the application
11. THE GuidedMentor_Platform SHALL respect the prefers-reduced-motion media query by disabling all animations and transitions when the user has configured reduced motion in their operating system
12. THE GuidedMentor_Platform SHALL implement landmark roles (main, nav, aside, footer) on all pages for screen reader navigation

### Requirement 26: Database Resilience and Performance

**User Story:** As a platform operator, I want optimized database performance with connection management and hot-key mitigation, so that the platform handles traffic spikes without degradation.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL deploy Amazon RDS Proxy between all Lambda functions in the Engagement Context (dashboards, notifications) and Aurora PostgreSQL to manage connection pooling, limiting concurrent connections to the Aurora writer instance to 80% of max_connections
2. THE GuidedMentor_Platform SHALL implement DynamoDB partition key strategies to avoid hot keys: the Notifications_Table SHALL use a composite key pattern (recipientUserId#YYYY-MM) as partition key to distribute writes across time-based partitions for high-volume notification recipients
3. THE GuidedMentor_Platform SHALL configure DynamoDB auto-scaling alarms that trigger at 70% consumed capacity to proactively alert operators before throttling occurs
4. THE Analytics_Database SHALL be configured with Aurora Serverless v2 scaling from 0.5 to 8 ACUs (Aurora Capacity Units) with a scale-down cooldown of 5 minutes to optimize cost during low-traffic periods

### Requirement 27: Architecture Testing and SOLID Enforcement

**User Story:** As a developer, I want automated architecture tests that enforce Clean Architecture layer dependencies and SOLID principles, so that code quality is maintained as the team grows.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL implement NetArchTest architecture tests in CI that verify: the Domain layer has no dependency on Infrastructure, Application, or Presentation assemblies; the Application layer has no dependency on Infrastructure or Presentation assemblies; no assembly references AWS SDK packages except the Infrastructure layer
2. THE GuidedMentor_Platform SHALL implement architecture tests verifying SOLID principles: all public classes in the Domain layer implement at most one interface (ISP), all repository interfaces are defined in the Domain layer (DIP), all command/query handlers have a single Handle method (SRP)
3. THE GuidedMentor_Platform SHALL use Bogus library alongside FsCheck for generating realistic test data (Australian names, valid email formats, realistic chapter assignments) for integration and E2E tests
4. THE GuidedMentor_Platform SHALL use Vitest with React Testing Library for component-level testing of all shared UI components and page-level integration tests for each micro-frontend
5. THE GuidedMentor_Platform SHALL enforce that all architecture tests pass in CI before pull request merge, with zero tolerance for architecture violations

### Requirement 28: Mentor Opportunities Board (Jobs, Workshops, Events, Training)

**User Story:** As a mentor, I want to post job openings, workshops, events, or training opportunities (from my company or any other source) on the platform, so that mentees can discover relevant career and learning opportunities through our community.

#### Acceptance Criteria

1. WHEN a mentor navigates to the Opportunities Board section in their dashboard, THE GuidedMentor_Platform SHALL display an option to create a new opportunity posting with the following fields: title (5-100 characters), opportunity type (job, workshop, event, training), company or organisation name (free text 2-100 characters, not restricted to mentor's own company), description (100-2000 characters), location (city name or "Remote" or "Online"), date and time (for workshops/events/training — optional for jobs), employment type (full-time, part-time, contract, internship — required only for job type), required AWS skills (multi-select from predefined list, 0-10), experience level (beginner, intermediate, advanced, any), and external URL (valid HTTPS link for application or registration)
2. THE GuidedMentor_Platform SHALL limit each mentor to a maximum of 5 active opportunity postings at any time (across all types combined)
3. WHEN a mentor publishes an opportunity posting, THE GuidedMentor_Platform SHALL set the expiry date to 30 days from publication (or the event date for workshops/events/training, whichever comes first) and display the remaining days on the card
4. IF an opportunity posting reaches its expiry, THEN THE GuidedMentor_Platform SHALL automatically archive the posting, remove it from public visibility, and notify the mentor with an option to renew for another 30 days (jobs only — workshops/events/training auto-archive after their event date)
5. WHEN a mentee views the Browse_Page, THE GuidedMentor_Platform SHALL display a "Sharing Opportunities" badge on mentor cards where the mentor has one or more active postings of any type
6. THE GuidedMentor_Platform SHALL provide a dedicated "Opportunities" page accessible from the navigation bar where all active postings are listed, sorted by most recently posted, with filters for: opportunity type (job, workshop, event, training), location (city/remote/online), employment type (for jobs), required skills, and experience level
7. WHEN a mentee clicks on an opportunity posting, THE GuidedMentor_Platform SHALL display the full details and a contextual action button ("Apply" for jobs, "Register" for workshops/events/training) that opens the external URL in a new tab, and record the click as an engagement event for analytics
8. THE GuidedMentor_Platform SHALL display opportunity postings from matched mentors (mentors with active or completed sessions with the mentee) with a "From your mentor" highlight badge on the mentee dashboard
9. WHEN a mentor edits or deletes an opportunity posting, THE GuidedMentor_Platform SHALL update the posting within 2 seconds and reflect changes across all views immediately
10. IF a mentor attempts to post a 6th opportunity while already having 5 active postings, THEN THE GuidedMentor_Platform SHALL reject the request and display a message indicating the maximum active posting limit has been reached with a prompt to archive an existing posting
11. WHEN a mentor publishes a new opportunity posting, THE GuidedMentor_Platform SHALL send a notification via the Notification_Service to all mentees who are currently matched with that mentor (active sessions) informing them of the new opportunity with a deep link to the posting
12. WHEN a mentee completes onboarding and indicates interest in receiving opportunity notifications (opt-in checkbox on Step 3: Goals), THE GuidedMentor_Platform SHALL send notifications for new opportunity postings from any mentor that match at least 2 of the mentee's listed skills, regardless of whether the mentee is matched with that mentor
13. THE GuidedMentor_Platform SHALL allow mentees to configure their opportunity notification preferences in Settings: receive notifications for all types (jobs, workshops, events, training) or specific types only, and toggle the skill-match notifications on or off

### Requirement 29: Meetup-Aligned Session Scheduling

**User Story:** As a mentor, I want to schedule mentoring sessions aligned with upcoming AWS meetup events, so that I can meet my mentee in person at the meetup without additional time commitment.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL maintain a calendar of upcoming AWS meetup events for all Australian chapters with the following information per event: event title, chapter, date, start time, end time, venue name, venue address, and event URL
2. WHEN a session plan is generated and the mentor is configuring the session, THE GuidedMentor_Platform SHALL present two scheduling options: "Schedule at next meetup" (shows upcoming meetup events for the mentor's chapter) and "Schedule independently" (shows the mentor's regular availability slots)
3. IF the mentor selects "Schedule at next meetup" and chooses a specific meetup event, THEN THE GuidedMentor_Platform SHALL associate the session with that meetup event and display the meetup date, time, and venue on the Session_Plan_Page for both mentor and mentee
4. WHEN a session is aligned with a meetup event, THE Session_Plan_Generator SHALL adapt the generated agenda to include context-appropriate items such as: "15-minute pre-meetup coffee chat" or "30-minute post-meetup debrief", adjusting the standard 35-minute agenda to fit around the meetup timing
5. THE GuidedMentor_Platform SHALL send reminder notifications to both mentor and mentee 24 hours before a meetup-aligned session with the meetup venue details and session agenda summary
6. WHEN a mentee views the Browse_Page, THE GuidedMentor_Platform SHALL display an "Attending [meetup name]" badge on mentor cards where the mentor has indicated attendance at an upcoming meetup, and allow mentees to filter mentors by upcoming meetup attendance
7. THE GuidedMentor_Platform SHALL allow chapter leads (mentors with a "chapter_lead" flag) to create and manage meetup events for their chapter, including: creating new events, editing event details, and cancelling events (with notification to all users with sessions aligned to that event)
8. IF a meetup event is cancelled by a chapter lead, THEN THE GuidedMentor_Platform SHALL notify all mentor-mentee pairs with sessions aligned to that event and prompt the mentor to reschedule (either to another meetup or to an independent time slot)
9. THE GuidedMentor_Platform SHALL display an "Upcoming Meetups" section on both the Mentee_Dashboard and Mentor_Dashboard showing the next 3 meetup events for the user's chapter with event details and a count of confirmed mentor attendees

### Requirement 30: User Activity Tracking and Engagement Analytics

**User Story:** As a platform operator, I want to track user activity and engagement patterns across the platform, so that I can identify usability issues, improve accessibility, and enhance the customer experience based on real behaviour data.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL implement client-side event tracking that captures the following user activities without collecting personally identifiable information: page views (page name, timestamp, duration), feature interactions (button clicks, form submissions, filter selections), navigation patterns (pages visited in sequence, exit points), accessibility feature usage (keyboard navigation events, screen reader detection, reduced motion preference), error encounters (error type, page, recovery action taken), and session metadata (session duration, browser, viewport width, timezone)
2. THE GuidedMentor_Platform SHALL persist all tracked events to the Engagement_Analytics table in DynamoDB with: userId (hashed for privacy), eventType, eventData (JSON), timestamp, sessionId (browser session), and pageContext
3. THE GuidedMentor_Platform SHALL batch client-side events and flush them to the backend every 30 seconds or when the user navigates away (using navigator.sendBeacon for reliability), minimizing API calls
4. THE GuidedMentor_Platform SHALL track the following accessibility-specific metrics to identify barriers: percentage of users with keyboard-only navigation, percentage using screen readers (detected via ARIA interaction patterns), percentage with reduced-motion preference enabled, form abandonment rates per step (indicating confusing UX), error frequency by page (indicating unclear instructions), and average time-to-complete for onboarding wizards per step
5. THE GuidedMentor_Platform SHALL track engagement metrics including: mentor browse-to-lock conversion rate, session plan view-to-checklist-completion rate, AI help assistant usage frequency and common questions, notification click-through rate by notification type, job posting view-to-apply click rate, and role toggle frequency
6. THE GuidedMentor_Platform SHALL provide an operator analytics dashboard (accessible only to platform administrators) displaying: daily/weekly/monthly active users, feature usage heatmap (most/least used features), user flow funnels (signup → onboard → browse → match → session → complete), accessibility compliance metrics (keyboard users, screen reader users), error hotspots (pages with highest error rates), and retention metrics (7-day, 30-day return rates)
7. THE GuidedMentor_Platform SHALL implement a consent banner on first visit informing users that anonymous usage data is collected to improve the platform experience, with an option to opt out of non-essential tracking while maintaining core functionality
8. IF a user opts out of tracking, THEN THE GuidedMentor_Platform SHALL disable all non-essential event collection and only retain events required for core functionality (authentication events, error logging for debugging)
9. THE GuidedMentor_Platform SHALL replicate tracked events from DynamoDB to the Analytics_Database (Aurora PostgreSQL, staging/prod only) via DynamoDB Streams for complex analytical queries, funnel analysis, and cohort comparisons
10. THE GuidedMentor_Platform SHALL implement error tracking with: automatic capture of unhandled JavaScript exceptions (stack trace, component tree, user action that triggered it), API error responses with correlation IDs, client-side performance metrics (LCP, FID, CLS — Core Web Vitals), and structured error log export to CloudWatch for debugging
11. THE GuidedMentor_Platform SHALL tag all tracked events with the user's active role (mentor/mentee) to enable role-specific analytics and identify UX differences between the two user types

### Requirement 31: Super Admin and Platform Management

**User Story:** As a super admin, I want to manage all users, monitor platform health, and enable/disable the platform during critical outages, so that I can maintain platform operations and handle emergencies.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL support a Super_Admin role accessible only via a dedicated admin login (separate from the standard mentor/mentee Cognito User Pool group) with multi-factor authentication enforced
2. WHEN a Super_Admin logs in, THE GuidedMentor_Platform SHALL display an admin dashboard showing: total user count (by role), active sessions count, platform health status (based on CloudWatch alarm states), and recent audit log entries
3. THE Super_Admin SHALL be able to view, search, and filter all user accounts by: name, email, role, chapter, onboarding status, and account status (active, locked, disabled)
4. THE Super_Admin SHALL be able to disable or re-enable any user account, with a required reason field that is recorded in the audit log
5. THE Super_Admin SHALL be able to place the entire platform in maintenance mode, which displays a "Platform temporarily unavailable" page to all non-admin users while allowing Super_Admin access to continue
6. THE Super_Admin SHALL be able to enable or disable individual platform features via feature flags (AI Help Assistant, Job Board, Meetup Calendar, Session Plan Generation) without requiring a deployment
7. IF the platform is placed in maintenance mode, THEN THE GuidedMentor_Platform SHALL display a user-friendly maintenance page with an estimated return time and contact information, and reject all API requests from non-admin users with a 503 status code
8. THE Super_Admin SHALL be able to view the operator analytics dashboard (Requirement 30.6) with full access to engagement funnels, error hotspots, and user activity metrics
9. WHEN a Super_Admin performs any action (disable user, toggle feature, maintenance mode), THE GuidedMentor_Platform SHALL record the action in the audit log with: adminId, timestamp, action, target resource, and reason
10. THE GuidedMentor_Platform SHALL limit Super_Admin access to a maximum of 5 designated accounts, and any attempt to add a 6th SHALL be rejected until an existing admin is removed

### Requirement 32: Mentor Availability Toggle (Vacation/Pause Mode)

**User Story:** As a mentor, I want to temporarily mark myself as unavailable (vacation, personal commitment) so that I am hidden from browse results without losing my profile or active sessions.

#### Acceptance Criteria

1. WHEN a mentor navigates to their Settings page, THE GuidedMentor_Platform SHALL display an "Availability Status" toggle with two states: Available and Unavailable (with an optional reason field: vacation, personal commitment, workload, other)
2. WHEN a mentor sets their status to Unavailable, THE Matching_Algorithm SHALL immediately exclude that mentor from all Browse_Page results, regardless of their activeMenteeCount or maxMentees capacity
3. WHEN a mentor is set to Unavailable, THE GuidedMentor_Platform SHALL display an "On Break" badge on their mentor profile visible only to their existing matched mentees
4. THE GuidedMentor_Platform SHALL NOT affect active sessions when a mentor sets themselves to Unavailable — all existing mentee sessions, checklist progress, and completion flows SHALL continue to function normally
5. WHEN a mentor sets their status back to Available, THE Matching_Algorithm SHALL immediately include that mentor in Browse_Page results (subject to normal capacity checks)
6. THE GuidedMentor_Platform SHALL allow a mentor to set an optional "return date" when marking as Unavailable, which displays to their matched mentees as "Back on [date]"
7. IF a mentor has been Unavailable for more than 90 days without returning, THEN THE GuidedMentor_Platform SHALL send a reminder notification asking if they wish to remain on the platform or deactivate their mentor profile
8. THE Mentor_Dashboard SHALL display the current availability status prominently with a one-click toggle to switch between Available and Unavailable
9. WHEN a mentee views a locked or matched mentor who is currently Unavailable, THE GuidedMentor_Platform SHALL display the mentor's return date (if set) and a message indicating the mentor is temporarily unavailable for new sessions but existing sessions continue

### Requirement 33: Simulation Data for Demo and Testing

**User Story:** As a developer or demo presenter, I want realistic seed data pre-loaded into the platform, so that I can demonstrate all features without manually creating accounts and content.

#### Acceptance Criteria

1. THE GuidedMentor_Platform SHALL provide a data seeding script (`seed-data.ts` or equivalent) that populates the dev/demo environment with realistic simulation data covering all user journeys
2. THE Seed_Data_Generator SHALL create the following minimum dataset: 20 mentor profiles (distributed across at least 8 Australian chapters, varied expertise areas, certifications, and years of experience), 30 mentee profiles (varied skills, experience levels, goals, and chapters), 15 active sessions (with generated session plans at various checklist completion stages), 5 completed sessions, 3 pending sessions, and 2 unresolved sessions
3. THE Seed_Data_Generator SHALL create: 10 active job postings (from different mentors, varied locations and employment types), 3 expired job postings, and 2 archived job postings
4. THE Seed_Data_Generator SHALL create: 5 upcoming meetup events (across different chapters, with confirmed attendees), 2 past meetup events, and 3 sessions aligned to meetup events
5. THE Seed_Data_Generator SHALL create: 50 notification records (varied types: request_sent, accepted, declined, plan_ready, completion_marked, reminders) distributed across seed users
6. THE Seed_Data_Generator SHALL create: 1 Super_Admin account, 2 chapter lead accounts (with chapter_lead flag), and 1 user with both mentor and mentee onboarding completed (to demonstrate role toggle)
7. THE Seed_Data_Generator SHALL use Bogus library to generate realistic Australian names, email addresses, company names, and professional titles
8. THE Seed_Data_Generator SHALL be idempotent — running it multiple times SHALL not create duplicate data (use deterministic IDs or check-before-insert logic)
9. THE Seed_Data_Generator SHALL be executable via a single CLI command: `dotnet run --project tools/SeedData -- --environment dev`
10. THE Seed_Data_Generator SHALL include a corresponding frontend demo script that guides a presenter through key user journeys with pre-configured accounts and expected outcomes documented
