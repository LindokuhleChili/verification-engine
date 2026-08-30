# Verification Engine — Design Brief for Google Stitch

## 1. Product context
A web app that lets South African shareholders claim unpaid dividends and dormant/lost shares
without the usual paper trail: certified ID copies, stamped bank letters, police-station affidavits.
It replaces that process with biometric verification, open banking, OCR, and e-signatures — but
the subject matter is inherently serious (identity documents, deceased estates, legal affidavits,
money), so the design has to feel *calm and trustworthy*, not flashy or "fintech-startup generic."

**Audience:** everyday shareholders (a wide age range, not just tech-savvy users), plus executors
and beneficiaries handling a bereavement. The design must read as credible to someone who has
never used a crypto app or a Notion-style SaaS tool.

**The page's job:** make a legally serious process feel safe, clear, and quick — never gamified,
never cold.

## 2. Design philosophy
Avoid the current AI-generated defaults: no warm cream-background-plus-terracotta-accent, no
near-black-plus-neon-accent, no purple/blue SaaS gradient, no heavy drop shadows or glassmorphism.

Instead, ground the palette and motifs in what this product actually is — stock certificates,
official seals, ledgers — reinterpreted with a light, modern, uncluttered hand. Think: the calm
confidence of a well-designed bank or land-registry portal, not a crypto dashboard.

## 3. Design tokens

### Color (light mode only)
| Token | Hex | Use |
|---|---|---|
| `bg-base` | `#F6F8F7` | Page background — cool near-white, not cream |
| `bg-surface` | `#FFFFFF` | Cards, panels, inputs |
| `bg-subtle` | `#EDF1EF` | Section dividers, disabled fields |
| `ink-primary` | `#1B2430` | Headings, primary text — deep ink-slate, not pure black |
| `ink-secondary` | `#5C6670` | Body copy, captions |
| `border-hairline` | `#DDE3E0` | Card borders, table rules |
| `accent-verified` | `#0F6E5C` | Primary accent — "verified," CTAs, links, active steps |
| `accent-verified-tint` | `#E3F0EC` | Accent background tint (badges, selected states) |
| `accent-brass` | `#A9852F` | Secondary accent, used sparingly — certificate/financial moments only (e.g. dividend amounts, seal icon) |
| `status-warning` | `#B8863A` | Action-needed states (muted amber, not neon) |
| `status-error` | `#B24B41` | Errors, rejected documents (muted brick, not alarm-red) |

Rule: `accent-brass` appears in at most one element per screen. It marks "this is an official/
financial artifact," not a general accent — overusing it flattens its meaning.

### Typography
- **Display** (headings, claim amounts, hero numbers): a refined serif with an engraved,
  certificate-like character — e.g. **Source Serif 4** or **Fraunces** (static, not the wonky
  variable-axis Fraunces). Used with restraint: page titles and key figures only.
- **Body/UI** (forms, buttons, nav, all interactive text): a clean humanist sans — e.g. **Inter**
  or **IBM Plex Sans**. This carries almost all on-screen text.
- **Utility/mono** (reference numbers, ID numbers, claim IDs, hashes): **IBM Plex Mono**. Signals
  "this is a precise, verifiable value" — distinct from conversational body text.

Type scale: establish a clear hierarchy (e.g. 40/28/20/16/14px) and keep weight contrast
deliberate — regular body, medium for labels, semibold (not bold) for headings.

### Layout concept
- Generous whitespace, 8px baseline grid, max content width ~720px for forms (never full-bleed
  forms — they read as unfinished on wide screens).
- Cards with 1px hairline borders (`border-hairline`) and *no* heavy drop shadow — at most a
  4–8px soft, low-opacity shadow for elevation. Corner radius: consistent 12px, not maximal
  pill-shaped everything.
- A persistent, quiet step indicator for multi-step flows — plain text labels ("Identity →
  Banking → Review"), not numbered circles with connecting lines (only use that pattern if a
  flow is a real fixed sequence, which the claim flows are, so it's earned here).

### Signature element
A fine hairline **engraved corner motif** — a small quarter-frame flourish reminiscent of the
guilloché border on a paper share certificate — appears only in one place: the top-left and
bottom-right corners of the "Verified" status card and the final claim-confirmation card. It's
never used decoratively elsewhere. This is the one deliberate flourish in an otherwise restrained
interface, and it ties directly back to the product's real subject (paper certificates being
converted into verified digital claims).

### Motion
Minimal and purposeful only: a soft 150–200ms fade/slide when moving between wizard steps, and a
gentle checkmark-draw animation the moment a verification step (biometric, banking, OCR) succeeds.
No ambient background animation, no parallax, no bouncing icons — this audience needs to trust the
screen, not be entertained by it.

## 4. Core screens
1. **Landing** — plain-language explanation of what the app does and why ("Claim what's yours,
   without the paperwork"), a single primary CTA, and a short trust section (security, POPIA
   compliance) — no stock photos of people shaking hands.
2. **Claim type selector** — three clear cards: "I'm the shareholder," "I'm inheriting shares,"
   "I lost my certificate." Each states in one sentence who it's for.
3. **Identity verification step** — camera capture UI for the biometric check, calm progress
   state, plain-language explanation of what happens to the photo.
4. **Banking link step** — open banking consent screen, clear about what's shared and why.
5. **Multi-party flow (deceased estate)** — a shared claim status view showing both Executor and
   Beneficiary's verification state side by side, plus a courier tracking card for the physical
   Letter of Executorship.
6. **Document upload / OCR review** — dropzone, then an editable extracted-fields review (from
   Textract) so the user can correct anything before submission.
7. **Claim dashboard** — list of claims with status badges (Pending / Verified / Action needed /
   Complete), using `accent-verified-tint` and `status-warning`/`status-error` backgrounds, never
   raw saturated color fills.
8. **Confirmation** — the one screen that uses the engraved corner motif in full, confirming the
   claim was submitted, with a plain-English summary of what happens next and by when.

## 5. Component notes
- **Buttons**: solid `accent-verified` for primary actions, outline/ghost for secondary — no
  gradient fills.
- **Status badges**: tinted background + matching text color, pill-shaped, small icon + label
  ("Verified," "Action needed") — never color alone to convey status (accessibility).
- **Upload dropzone**: hairline dashed border, clear icon, states for empty / uploading / uploaded
  / rejected (with a specific, non-vague reason for rejection).
- **Forms**: labels above fields (not floating placeholders — this audience needs persistent
  labels), inline validation, generous touch targets for older/less tech-fluent users.

## 6. Accessibility & quality floor
- WCAG AA contrast minimum throughout (verify `ink-secondary` on `bg-base` and `bg-subtle`
  specifically).
- Visible keyboard focus states on every interactive element.
- Respect `prefers-reduced-motion` — all transitions above become instant state changes.
- Fully responsive down to a small mobile viewport — this audience is likely to use a phone.
- Never rely on color alone for verification status; always pair with an icon and text label.
