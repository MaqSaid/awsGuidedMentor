# GuidedMentor Platform — Demo Guide

> A step-by-step presenter walkthrough for demonstrating all platform features using pre-seeded accounts.

## Prerequisites

1. **Seed the demo environment** before starting:

   ```bash
   dotnet run --project tools/SeedData -- --environment dev
   ```

2. **Start the platform locally** (or ensure the dev environment is deployed):

   ```bash
   # Backend — start Lambda local emulator or deployed dev endpoint
   # Frontend — start all micro-frontends
   cd frontend && npm run dev
   ```

3. **Verify the platform is running** by navigating to `http://localhost:3000`. You should see the GuidedMentor login page with the dark glassmorphism design.

---

## Demo Credentials

| Role | Email | Password | Notes |
|------|-------|----------|-------|
| Super Admin | `admin@guidedmentor.dev` | `Admin@Guided2024!` | MFA enabled, full platform control |
| Chapter Lead (Sydney) | `lead.sydney@guidedmentor.dev` | `ChapterLead@2024!` | Can create/manage Sydney meetups |
| Chapter Lead (Melbourne) | `lead.melbourne@guidedmentor.dev` | `ChapterLead@2024!` | Can create/manage Melbourne meetups |
| Mentor (demo) | `mentor.demo@guidedmentor.dev` | `MentorDemo@2024!` | First seeded mentor, Sydney chapter |
| Mentee (demo) | `mentee.demo@guidedmentor.dev` | `MenteeDemo@2024!` | First seeded mentee, Sydney chapter |
| Dual-Role User | `dual.role@guidedmentor.dev` | `DualRole@2024!` | Both mentor + mentee onboarding complete |

> **Note:** All demo passwords meet the 12+ character requirement with uppercase, lowercase, digit, and special character.

---

## Demo Flow Overview

```
Auth → Onboard → Browse → Lock → Session Plan → Complete →
Role Toggle → Opportunities Board → Meetup Calendar → Admin Dashboard
```

Estimated total demo time: **25–35 minutes**

---

## Journey 1: Authentication & Account Security

**Account:** Use a fresh browser / incognito window

### Step 1.1 — Google OAuth Sign-In

1. Navigate to `http://localhost:3000`
2. Click the **"Sign in with Google"** button
3. Complete the Google OAuth flow (dev mode redirects immediately)

**Expected outcome:** User is redirected to the **Role Selection** screen. Navigation is blocked until a role is chosen.

### Step 1.2 — Email/Password Sign-Up

1. Click **"Create Account"** on the login page
2. Enter email: `newuser@example.com`
3. Enter password: `DemoPassword@1`
4. Observe **inline validation** (300ms debounce):
   - ✅ 12+ characters
   - ✅ Uppercase letter
   - ✅ Lowercase letter
   - ✅ Digit
   - ✅ Special character
5. Submit the form

**Expected outcome:** A verification code is sent (10-minute expiry). The UI shows a code entry field with a countdown.

### Step 1.3 — Account Lockout (optional demo)

1. Attempt to sign in with incorrect password **5 times within 15 minutes**
2. On the 5th failure, observe the lockout message

**Expected outcome:** Account is locked for 30 minutes. The user sees a friendly message: *"Account temporarily locked. Please try again later or reset your password."* An email notification is sent to the account holder.

### Troubleshooting

- **Google OAuth not redirecting:** Ensure Cognito User Pool has the Google IdP configured with valid client credentials for the dev environment.
- **Verification code not received:** Check CloudWatch logs for the Cognito trigger Lambda. In local dev, codes may be logged to console.

---

## Journey 2: Onboarding Wizard

**Account:** Log in as `mentee.demo@guidedmentor.dev` (or use a freshly created account)

> If using an existing seeded account, onboarding is already complete. To re-demo, use the fresh sign-up from Journey 1.

### Step 2.1 — Role Selection

1. After first login, the **Role Selection** screen appears
2. Select **"I'm looking for a mentor"** (Mentee role)

**Expected outcome:** User is routed to the 4-step mentee onboarding wizard. A progress indicator shows Step 1 of 4.

### Step 2.2 — Mentee Onboarding (4 Steps)

**Step 1: Profile**
- Enter display name, select AWS chapter (e.g., Sydney), enter city
- Profile photo is optional (skip for speed)

**Step 2: Skills**
- Add 1–10 skills from the AWS skills list (e.g., "Lambda", "DynamoDB", "S3")
- Set experience level and years for each

**Step 3: Goals**
- Select primary goal (e.g., "Career Transition")
- Enter goal description (50–500 characters)
- Select preferred mentoring duration

**Step 4: Preferences**
- Set availability days and time slots
- Choose communication preference (video/chat/both)
- Optionally upload a resume (PDF/DOCX, ≤5MB)

3. Click **"Complete Onboarding"**

**Expected outcome:**
- Profile is saved to the Mentees table
- Onboarding status changes to `completed`
- User is redirected to the **Mentee Dashboard**
- The **Onboarding Tour** overlay starts automatically (can be dismissed with Escape)

### Step 2.3 — Progress Persistence (optional)

1. Navigate away mid-wizard (close tab or click browser back)
2. Return and log in again

**Expected outcome:** The wizard resumes from the last completed step. Previously entered data is preserved.

### Troubleshooting

- **Validation errors not showing:** Check that the FluentValidation pipeline is registered. Inline errors appear below each field after 300ms debounce.
- **Resume upload fails:** Verify S3 presigned URL generation is working. Check CORS headers on the S3 bucket.

---

## Journey 3: Browse Mentors & Matching

**Account:** Log in as `mentee.demo@guidedmentor.dev`

### Step 3.1 — View Browse Page

1. Navigate to **"Browse Mentors"** from the dashboard or NavBar
2. Observe the paginated grid of mentor cards (12 per page)

**Expected outcome:**
- Each card shows: mentor name, title, chapter, expertise tags, and a **compatibility score badge**
- Scores are colour-coded: 🟢 green (>80%), 🟠 orange (50–79%), 🔴 red (<50%)
- Cards are sorted by score (descending), then alphabetically for ties
- Mentors with `availabilityStatus=unavailable` are **excluded** from results
- Mentors at full capacity (`activeMenteeCount ≥ maxMentees`) are excluded

### Step 3.2 — Filter Mentors

1. Open the **Filter Panel** (left sidebar or top bar)
2. Filter by chapter: select "Sydney"
3. Filter by skill: select "Lambda"

**Expected outcome:** Results update to show only Sydney-chapter mentors with Lambda expertise. Scores recalculate in context.

### Step 3.3 — View Mentor Detail

1. Click on a mentor card
2. Review their full profile: bio, expertise areas, certifications, availability slots, active mentee count vs. max

**Expected outcome:** Full profile renders with all seeded data. If the mentor has active opportunity postings, a **"Sharing Opportunities"** badge is visible.

### Troubleshooting

- **All scores showing 0%:** Ensure the mentee has completed onboarding with skills and goals set. The matching algorithm requires mentee profile data.
- **No mentors appearing:** Verify seed data was run. Check that at least one mentor has `availabilityStatus=available` and is below max capacity.

---

## Journey 4: Locking & Session Request

**Account:** Continue as `mentee.demo@guidedmentor.dev`

### Step 4.1 — Lock a Mentor

1. From the Browse page, click **"Request Mentor"** on a compatible mentor card
2. The **Lock Confirmation Modal** appears with:
   - Mentor name and score
   - "You have 15 minutes to confirm" timer
   - Confirm / Cancel buttons

3. Click **"Confirm"**

**Expected outcome:**
- A 15-minute lock is acquired via DynamoDB conditional write
- The timer counts down visually (MM:SS)
- Other mentees attempting to lock the same mentor see "Currently unavailable"
- Only **one active lock per mentee** is allowed

### Step 4.2 — Confirm Selection

1. While the lock is active, click **"Confirm Selection"**

**Expected outcome:**
- A pending session record is created
- The mentor receives a notification (visible in their NotificationBell)
- The mentee is redirected to their dashboard showing the new pending session

### Step 4.3 — Lock Expiry (optional demo)

1. Acquire a lock but **do not confirm** within 15 minutes
2. Wait for the timer to expire (or manually trigger the EventBridge cleanup for demo speed)

**Expected outcome:** The lock is automatically released. The mentor becomes available again. The mentee sees a "Lock expired" toast notification.

### Troubleshooting

- **"Already have an active lock" error:** Each mentee can only hold one lock at a time. Release or confirm the existing lock first.
- **Lock not expiring:** Check that the EventBridge scheduler (5-minute interval) is running. In local dev, you may need to manually invoke the cleanup handler.

---

## Journey 5: AI Session Plan Generation

**Account:** Log in as `mentor.demo@guidedmentor.dev`

### Step 5.1 — Accept a Pending Request

1. Navigate to the **Mentor Dashboard**
2. In the "Pending Requests" section, find a request (oldest first, with compatibility score)
3. Click **"Accept"**

**Expected outcome:**
- Session status changes to `active`
- AI session plan generation is triggered automatically via EventBridge
- Both mentor and mentee receive a notification
- The Mentor Dashboard shows the new active mentee in the "Active Mentees" section

### Step 5.2 — View Generated Session Plan

1. Click on the active session card
2. Navigate to the **Session Plan** page

**Expected outcome:**
- The session plan displays:
  - **Session Title** (≤100 characters)
  - **Timed Agenda** (3–7 items, each ≥3 minutes, total = 35 minutes)
  - **Pre-work Tasks** (2–5 items, each ≤200 characters)
  - **Follow-up Tasks** (2–5 items, each ≤200 characters)
- Each agenda item shows a duration badge
- Streaming display via Vercel AI SDK `useObject()` shows content appearing in real-time during generation

### Step 5.3 — Interact with Checklists

1. Toggle a pre-work item as complete (click the checkbox)
2. Toggle two follow-up items as complete
3. Observe the **Progress Bar** updating

**Expected outcome:**
- Checkboxes toggle with optimistic UI (immediate visual update)
- Progress bar shows: `round((checked / total) × 100)%`
- If network fails, a retry button appears and items revert

### Troubleshooting

- **Plan shows "Generating..." indefinitely:** Check Bedrock API connectivity. Verify the Polly retry policy (3 retries: 2s, 4s, 8s). If all retries fail, the session enters `pending_plan` state with a graceful degradation message.
- **Plan content seems inappropriate:** Verify Bedrock Guardrails are configured (content filters for hate/insults/sexual/violence + PII redaction).

---

## Journey 6: Session Completion Flow

**Account:** Use both `mentee.demo@guidedmentor.dev` and `mentor.demo@guidedmentor.dev`

### Step 6.1 — Mentee Initiates Completion

1. Log in as the **mentee**
2. Navigate to an active session (one of the seeded sessions with high progress)
3. Click **"Mark as Complete"**
4. Confirm in the dialog

**Expected outcome:**
- Session status changes to `mentee_completed`
- The `menteeCompletedAt` timestamp is set (irrevocable)
- The mentor receives a notification: "Your mentee has marked the session as complete"
- The mentee sees: "Waiting for mentor confirmation"

### Step 6.2 — Mentor Confirms Completion

1. Log in as the **mentor**
2. Open the same session from the notification or dashboard
3. Click **"Confirm Completion"**

**Expected outcome:**
- Session status changes to `completed`
- Both parties receive a confirmation notification
- The session moves to the "Completed" section on both dashboards
- Mentor's `activeMenteeCount` decreases by 1 (freeing capacity)

### Step 6.3 — Demonstrate State Machine Enforcement

1. Attempt to have the **mentor** mark complete first (before the mentee)

**Expected outcome:** The action is rejected. The UI shows: *"The mentee must mark the session as complete first."* This enforces the completion flow state machine ordering.

### Troubleshooting

- **Completion button not visible:** Ensure the session is in `active` state. Pending or unresolved sessions have different actions available.
- **Mentor can't confirm:** The mentee must mark complete first. Check that `menteeCompletedAt` is set on the session record.

---

## Journey 7: Role Toggle

**Account:** Log in as `dual.role@guidedmentor.dev`

### Step 7.1 — Observe Current Role

1. Log in and observe the NavBar shows the **active role** (e.g., "Mentor")
2. The dashboard displays mentor-specific content (pending requests, active mentees, capacity)

**Expected outcome:** The NavBar shows a **Role Toggle** button. The current role is highlighted.

### Step 7.2 — Toggle to Opposite Role

1. Click the **Role Toggle** button in the NavBar (one-click action)

**Expected outcome:**
- Active role switches instantly (Mentor → Mentee or vice versa)
- The dashboard reloads with the opposite role's content
- NavBar links adapt to the new role (e.g., "Browse Mentors" appears for mentee role)
- The inactive role's profile data is **preserved** (not modified or cleared)

### Step 7.3 — Toggle Back

1. Click Role Toggle again

**Expected outcome:** Returns to original role. Both profiles remain intact. This demonstrates Property 2 (toggle twice = original) and Property 4 (toggle preserves inactive profile).

### Troubleshooting

- **Role Toggle button missing:** Ensure the user has both onboarding wizards completed. The toggle only appears when both `mentorOnboardingStatus` and `menteeOnboardingStatus` are `completed`.
- **Dashboard shows empty state after toggle:** The dual-role user should have seeded data for both roles. Verify the seed data includes sessions/activity for both profiles.

---

## Journey 8: Opportunities Board

**Account:** Log in as `mentor.demo@guidedmentor.dev`

### Step 8.1 — Create an Opportunity Posting

1. Navigate to **"Opportunities"** from the NavBar
2. Click **"Post Opportunity"**
3. Fill in the form:
   - Type: "Job" (or "Workshop" / "Event" / "Training")
   - Title: "Senior Cloud Engineer"
   - Organisation: "Acme Cloud Services"
   - Description: brief description of the role
   - Location: "Sydney, Remote OK"
   - Employment Type: "Full-time" (for jobs)
   - Required Skills: select from skill list
   - Required Experience: "Mid-level"
4. Submit

**Expected outcome:**
- Posting is created with 30-day expiry (or event date if type is event)
- The mentor's Opportunities list shows the new posting
- Matched mentees (≥2 skill overlap) receive a notification
- The posting appears on the public Opportunities browse page
- Mentor's profile now shows "Sharing Opportunities" badge on Browse page

### Step 8.2 — Browse Opportunities (as Mentee)

1. Log in as `mentee.demo@guidedmentor.dev`
2. Navigate to **"Opportunities"**
3. Use filters: type, location, skills, experience level
4. Click on a posting to view details

**Expected outcome:**
- Seeded postings appear (10 active from various mentors)
- Filters narrow results correctly
- Expired/archived postings are excluded
- Postings from the mentee's matched mentor show a **"From your mentor"** badge
- Action button is contextual: "Apply" for jobs, "Register" for events/workshops/training

### Step 8.3 — Posting Limit Enforcement (optional)

1. As a mentor, attempt to create a 6th active posting (all types combined count toward the limit)

**Expected outcome:** The request is rejected with: *"Maximum 5 active postings allowed. Archive an existing posting to create a new one."*

### Troubleshooting

- **"Post Opportunity" button not visible:** Only mentors can post. Ensure you're logged in with a mentor role.
- **Notifications not reaching mentees:** Check that matched mentees have at least 2 overlapping skills and haven't opted out of opportunity notifications in their preferences.

---

## Journey 9: Meetup Calendar & Session Alignment

**Account:** Log in as `lead.sydney@guidedmentor.dev`

### Step 9.1 — View Upcoming Meetups

1. Navigate to **"Meetups"** from the NavBar
2. Observe the calendar view showing upcoming events

**Expected outcome:** Up to 3 upcoming meetups for the user's chapter are displayed, sorted by date (ascending). Cancelled and past events are excluded.

### Step 9.2 — Create a Meetup Event (Chapter Lead)

1. Click **"Create Meetup"**
2. Fill in:
   - Title: "AWS Sydney — Serverless Deep Dive"
   - Date: select a future date
   - Start/End time: 6:00 PM – 8:00 PM
   - Venue: "AWS Sydney Office, 26 Pitt St"
   - Event URL: link to meetup.com event page
3. Submit

**Expected outcome:**
- Meetup is created and visible in the calendar
- Only chapter leads can access the "Create Meetup" action (non-leads don't see the button)

### Step 9.3 — Align a Session to a Meetup

1. Log in as `mentor.demo@guidedmentor.dev`
2. Open an active session
3. Click **"Align to Meetup"**
4. Select the upcoming Sydney meetup from the list

**Expected outcome:**
- The session references the meetup's date, time, and venue
- A **"Attending [meetup name]"** badge appears on the session card
- Both mentor and mentee receive a 24-hour reminder notification before the event

### Step 9.4 — Cancel a Meetup (Chapter Lead, optional)

1. Log in as `lead.sydney@guidedmentor.dev`
2. Cancel an existing meetup

**Expected outcome:**
- Meetup status changes to `cancelled`
- All sessions aligned to this meetup are identified
- Affected mentor-mentee pairs receive a notification about the cancellation

### Troubleshooting

- **"Create Meetup" not visible:** Only chapter leads have this permission. Verify the account has the `chapter_lead` flag set.
- **Meetup not appearing in alignment options:** Ensure the meetup is in the same chapter as the session participants and is in the future.

---

## Journey 10: Super Admin Dashboard

**Account:** Log in as `admin@guidedmentor.dev`

> **Note:** MFA is required. In dev mode, the TOTP code may be auto-generated or logged to console.

### Step 10.1 — View Platform Overview

1. After MFA verification, the **Admin Dashboard** loads automatically
2. Observe:
   - Total user counts by role (mentors, mentees, dual-role)
   - Active sessions count
   - Platform health status (CloudWatch alarm states: OK / ALARM)
   - Recent audit log entries

**Expected outcome:** Dashboard shows aggregated data from all seeded entities. Health indicators reflect current CloudWatch alarm states.

### Step 10.2 — User Management

1. Navigate to the **User Search** panel
2. Search for a user by email (e.g., `mentor.demo@guidedmentor.dev`)
3. Apply filters: role = "Mentor", chapter = "Sydney"
4. Click on the user to view their profile

**Expected outcome:** Search results show matching users with pagination. Each result displays name, email, role, chapter, onboarding status, and account status.

### Step 10.3 — Disable a User Account

1. Select a user from search results
2. Click **"Disable Account"**
3. In the confirmation modal:
   - Enter a reason (required field): "Demo: testing account disable flow"
   - Confirm the action

**Expected outcome:**
- User account is disabled (cannot log in)
- An audit log entry is created with the admin's ID, action, target user, and reason
- The action can be reversed via "Enable Account"

### Step 10.4 — Maintenance Mode

1. Navigate to the **Platform Settings** section
2. Toggle **"Maintenance Mode"** ON
3. Enter an estimated return time (optional)
4. Confirm

**Expected outcome:**
- All non-admin requests return HTTP 503 (Service Unavailable)
- Admin users can still access the platform normally
- The maintenance mode flag is stored in AppConfig
- An audit entry records who enabled it and when

5. Toggle maintenance mode **OFF** to restore normal operation

### Step 10.5 — Feature Flags

1. Navigate to the **Feature Flags** panel
2. Toggle one of:
   - AI Help Assistant
   - Opportunities Board
   - Meetup Calendar
   - Session Plans

**Expected outcome:** The feature is immediately disabled/enabled platform-wide. Users see the feature appear or disappear on their next page load. An audit entry is recorded.

### Step 10.6 — Audit Log

1. Navigate to the **Audit Log** viewer
2. Filter by date range or action type (e.g., "disable_user", "maintenance_mode")

**Expected outcome:** Paginated audit entries showing: timestamp, admin email, action, target resource, and reason. All previous admin actions from this demo are visible.

### Troubleshooting

- **MFA code rejected:** Ensure the TOTP secret is correctly configured for the admin account. In local dev, check console output for the current code.
- **Dashboard showing zero counts:** Verify seed data was run successfully. Check for seed marker in the SeedMarkers table.
- **Maintenance mode not blocking requests:** Ensure the maintenance mode middleware is registered in the request pipeline before other handlers.

---

## Journey 11: Notifications & AI Help

**Account:** Log in as `mentee.demo@guidedmentor.dev`

### Step 11.1 — Real-Time Notifications

1. Observe the **Notification Bell** in the NavBar showing an unread count badge
2. Click the bell to open the notification panel
3. Observe notifications listed in reverse chronological order (max 50)
4. Click a notification to navigate to the relevant page

**Expected outcome:**
- Badge shows exact count (1–99) or "99+" for overflow
- Badge is hidden when unread count = 0
- Notifications include various types: request_sent, accepted, plan_ready, completion_marked, reminders
- Clicking "Mark all as read" clears the badge
- Real-time delivery: new notifications appear within 5 seconds (via AppSync subscription)

### Step 11.2 — AI Help Assistant

1. Click the **floating help bubble** (bottom-right corner) or press **Ctrl+H**
2. Ask a platform question: *"How do I browse mentors?"*
3. Observe the streamed response

**Expected outcome:**
- The chat interface opens with ARIA labels for accessibility
- The response streams in real-time via `useChat()` from Vercel AI SDK
- Responses are constrained to platform-related topics
- Ask an off-topic question (e.g., "What's the weather?") → polite redirect: *"I can only help with GuidedMentor platform questions."*
- Rate limited to 20 messages/minute

### Troubleshooting

- **Notifications not appearing in real-time:** Check AppSync WebSocket connection. Look for reconnection attempts in browser DevTools console.
- **AI Help not responding:** Verify Bedrock API connectivity and that the Guardrails are not overly restrictive for the dev environment.

---

## Journey 12: Accessibility & UX Features

**Account:** Any seeded account

### Step 12.1 — Skip Navigation

1. Refresh the page
2. Press **Tab** once

**Expected outcome:** A "Skip to main content" link becomes visible and focused. Pressing Enter skips past the NavBar to the main content area.

### Step 12.2 — Keyboard Navigation

1. Navigate through the Browse Mentors page using only the keyboard
2. Use **Tab** to move between mentor cards
3. Use **Enter** to select/activate a card

**Expected outcome:** All interactive elements are reachable via keyboard. Focus indicators are clearly visible. Modal focus is trapped (Tab cycles within the modal, Escape closes it).

### Step 12.3 — Reduced Motion

1. Enable "Reduce motion" in OS accessibility settings
2. Navigate through the platform

**Expected outcome:** Animations and transitions are disabled or minimized. The `prefers-reduced-motion` media query is respected throughout.

### Step 12.4 — Screen Reader Announcements

1. Enable a screen reader (e.g., NVDA, VoiceOver)
2. Perform an action that triggers dynamic content (e.g., submit a form with errors, toggle a notification)

**Expected outcome:** `aria-live` regions announce errors, loading states, and notifications. Form errors are announced immediately. The onboarding tour steps are announced as they advance.

### Troubleshooting

- **Focus not trapped in modals:** Ensure the FocusTrap utility is wrapping modal content. Check that the `tabindex` attributes are set correctly.
- **Skip link not visible on focus:** Verify the CSS positions the skip link off-screen until `:focus`, then brings it into view.

---

## Quick Reference: Seeded Data Summary

| Entity | Count | Key Details |
|--------|-------|-------------|
| Super Admin | 1 | `admin@guidedmentor.dev`, MFA enabled |
| Chapter Leads | 2 | Sydney + Melbourne |
| Mentors | 20 | 8+ chapters, varied expertise |
| Mentees | 30 | All chapters, varied goals |
| Active Sessions | 15 | Checklist progress 10%–90% |
| Completed Sessions | 5 | Both parties confirmed |
| Pending Sessions | 3 | Awaiting mentor acceptance |
| Unresolved Sessions | 2 | Escalated (14+ days no response) |
| Active Opportunity Postings | 10 | Jobs, workshops, events, training |
| Expired Postings | 3 | Past 30-day window |
| Archived Postings | 2 | Manually archived |
| Upcoming Meetups | 5 | Across 5 chapters |
| Past Meetups | 2 | Completed events |
| Meetup-Aligned Sessions | 3 | Sessions scheduled at meetup venues |
| Notifications | 50 | All types, mixed read/unread |
| Dual-Role User | 1 | Both onboardings complete |
| Unavailable Mentor | 1 | `availabilityStatus=unavailable`, return date set |

---

## Tips for Presenters

- **Speed up lock expiry:** In dev, reduce the lock TTL to 2 minutes in the environment config, or manually invoke the EventBridge cleanup Lambda.
- **Skip MFA for admin:** In local dev, configure Cognito to not enforce MFA, or use the console-logged TOTP code.
- **Show real-time updates:** Open two browser windows side-by-side (mentor + mentee) to demonstrate notification delivery and session state changes.
- **Demonstrate error recovery:** Temporarily disable a backend service to show the friendly error messages, retry buttons, and circuit breaker behavior.
- **Reset demo state:** Delete the seed marker record from the `GuidedMentor-SeedMarkers` table and re-run the seed command to start fresh.
- **Feature flag demo:** Disable "AI Help" via the admin panel, then show the help bubble disappearing from the mentee's view on page reload.
