import { createTheme } from "@mui/material/styles"

const css = (name: string) =>
  getComputedStyle(document.documentElement).getPropertyValue(name).trim()
export function makeTheme() {
  return createTheme({
    palette: {
      primary: {
        main: css("--brand-primary"),
        dark: css("--brand-hover"),
        light: css("--brand-secondary"),
      },
      background: {
        default: css("--brand-tertiary"),
        paper: css("--surface-bg"),
      },
      text: {
        primary: css("--text-primary"),
        secondary: css("--text-secondary"),
      },
      divider: css("--border-secondary"),
      success: { main: css("--success") },
      warning: { main: css("--warning") },
      error: { main: css("--danger") },
    },
    typography: {
      fontFamily: css("--font-sans"),
      fontSize: 14,
      h4: {
        fontSize: "var(--title-size)",
        fontWeight: 600,
        letterSpacing: "-.035em",
        lineHeight: 1.35,
      },
      h5: {
        fontSize: "var(--text-title-size)",
        fontWeight: 600,
        letterSpacing: "-.025em",
      },
      h6: {
        fontSize: "var(--text-heading-size)",
        fontWeight: 600,
        letterSpacing: "-.02em",
      },
      subtitle1: { fontWeight: 600, fontSize: "var(--text-label-size)" },
      subtitle2: { fontWeight: 600 },
      body2: { lineHeight: 1.65 },
      button: { textTransform: "none", fontWeight: 600 },
    },
    shape: { borderRadius: 12 },
    components: {
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: {
            borderRadius: "var(--corner-md)",
            padding: "var(--space-md) var(--space-xl)",
            minHeight: 38,
            fontSize: "var(--text-small-size)",
          },
          outlined: {
            borderColor: "var(--border-primary)",
            color: "var(--text-primary)",
            background: "var(--surface-bg)",
          },
          text: { minWidth: 0 },
        },
      },
      MuiPaper: {
        defaultProps: { elevation: 0 },
        styleOverrides: { root: { backgroundImage: "none" } },
      },
      MuiCard: {
        styleOverrides: { root: { borderRadius: "var(--corner-lg)" } },
      },
      MuiTextField: { defaultProps: { size: "small", fullWidth: true } },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            borderRadius: "var(--corner-md)",
            fontSize: "var(--text-small-size)",
            background: "var(--surface-bg)",
          },
          notchedOutline: { borderColor: "var(--border-primary)" },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: "var(--corner-sm)",
            height: 25,
            fontSize: "var(--text-meta-size)",
            fontWeight: 500,
          },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: {
            borderColor: "var(--border-secondary)",
            fontSize: "var(--text-small-size)",
            padding: "var(--space-xl)",
          },
          head: {
            background: "var(--bg-faint)",
            color: "var(--text-secondary)",
            fontWeight: 500,
          },
        },
      },
      MuiDialog: {
        styleOverrides: { paper: { borderRadius: "var(--corner-lg)" } },
      },
      MuiTooltip: { defaultProps: { arrow: true } },
    },
  })
}
