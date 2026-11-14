"use client";
import { FormEvent, useCallback, useEffect, useRef, useState } from "react";

import { Brand } from "../components/Brand";
import { Login } from "../components/Login";
import { Overlay } from "../components/Overlay";

type Status = "active" | "inactive";
type Account = { id: string; holderName: string; cpf: string; status: Status; version: number };
type Draft = { holderName: string; cpf: string; status: Status };
const empty: Draft = { holderName: "", cpf: "", status: "active" };
const pageSize = 20;
const base = "/backend";

export default function Accounts() {
  const [permissions, setPermissions] = useState({ canWrite: false, canAssign: false });
  const [assignment, setAssignment] = useState<{ id: string; kind: "portfolio" | "owner"; name: string } | null>(null);
  const [subject, setSubject] = useState("");
  const [items, setItems] = useState<Account[]>([]);
  const [offset, setOffset] = useState(0);
  const [hasNext, setHasNext] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loginReason, setLoginReason] = useState<"required" | "expired" | "failed" | null>(null);
  const authenticated = useRef(false);
  const [forbidden, setForbidden] = useState(false);
  const [formOpen, setFormOpen] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const [draft, setDraft] = useState(empty);
  const [editing, setEditing] = useState<Account | null>(null);
  const [deleting, setDeleting] = useState<Account | null>(null);
  const [busy, setBusy] = useState(false);
  const pending = useRef(new Map<string, string>());
  const inFlight = useRef(false);
  const requestNumber = useRef(0);
  const refreshButton = useRef<HTMLButtonElement>(null);

  const load = useCallback(async (start: number) => {
    const request = ++requestNumber.current;
    setLoading(true);
    setLoadError("");
    try {
      const response = await fetch(`${base}?limit=${pageSize + 1}&offset=${start}`, { cache: "no-store" });
      if (response.status === 401 && request === requestNumber.current) {
        setLoginReason(new URLSearchParams(window.location.search).get("login") === "failed"
          ? "failed" : authenticated.current ? "expired" : "required");
        return;
      }
      if (!response.ok) {
        if (request === requestNumber.current) {
          setForbidden(response.status === 403);
          if (response.status === 403) {
            setItems([]);
            setPermissions({ canWrite: false, canAssign: false });
          }
          setLoadError(response.status === 403
            ? "Seu usuário não tem permissão para acessar contas."
            : "Não foi possível carregar as contas. Tente novamente.");
        }
        return;
      }

      const permissionResponse = await fetch(base + "/permissions", { cache: "no-store" });
      const nextPermissions = permissionResponse.ok ? (await permissionResponse.json()).data : { canWrite: false, canAssign: false };
      const { data: accounts }: { data: Account[] } = await response.json();
      if (request !== requestNumber.current) return;
      authenticated.current = true;
      setForbidden(false);
      setLoginReason(null);
      setPermissions(nextPermissions);
      setItems(accounts.slice(0, pageSize));
      setHasNext(accounts.length > pageSize);
      setOffset(start);
    } catch {
      if (request === requestNumber.current) setLoadError("Não foi possível carregar as contas. Tente novamente.");
    } finally {
      if (request === requestNumber.current) setLoading(false);
    }
  }, []);

  useEffect(() => { void load(0); }, [load]);

  async function write(method: string, url: string, body: object | null, done: () => void) {
    if (inFlight.current) return;
    inFlight.current = true;
    setBusy(true);
    setError("");
    setNotice("");
    const content = body === null ? undefined : JSON.stringify(body);
    const fingerprint = JSON.stringify([method, url, content]);
    const key = pending.current.get(fingerprint) ?? crypto.randomUUID();
    pending.current.set(fingerprint, key);
    try {
      const response = await fetch(url, {
        method, body: content,
        headers: { "Idempotency-Key": key, ...(content ? { "Content-Type": "application/json" } : {}) },
      });
      if (!response.ok) {
        if (response.status === 401) setLoginReason("expired");
        const problem = await response.json().catch(() => null);
        if (response.status < 500 && response.status !== 408 && response.status !== 429)
          pending.current.delete(fingerprint);
        setError(problem?.error?.message ?? "Não foi possível concluir a operação.");
        return;
      }
      pending.current.delete(fingerprint);
      done();
      await load(0);
    } catch {
      setError("A conexão falhou. Tente novamente.");
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }

  function closeOverlay() {
    setFormOpen(false);
    setEditing(null);
    setDeleting(null);
    setAssignment(null);
    setError("");
  }

  function edit(account: Account | null) {
    closeOverlay();
    setEditing(account);
    setDraft(account ? { holderName: account.holderName, cpf: "", status: account.status } : empty);
    setFormOpen(true);
  }

  function startAssignment(account: Account, kind: "portfolio" | "owner") {
    closeOverlay();
    setSubject("");
    setAssignment({ id: account.id, kind, name: account.holderName });
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const current = editing;
    void write(current ? "PUT" : "POST", current ? `${base}/${current.id}` : base,
      current ? { holderName: draft.holderName, status: draft.status } : { holderName: draft.holderName, cpf: draft.cpf },
      () => { closeOverlay(); setNotice(current ? "Conta atualizada." : "Conta aberta."); });
  }

  if (loginReason) return <Login reason={loginReason} />;

  return (
    <>
      <header className="topbar">
        <div className="topbar-inner">
          <Brand />
          {authenticated.current || forbidden ? (
            <form method="post" action="/auth/logout"><button type="submit" className="logout"><span aria-hidden="true">↪</span> Sair</button></form>
          ) : null}
        </div>
      </header>

      <main className="workspace">
        <div className="page-intro">
          <div>
            <h1>Contas</h1>
            <p>{permissions.canAssign ? "Consulte e gerencie as contas cadastradas." :
              permissions.canWrite ? "Consulte e gerencie as contas da sua carteira." : "Consulte as contas disponíveis para o seu perfil."}</p>
          </div>
          {!forbidden && <div className="actions toolbar">
            <button ref={refreshButton} type="button" onClick={() => void load(offset)} disabled={loading || busy}>
              <span aria-hidden="true">↻</span> Atualizar lista
            </button>
            {permissions.canWrite && <button type="button" className="primary" disabled={busy || loading} onClick={() => edit(null)}>
              <span aria-hidden="true">+</span> Nova conta
            </button>}
          </div>}
        </div>

        {notice && <p className="feedback success" role="status">{notice}</p>}
        {loadError && <p className="feedback error" role="alert">{loadError}</p>}
        {!forbidden && <section className="panel" aria-labelledby="accounts-heading" aria-busy={loading}>
          <div className="panel-title">
            <h2 id="accounts-heading">Contas cadastradas</h2>
            {!loading && !loadError && items.length > 0 && <span className="count">{items.length} {items.length === 1 ? "conta nesta página" : "contas nesta página"}</span>}
          </div>
          {loading && !items.length && <div className="empty-state" role="status">
            <span>Carregando contas…</span><div className="skeleton" aria-hidden="true" />
          </div>}
          {!loading && !loadError && !items.length && <div className="empty-state">
            <span className="state-symbol" aria-hidden="true">▤</span>
            <p>Nenhuma conta nesta página.</p>
            {permissions.canWrite && offset === 0 && <button className="primary" type="button" onClick={() => edit(null)}>Nova conta</button>}
          </div>}
          {items.length > 0 && <table aria-label="Contas cadastradas" role="table">
            <thead role="rowgroup"><tr role="row">
              <th role="columnheader" scope="col">Titular da conta</th>
              <th role="columnheader" scope="col">Identificador (CPF)</th>
              <th role="columnheader" scope="col">Status</th>
              {permissions.canWrite && <th role="columnheader" scope="col">Ações</th>}
            </tr></thead>
            <tbody role="rowgroup">{items.map(account => (
              <tr key={account.id} role="row">
                <td className="holder-cell" role="cell"><div className="holder">
                  <span className={`avatar ${account.status}`} aria-hidden="true">{account.holderName.trim().split(/\s+/).slice(0, 2).map(word => Array.from(word)[0]).join("").toLocaleUpperCase("pt-BR")}</span>
                  <strong>{account.holderName}</strong>
                </div></td>
                <td className="cpf-cell" role="cell"><span className="mobile-label">CPF </span><span className="cpf">{account.cpf}</span></td>
                <td className="status-cell" role="cell"><span className={`badge ${account.status}`}>{account.status === "active" ? "Ativa" : "Inativa"}</span></td>
                {permissions.canWrite && <td className="row-actions" role="cell"><div className="actions">
                  <button className="compact" disabled={busy} onClick={() => edit(account)} aria-label={`Editar conta de ${account.holderName}`}>Editar</button>
                  <button className="compact danger" disabled={busy} aria-label={`Excluir conta de ${account.holderName}`} onClick={() => { closeOverlay(); setDeleting(account); }}>Excluir</button>
                  {permissions.canAssign && <>
                    <button className="compact" disabled={busy} onClick={() => startAssignment(account, "portfolio")}>Transferir carteira</button>
                    <button className="compact" disabled={busy} onClick={() => startAssignment(account, "owner")}>Vincular titular</button>
                  </>}
                </div></td>}
              </tr>
            ))}</tbody>
          </table>}
          {(offset > 0 || hasNext) && <nav aria-label="Páginas de contas" className="actions pager">
            <button disabled={!offset || loading || busy} onClick={() => void load(Math.max(0, offset - pageSize))}>Anterior</button>
            <span>Página {Math.floor(offset / pageSize) + 1}</span>
            <button disabled={!hasNext || loading || busy} onClick={() => void load(offset + pageSize)}>Próxima</button>
          </nav>}
        </section>}
      </main>
      <footer className="site-footer"><div>Banco KRT <span>Gestão de contas</span></div></footer>

      {formOpen && permissions.canWrite && <Overlay kind="drawer" title={editing ? "Editar conta" : "Abrir nova conta"}
        subtitle={editing?.holderName} busy={busy} onClose={closeOverlay}>
        <form className="overlay-form" onSubmit={save}>
          <fieldset className="overlay-body" disabled={busy}>
            <label htmlFor="holder">Nome do titular</label>
            <input id="holder" value={draft.holderName} onChange={e => setDraft({ ...draft, holderName: e.target.value })} required minLength={2} maxLength={120} autoFocus />
            {editing ? <>
              <div className="field"><span>CPF do titular</span><code className="cpf">{editing.cpf}</code></div>
              <label htmlFor="status">Status</label>
              <select id="status" value={draft.status} onChange={e => setDraft({ ...draft, status: e.target.value as Status })}>
                <option value="active">Ativa</option><option value="inactive">Inativa</option>
              </select>
            </> : <>
              <label htmlFor="cpf">CPF</label>
              <input id="cpf" inputMode="numeric" placeholder="000.000.000-00" maxLength={14} required value={draft.cpf} onChange={e => setDraft({ ...draft, cpf: e.target.value })} />
              <p className="feedback info">Novas contas começam com status <strong>Ativa</strong>.</p>
            </>}
            {error && <p className="feedback error" role="alert">{error}</p>}
          </fieldset>
          <div className="overlay-footer actions">
            <button type="button" disabled={busy} onClick={closeOverlay}>Cancelar</button>
            <button className="primary" type="submit" disabled={busy}>{busy ? "Salvando…" : editing ? "Salvar alterações" : "Abrir conta"}</button>
          </div>
        </form>
      </Overlay>}

      {assignment && permissions.canAssign && <Overlay kind="drawer"
        title={assignment.kind === "portfolio" ? "Transferir carteira" : "Vincular titular"} subtitle={assignment.name} busy={busy} onClose={closeOverlay}>
        <form className="overlay-form" onSubmit={event => {
          event.preventDefault();
          void write("PUT", `${base}/${assignment.id}/${assignment.kind}`, { subject }, () => {
            closeOverlay();
            setNotice(assignment.kind === "portfolio" ? "Carteira atualizada." : "Titular vinculado.");
          });
        }}>
          <fieldset className="overlay-body" disabled={busy}>
            <label htmlFor="subject">ID no Keycloak do {assignment.kind === "portfolio" ? "gerente" : "titular"}</label>
            <input id="subject" required pattern="[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}"
              value={subject} onChange={event => setSubject(event.target.value)} autoFocus />
            {error && <p className="feedback error" role="alert">{error}</p>}
          </fieldset>
          <div className="overlay-footer actions">
            <button type="button" disabled={busy} onClick={closeOverlay}>Cancelar</button>
            <button className="primary" type="submit" disabled={busy}>{busy ? "Salvando…" : "Confirmar vínculo"}</button>
          </div>
        </form>
      </Overlay>}

      {deleting && permissions.canWrite && <Overlay kind="danger" title={`Excluir a conta de ${deleting.holderName}?`} busy={busy} onClose={closeOverlay}>
        <div className="overlay-body">
          <div className="account-summary"><span>CPF do titular</span><code className="cpf">{deleting.cpf}</code></div>
          <p className="feedback warning">A conta deixará de aparecer na lista.</p>
          {error && <p className="feedback error" role="alert">{error}</p>}
        </div>
        <div className="overlay-footer actions">
          <button type="button" disabled={busy} onClick={closeOverlay} autoFocus>Cancelar</button>
          <button type="button" className="destructive" disabled={busy} onClick={() => void write("DELETE", `${base}/${deleting.id}`, null, () => {
            closeOverlay();
            setNotice("Conta excluída.");
            requestAnimationFrame(() => refreshButton.current?.focus());
          })}>{busy ? "Excluindo…" : "Confirmar exclusão"}</button>
        </div>
      </Overlay>}
    </>
  );
}
