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
      info: { main: css("--brand-primary") },
    },
    typography: {
      fontFamily: css("--font-sans"),
      fontSize: 14,
      h4: {
        fontSize: "var(--title-size)",
        fontWeight: 650,
        letterSpacing: "-.035em",
        lineHeight: 1.35,
      },
      h5: {
        fontSize: "var(--text-title-size)",
        fontWeight: 650,
        letterSpacing: "-.025em",
      },
      h6: {
        fontSize: "var(--text-heading-size)",
        fontWeight: 600,
        letterSpacing: "-.02em",
      },
      subtitle1: { fontWeight: 600, fontSize: "var(--text-label-size)" },
      subtitle2: { fontWeight: 600, fontSize: "var(--text-small-size)" },
      body1: { fontSize: "var(--text-label-size)", lineHeight: 1.5 },
      body2: { fontSize: "var(--text-small-size)", lineHeight: 1.5 },
      caption: { fontSize: "var(--text-meta-size)", lineHeight: 1.5 },
      button: { textTransform: "none", fontWeight: 600 },
    },
    shape: { borderRadius: 6 },
    components: {
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: {
            borderRadius: "var(--corner-md)",
            padding: "var(--space-md) var(--space-xl)",
            minHeight: 44,
            fontSize: "var(--text-small-size)",
            "&.Mui-disabled": { background: "var(--unknown-soft)", color: "var(--text-secondary)" },
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
      MuiInputLabel: {
        defaultProps: { shrink: true },
        styleOverrides: {
          root: {
            position: "static",
            transform: "none",
            maxWidth: "100%",
            fontSize: "var(--text-meta-size)",
            fontWeight: 600,
            color: "var(--text-secondary)",
            marginBottom: "var(--space-xs)",
          },
        },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            borderRadius: "var(--corner-md)",
            fontSize: "var(--text-small-size)",
            background: "var(--surface-bg)",
            minHeight: 44,
            "&.Mui-focused": { outline: "3px solid var(--brand-primary)", outlineOffset: 2 },
          },
          notchedOutline: { borderColor: "var(--border-primary)", "& legend": { display: "none" }, top: 0 },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: "var(--corner-full)",
            height: 28,
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
      MuiIconButton: {
        styleOverrides: { root: { minWidth: 44, minHeight: 44 } },
      },
      MuiFormHelperText: {
        styleOverrides: { root: { marginLeft: 0, fontSize: "var(--text-meta-size)" } },
      },
      MuiDialog: {
        styleOverrides: { paper: { borderRadius: "var(--corner-panel)", margin: "var(--space-xl)", width: "calc(100% - var(--space-2xl))" } },
      },
      MuiDialogContent: {
        styleOverrides: { root: { padding: "var(--space-2xl)", "&:first-of-type": { paddingTop: "var(--space-2xl)" } } },
      },
      MuiDialogActions: {
        styleOverrides: { root: { padding: "var(--space-xl) var(--space-2xl) var(--space-2xl)", gap: "var(--space-md)", flexWrap: "wrap" } },
      },
      MuiTab: {
        styleOverrides: { root: { textTransform: "none", fontSize: "var(--text-small-size)", fontWeight: 500, minHeight: 48 } },
      },
      MuiAlert: {
        styleOverrides: { root: { borderRadius: "var(--corner-md)", fontSize: "var(--text-small-size)" } },
      },
      MuiTooltip: { defaultProps: { arrow: true } },
    },
  })
}
