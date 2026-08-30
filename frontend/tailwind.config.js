/**
 * Design tokens copied verbatim from docs/design.md and docs/design-tokens.md - the
 * project's design brief for this app. Nothing here should drift from that document;
 * if a screen needs a value not listed there, extrapolate from these tokens rather
 * than adding an arbitrary one-off (see design.md section 2, "Design philosophy").
 *
 * @type {import('tailwindcss').Config}
 */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        "bg-base": "#F6F8F7",
        "bg-surface": "#FFFFFF",
        "bg-subtle": "#EDF1EF",
        "ink-primary": "#1B2430",
        "ink-secondary": "#5C6670",
        "border-hairline": "#DDE3E0",
        "accent-verified": "#0F6E5C",
        "accent-verified-tint": "#E3F0EC",
        "accent-brass": "#A9852F",
        "status-warning": "#B8863A",
        "status-warning-tint": "#F5EBDC",
        "status-error": "#B24B41",
        "status-error-tint": "#F6E7E5"
      },
      fontFamily: {
        display: ['"Source Serif 4"', "serif"],
        body: ['"IBM Plex Sans"', "sans-serif"],
        mono: ['"IBM Plex Mono"', "monospace"]
      },
      fontSize: {
        // [fontSize, { lineHeight, fontWeight }] - matches design.md's type scale exactly.
        "display-lg": ["40px", { lineHeight: "48px", fontWeight: "600" }],
        "display-md": ["32px", { lineHeight: "40px", fontWeight: "600" }],
        "headline-lg": ["24px", { lineHeight: "32px", fontWeight: "600" }],
        "headline-md": ["20px", { lineHeight: "28px", fontWeight: "600" }],
        "body-lg": ["18px", { lineHeight: "28px", fontWeight: "400" }],
        "body-md": ["16px", { lineHeight: "24px", fontWeight: "400" }],
        "label-md": ["14px", { lineHeight: "20px", letterSpacing: "0.02em", fontWeight: "600" }],
        "label-sm": ["12px", { lineHeight: "16px", fontWeight: "500" }],
        "code-md": ["14px", { lineHeight: "20px", fontWeight: "400" }]
      },
      borderRadius: {
        DEFAULT: "0.5rem",
        md: "0.75rem",
        lg: "1rem",
        xl: "1.5rem"
      },
      spacing: {
        "margin-mobile": "16px",
        "margin-desktop": "32px",
        gutter: "24px"
      },
      maxWidth: {
        form: "720px"
      }
    }
  },
  plugins: []
};
