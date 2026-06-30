---
inclusion: fileMatch
fileMatchPattern: "**/*.tsx"
---

# Design System Conventions

## Color Tokens (use via Tailwind — never hardcode hex)
- `bg-bg-primary` (#0a0a1a) — page background
- `bg-bg-secondary` (#0f0f2a) — secondary backgrounds
- `bg-bg-card` (rgba(255,255,255,0.04)) — glassmorphism cards
- `text-text-primary` (#f1f0ff) — headings, body
- `text-text-secondary` (#9ca3af) — labels, subtitles
- `text-violet` / `bg-violet` (#7c3aed) — primary brand, buttons
- `text-violet-light` (#a78bfa) — gradient text, hover
- `text-mint` / `bg-mint` (#10b981) — success, scores >80%
- `text-amber` (#f59e0b) — warnings, locked state
- `text-rose` (#f43f5e) — errors

## Typography
- Headings: `style={{ fontFamily: 'Outfit, sans-serif' }}`
- Body: Inter (set in CSS, default)

## Components
- Cards: `className="glass-card p-6"` (backdrop-blur + border)
- Scores: `<ScoreRing score={n} size="sm|md|lg" />`
- Buttons: `className="btn-violet"` or `"btn-mint"` or `"btn-ghost"`
- Badges: `className="text-xs px-2 py-0.5 rounded-full bg-{color}/20 text-{color}"`
- Glow effects: `className="glow-violet"` or `"glow-mint"`
