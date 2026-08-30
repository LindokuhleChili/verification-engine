---
name: Verification Engine
colors:
  surface: '#f7faf7'
  surface-dim: '#d7dbd8'
  surface-bright: '#f7faf7'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f1f4f1'
  surface-container: '#ebefec'
  surface-container-high: '#e5e9e6'
  surface-container-highest: '#e0e3e0'
  on-surface: '#181c1b'
  on-surface-variant: '#3f4945'
  inverse-surface: '#2d3130'
  inverse-on-surface: '#eef2ee'
  outline: '#6f7975'
  outline-variant: '#bec9c4'
  surface-tint: '#076b59'
  primary: '#005445'
  on-primary: '#ffffff'
  primary-container: '#0f6e5c'
  on-primary-container: '#9bedd6'
  inverse-primary: '#84d6c0'
  secondary: '#785a00'
  on-secondary: '#ffffff'
  secondary-container: '#fdd273'
  on-secondary-container: '#775800'
  tertiary: '#77372a'
  on-tertiary: '#ffffff'
  tertiary-container: '#944e3f'
  on-tertiary-container: '#ffd4cb'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#a0f2db'
  primary-fixed-dim: '#84d6c0'
  on-primary-fixed: '#002019'
  on-primary-fixed-variant: '#005142'
  secondary-fixed: '#ffdf9d'
  secondary-fixed-dim: '#ebc165'
  on-secondary-fixed: '#251a00'
  on-secondary-fixed-variant: '#5b4300'
  tertiary-fixed: '#ffdad3'
  tertiary-fixed-dim: '#ffb4a4'
  on-tertiary-fixed: '#3a0a03'
  on-tertiary-fixed-variant: '#733427'
  background: '#f7faf7'
  on-background: '#181c1b'
  surface-variant: '#e0e3e0'
  bg-base: '#F6F8F7'
  bg-surface: '#FFFFFF'
  bg-subtle: '#EDF1EF'
  ink-primary: '#1B2430'
  ink-secondary: '#5C6670'
  border-hairline: '#DDE3E0'
  accent-verified-tint: '#E3F0EC'
  status-warning: '#B8863A'
  status-error: '#B24B41'
typography:
  display-lg:
    fontFamily: Source Serif 4
    fontSize: 40px
    fontWeight: '600'
    lineHeight: 48px
  display-md:
    fontFamily: Source Serif 4
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 40px
  headline-lg:
    fontFamily: Source Serif 4
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  headline-md:
    fontFamily: Source Serif 4
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: IBM Plex Sans
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: IBM Plex Sans
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-md:
    fontFamily: IBM Plex Sans
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.02em
  label-sm:
    fontFamily: IBM Plex Sans
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
  code-md:
    fontFamily: IBM Plex Mono
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base-unit: 8px
  max-width-form: 720px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 32px
---

# Verification Engine — Design System

## Color (light mode only)
- `bg-base`: `#F6F8F7` (Page background)
- `bg-surface`: `#FFFFFF` (Cards, panels, inputs)
- `bg-subtle`: `#EDF1EF` (Section dividers, disabled fields)
- `ink-primary`: `#1B2430` (Headings, primary text)
- `ink-secondary`: `#5C6670` (Body copy, captions)
- `border-hairline`: `#DDE3E0` (Card borders)
- `accent-verified`: `#0F6E5C` (Primary accent, CTAs, links)
- `accent-verified-tint`: `#E3F0EC` (Badges, selected states)
- `accent-brass`: `#A9852F` (Secondary accent, used for financial artifacts)
- `status-warning`: `#B8863A` (Muted amber)
- `status-error`: `#B24B41` (Muted brick)

## Typography
- **Display**: Source Serif 4 (Refined serif, engraved character)
- **Body/UI**: IBM Plex Sans (Clean humanist sans)
- **Utility/Mono**: IBM Plex Mono (Precise values, claim IDs)

## Layout & Components
- **Grid**: 8px baseline grid, max content width 720px for forms.
- **Cards**: 1px hairline border (`border-hairline`), minimal 4-8px soft shadow, 12px corner radius.
- **Buttons**: Solid `accent-verified` (primary), Outline/Ghost (secondary).
- **Forms**: Labels above fields, inline validation, generous touch targets.
- **Signature Element**: Engraved hairline corner motif (flourish) for verification and confirmation cards.
