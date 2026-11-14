"use client";
import { FormEvent, useCallback, useEffect, useRef, useState } from "react";

type Status = "active" | "inactive";
type Account = { id: string; holderName: string; cpf: string; status: Status; version: number };
type Draft = { holderName: string; cpf: string; status: Status };
const empty: Draft = { holderName: "", cpf: "", status: "active" };
const pageSize = 20;
const base = "/backend";

export default function Accounts() {
  const [items, setItems] = useState<Account[]>([]);
  const [offset, setOffset] = useState(0);
  const [hasNext, setHasNext] = useState(false);
  const [loading, setLoading] = useState(true);
  const [needsLogin, setNeedsLogin] = useState(false);
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
  const nameInput = useRef<HTMLInputElement>(null);

  const load = useCallback(async (start: number) => {
    const request = ++requestNumber.current;
    setLoading(true);
    setLoadError("");
    try {
      const response = await fetch(`${base}?limit=${pageSize + 1}&offset=${start}`, { cache: "no-store" });
      if (response.status === 401 && request === requestNumber.current) {
        setNeedsLogin(true);
        return;
      }
      if (!response.ok) {
        if (request === requestNumber.current) setLoadError(response.status === 403
          ? "Seu usuário não tem permissão para acessar contas."
          : "Não foi possível carregar as contas. Tente novamente.");
        return;
      }
      if (request === requestNumber.current) setNeedsLogin(false);
      const { data: accounts }: { data: Account[] } = await response.json();
      if (request !== requestNumber.current) return;
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
        if (response.status === 401) setNeedsLogin(true);
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
      setError("A conexão falhou. Repita a operação para usar a mesma chave.");
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }

  function edit(account: Account | null) {
    setDeleting(null);
    setError("");
    setEditing(account);
    setDraft(account ? { holderName: account.holderName, cpf: "", status: account.status } : empty);
    nameInput.current?.focus();
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const current = editing;
    void write(current ? "PUT" : "POST", current ? `${base}/${current.id}` : base,
      current ? { holderName: draft.holderName, status: draft.status }
        : { holderName: draft.holderName, cpf: draft.cpf },
      () => { edit(null); setNotice(current ? "Conta atualizada." : "Conta aberta."); });
  }

  if (needsLogin) return (
    <main>
      <header><span className="brand">Gestão de clientes</span><h1>Contas</h1></header>
      <section className="panel">
        <h2>Entrar no sistema</h2>
        <p>Entre para gerenciar as contas.</p>
        <a className="primary login-link" href="/auth/login">Entrar com Keycloak</a>
      </section>
    </main>
  );

  return (
    <main>
      <header className="page-header">
        <div><span className="brand">Gestão de clientes</span><h1>Contas</h1></div>
        {!loading && <form method="post" action="/auth/logout"><button type="submit">Sair</button></form>}
      </header>
      <div className="workspace">
        <section className="panel" aria-labelledby="accounts-heading">
          <div className="panel-title"><h2 id="accounts-heading">Contas cadastradas</h2>
            <button className="subtle" type="button" onClick={() => void load(offset)} disabled={loading || busy}>Atualizar lista</button></div>
          {loadError && <p className="error" role="alert">{loadError}</p>}
          {notice && <p className="success" role="status">{notice}</p>}
          {loading && <p className="muted" role="status">Carregando contas…</p>}
          {!loading && !loadError && !items.length && <p className="muted">Nenhuma conta nesta página.</p>}
          {!!items.length && <div className="table-scroll"><table>
            <thead><tr><th>Titular</th><th>CPF</th><th>Status</th><th>Ações</th></tr></thead>
            <tbody>{items.map(account => <tr key={account.id}>
              <td>{account.holderName}</td><td className="cpf">{account.cpf}</td>
              <td><span className={`badge ${account.status}`}>{account.status === "active" ? "Ativa" : "Inativa"}</span></td>
              <td className="actions"><button className="subtle" disabled={busy} onClick={() => edit(account)} aria-label={`Editar conta de ${account.holderName}`}>Editar</button>
                <button className="subtle danger" disabled={busy} aria-label={`Excluir conta de ${account.holderName}`} onClick={() => { setError(""); setDeleting(account); }}>Excluir</button></td>
            </tr>)}</tbody>
          </table></div>}
          {(offset > 0 || hasNext) && <nav aria-label="Páginas de contas" className="actions pager">
            <button disabled={!offset || loading || busy} onClick={() => void load(Math.max(0, offset - pageSize))}>Anterior</button>
            <span>Página {Math.floor(offset / pageSize) + 1}</span>
            <button disabled={!hasNext || loading || busy} onClick={() => void load(offset + pageSize)}>Próxima</button>
          </nav>}
          {deleting && <div className="confirm" role="group" aria-label="Confirmar exclusão">
            <p>Excluir a conta de <strong>{deleting.holderName}</strong>?</p>
            {error && <p className="error" role="alert">{error}</p>}
            <div className="actions"><button disabled={busy} onClick={() => { setDeleting(null); setError(""); }}>Cancelar</button>
              <button className="destructive" disabled={busy} onClick={() => void write("DELETE", `${base}/${deleting.id}`, null, () => {
                if (editing?.id === deleting.id) edit(null);
                setDeleting(null);
                setNotice("Conta excluída.");
              })}>{busy ? "Excluindo…" : "Confirmar exclusão"}</button></div>
          </div>}
        </section>
        <section className="panel" aria-labelledby="form-heading">
          <h2 id="form-heading">{editing ? "Editar conta" : "Abrir conta"}</h2>
          <form onSubmit={save}>
            <fieldset disabled={busy || !!deleting}>
              <label htmlFor="holder">Nome do titular</label>
              <input id="holder" ref={nameInput} value={draft.holderName} onChange={e => setDraft({ ...draft, holderName: e.target.value })} required minLength={2} maxLength={120} />
              {editing ? <>
                <label htmlFor="status">Status</label>
                <select id="status" value={draft.status} onChange={e => setDraft({ ...draft, status: e.target.value as Status })}>
                  <option value="active">Ativa</option><option value="inactive">Inativa</option>
                </select>
              </> : <>
                <label htmlFor="cpf">CPF</label>
                <input id="cpf" inputMode="numeric" placeholder="000.000.000-00" maxLength={14} required value={draft.cpf} onChange={e => setDraft({ ...draft, cpf: e.target.value })} />
              </>}
              {!deleting && error && <p className="error" role="alert">{error}</p>}
              <div className="actions form-actions"><button className="primary" type="submit">{busy ? "Salvando…" : editing ? "Salvar alterações" : "Abrir conta"}</button>
                {editing && <button type="button" onClick={() => edit(null)}>Cancelar</button>}</div>
            </fieldset>
          </form>
        </section>
      </div>
    </main>
  );
}
