"use client";

import { useId, useSyncExternalStore } from "react";
import { Icon } from "./Icon";

type Appearance = "system" | "light" | "dark";

declare global {
  interface Window {
    krtTheme?: { getPreference: () => Appearance; setPreference: (value: Appearance) => void };
  }
}

function subscribe(onChange: () => void) {
  window.addEventListener("krt-theme-change", onChange);
  return () => window.removeEventListener("krt-theme-change", onChange);
}

const currentPreference = () => window.krtTheme?.getPreference() ?? "system";
const serverPreference = () => "system" as const;

export function ThemeSwitcher() {
  const id = useId();
  const appearance = useSyncExternalStore(subscribe, currentPreference, serverPreference);

  return <div className="theme-switcher">
    <Icon name="appearance" />
    <label className="sr-only" htmlFor={id}>Aparência</label>
    <select id={id} value={appearance} onChange={event => window.krtTheme?.setPreference(event.target.value as Appearance)}>
      <option value="system">Sistema</option>
      <option value="light">Claro</option>
      <option value="dark">Escuro</option>
    </select>
  </div>;
}
