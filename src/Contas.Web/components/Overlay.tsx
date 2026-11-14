"use client";

import { type ReactNode, useEffect, useId, useRef } from "react";

export function Overlay({ kind, title, subtitle, busy, onClose, children }: {
  kind: "drawer" | "danger";
  title: string;
  subtitle?: string;
  busy: boolean;
  onClose: () => void;
  children: ReactNode;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  const heading = useId();

  useEffect(() => {
    const element = dialog.current;
    element?.showModal();
    return () => element?.close();
  }, []);

  return (
    <dialog ref={dialog} className={`overlay ${kind === "drawer" ? "drawer" : "confirm"}`} aria-labelledby={heading}
      onCancel={event => { event.preventDefault(); if (!busy) onClose(); }}>
      <div className="overlay-header">
        <div><h2 id={heading}>{title}</h2>{subtitle && <p>{subtitle}</p>}</div>
        <button type="button" className="icon-button" aria-label="Fechar" disabled={busy} onClick={onClose}>×</button>
      </div>
      {children}
    </dialog>
  );
}
