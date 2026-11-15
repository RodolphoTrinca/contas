"use client";

import { type ReactNode, useEffect, useId, useRef } from "react";
import { Icon } from "./Icon";

export function Overlay({ kind, title, subtitle, mode, busy, onClose, children }: {
  kind: "drawer" | "danger";
  title: string;
  subtitle?: string;
  mode?: string;
  busy: boolean;
  onClose: () => void;
  children: ReactNode;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  const heading = useId();
  const description = useId();

  useEffect(() => {
    const element = dialog.current;
    const previous = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    element?.showModal();
    element?.querySelector<HTMLElement>("[data-initial-focus]")?.focus();
    return () => { element?.close(); if (previous?.isConnected) previous.focus(); };
  }, []);

  return <dialog ref={dialog} className={`overlay ${kind === "drawer" ? "drawer" : "confirm"}`} aria-labelledby={heading}
    aria-describedby={subtitle ? description : undefined} aria-busy={busy}
    onCancel={event => { event.preventDefault(); if (!busy) onClose(); }}>
    <div className="sheet-handle" aria-hidden="true"><span /></div>
    <div className="overlay-header">
      <span className="overlay-emblem"><Icon name={kind === "danger" ? "warning" : "wallet"} /></span>
      <div className="overlay-heading">{kind === "danger" && <span className="critical-label">Ação irreversível</span>}<h2 id={heading}>{title}</h2>{subtitle && <p id={description}>{subtitle}</p>}</div>
      <button type="button" className="icon-button close-button" aria-label="Fechar" disabled={busy} onClick={onClose}><Icon name="close" /></button>
    </div>
    {mode && <div className="mode-strip"><span>Modo ativo</span><strong>{mode}</strong></div>}
    {children}
  </dialog>;
}
