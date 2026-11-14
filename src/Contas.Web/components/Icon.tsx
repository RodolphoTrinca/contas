import type { CSSProperties } from "react";

const paths = {
  plus: "M12 5v14M5 12h14",
  refresh: "M20 7v5h-5M4 17v-5h5M6 7a7 7 0 0 1 12-1l2 3M4 15l2 3a7 7 0 0 0 12-1",
  edit: "m15 5 4 4M4 20l4-1L20 7a2.8 2.8 0 0 0-4-4L4 15v5Z",
  trash: "M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7M14 10v7",
  close: "m6 6 12 12M6 18 18 6",
  search: "M21 21l-5-5M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0",
  shield: "m12 3 8 3v5c0 5-4 8-8 10-4-2-8-5-8-10V6l8-3Zm-4 9 3 3 5-6",
  lock: "M6 10h12v11H6V10Zm3 0V6a3 3 0 0 1 6 0v4M12 14v3",
  key: "M10 9a4 4 0 1 1-8 0 4 4 0 0 1 8 0Zm0 0h11v4h-4v-2h-3V9",
  logout: "M9 4H4v16h5M9 12h12m-5-5 5 5-5 5",
  wallet: "M20 8V5H4a2 2 0 0 0 0 4h17v11H4a2 2 0 0 1-2-2V7m19 6h-6v4h6M17 15h.01",
  user: "M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM4 21v-2a8 8 0 0 1 16 0v2H4Z",
  transfer: "M3 7h17m-4-4 4 4-4 4M21 17H4m4-4-4 4 4 4",
  arrow: "M5 12h14m-6-6 6 6-6 6",
  chevron: "m9 5 7 7-7 7",
  warning: "m12 3 10 18H2L12 3Zm0 6v5m0 3h.01",
  check: "m5 12 4 4L19 6",
  info: "M12 8h.01M12 11v6m9-5a9 9 0 1 1-18 0 9 9 0 0 1 18 0",
  document: "M5 3h10l4 4v14H5V3Zm10 0v5h4M9 12h6m-6 4h6",
} as const;

export function Icon({ name, className = "", style }: { name: keyof typeof paths; className?: string; style?: CSSProperties }) {
  return <svg className={`icon ${className}`} style={style} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name]} /></svg>;
}
