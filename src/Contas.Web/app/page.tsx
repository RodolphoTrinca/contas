"use client";
import { FormEvent, useCallback, useEffect, useRef, useState } from "react";

import { Brand } from "../components/ui/Brand";
import { Login } from "../components/auth/Login";
import { Overlay } from "../components/ui/Overlay";
import { Icon } from "../components/ui/Icon";
import { ThemeSwitcher } from "../components/ui/ThemeSwitcher";
import { AccountForm } from "../features/accounts/AccountForm";
import { AccountTable } from "../features/accounts/AccountTable";
import type { Account, Draft } from "../features/accounts/accounts";

import { isDefinitiveStatus, prepareWrite } from "../features/accounts/pending-write";

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
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
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
  const writeScope = useRef<string | null>(null);
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
      writeScope.current = permissionResponse.headers.get("idempotency-scope");
      setForbidden(false);
      setLoginReason(null);
      setPermissions(nextPermissions);
      setItems(accounts.slice(0, pageSize));
      setHasNext(accounts.length > pageSize);
      setLastUpdated(new Date());
      setOffset(start);
    } catch {
      if (request === requestNumber.current) setLoadError("Não foi possível carregar as contas. Tente novamente.");
    } finally {
      if (request === requestNumber.current) setLoading(false);
    }
  }, []);

  useEffect(() => { void load(0); }, [load]);

  async function write(method: string, url: string, body: object | null, done: () => void, expectedVersion?: number) {
    if (inFlight.current) return;
    inFlight.current = true;
    setBusy(true);
    setError("");
    setNotice("");
    const content = body === null ? undefined : JSON.stringify(body);
    try {
      const headers = new Headers(content ? { "Content-Type": "application/json" } : undefined);
      let finish = () => {};
      if (method === "POST" || expectedVersion !== undefined) {
        try {
          const scope = writeScope.current;
          if (!scope) throw new Error("Escopo de escrita indisponível.");
          const storage = sessionStorage;
          const attempt = await prepareWrite(storage, scope, method, url, content, expectedVersion);
          if (!attempt) {
            setError("O prazo de reenvio acabou. Confira a conta antes de iniciar outra tentativa.");
            return;
          }
          headers.set("Idempotency-Key", attempt.key);
          if (attempt.expectedVersion !== undefined)
            headers.set("Expected-Version", String(attempt.expectedVersion));
          finish = () => {
            try { storage.removeItem(attempt.storageKey); } catch { /* Keep the pending key if storage is unavailable. */ }
          };
        } catch {
          setError("Não foi possível salvar a chave de reenvio. A operação não foi enviada.");
          return;
        }
      }
      const response = await fetch(url, { method, body: content, headers });
      if (isDefinitiveStatus(response.status)) finish();
      if (!response.ok) {
        if (response.status === 401) setLoginReason("expired");
        const problem = await response.json().catch(() => null);
        setError(problem?.error?.message ?? "Não foi possível concluir a operação.");
        return;
      }
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
      () => { closeOverlay(); setNotice(current ? "Conta atualizada." : "Conta aberta."); }, current?.version);
  }

  if (loginReason) return <Login reason={loginReason} />;

  const role = permissions.canAssign ? "Operador" : permissions.canWrite ? "Gerente" : "Cliente";
  const filtered = items.filter(account =>
    (statusFilter === "all" || account.status === statusFilter) &&
    (account.holderName.toLocaleLowerCase("pt-BR").includes(search.toLocaleLowerCase("pt-BR")) || account.cpf.includes(search)));

  return <>
    <a className="skip-link" href="#workspace">Ir para as contas</a>
    <header className="topbar"><div className="topbar-inner">
      <div className="header-identity"><Brand /><span className="context-chip operation-chip">Operação interna <span>| Gestão de contas</span></span><nav aria-label="Navegação principal"><a href="/" aria-current="page">Contas</a></nav></div>
      <div className="header-session"><ThemeSwitcher />{(authenticated.current || forbidden) && <><span className="session-chip"><span className="status-dot" />Sessão ativa</span><div className="profile"><span className="profile-icon"><Icon name="user" /></span><div><strong>{forbidden ? "Acesso restrito" : role}</strong><span>Banco KRT</span></div></div><form method="post" action="/auth/logout"><button type="submit" className="logout"><Icon name="logout" />Sair</button></form></>}</div>
    </div></header>

    <main id="workspace" className="workspace">
      {permissions.canAssign && !forbidden && <nav className="workspace-nav" aria-label="Área de trabalho">
        <a href="/" aria-current="page">Contas</a>
        <a href="/historico">Histórico de alterações</a>
      </nav>}
      {notice && <div className="feedback success notice" role="status"><Icon name="check" /><p>{notice}</p><button className="icon-button" aria-label="Fechar notificação" onClick={() => setNotice("")}><Icon name="close" /></button></div>}
      <div className="page-intro"><div><p className="eyebrow">Gestão de contas <span>• {permissions.canAssign ? "Visão geral" : permissions.canWrite ? "Minha carteira" : "Minhas contas"}</span></p><h1>Contas</h1><p className="intro-description">{permissions.canAssign ? "Consulte e gerencie as contas cadastradas." : permissions.canWrite ? "Consulte e gerencie as contas da sua carteira." : "Consulte as contas disponíveis para o seu perfil."}</p></div>
        {!forbidden && <div className="intro-tools"><div className="sync-status"><span>Última atualização</span><strong>{lastUpdated ? lastUpdated.toLocaleTimeString("pt-BR") : "Carregando…"}</strong></div><div className="actions toolbar"><button ref={refreshButton} type="button" onClick={() => void load(offset)} disabled={loading || busy}><Icon name="refresh" className={loading ? "spinning" : ""} /><span>Atualizar lista</span></button>{permissions.canWrite && <button type="button" className="primary" disabled={busy || loading} onClick={() => edit(null)}><Icon name="plus" />Nova conta</button>}</div></div>}
      </div>

      {loadError && <div className="feedback error" role="alert"><Icon name="warning" /><div><strong>{forbidden ? "Acesso não autorizado" : "Não foi possível atualizar"}</strong><p>{loadError}</p></div></div>}
      {!forbidden && <section className="panel" aria-labelledby="accounts-heading" aria-busy={loading}>
        <div className="panel-title"><div className="panel-heading"><Icon name="wallet" /><h2 id="accounts-heading">Contas cadastradas</h2>{!loading && !loadError && <span className="count">{items.length} {items.length === 1 ? "conta" : "contas"}</span>}</div><span className="list-note"><Icon name="info" />Exibidas conforme seu acesso</span></div>
        <div className="list-tools"><div className="filter-pills" role="group" aria-label="Filtrar por status nesta página">{[{ value: "all", label: "Todas" }, { value: "active", label: "Ativas" }, { value: "inactive", label: "Inativas" }].map(filter => <button key={filter.value} aria-pressed={statusFilter === filter.value} onClick={() => setStatusFilter(filter.value)}>{filter.label}<span>{filter.value === "all" ? items.length : items.filter(account => account.status === filter.value).length}</span></button>)}</div><div className="search-field"><label className="sr-only" htmlFor="account-search">Filtrar por nome ou CPF nesta página</label><Icon name="search" /><input id="account-search" type="search" placeholder="Filtrar nesta página…" value={search} onChange={event => setSearch(event.target.value)} />{search && <button className="icon-button" onClick={() => setSearch("")} aria-label="Limpar busca"><Icon name="close" /></button>}</div></div>
        {loading && !items.length && <div className="empty-state" role="status"><Icon name="refresh" className="spinning" /><strong>Carregando contas…</strong><div className="skeleton" aria-hidden="true" /></div>}
        {!loading && !loadError && !items.length && <div className="empty-state"><span className="state-symbol"><Icon name="wallet" /></span><h3>Nenhuma conta nesta página.</h3><p>{permissions.canWrite ? "Abra uma conta para começar a gerenciar os titulares." : "As contas vinculadas ao seu perfil aparecerão aqui."}</p>{permissions.canWrite && offset === 0 && <button className="primary" type="button" onClick={() => edit(null)}><Icon name="plus" />Nova conta</button>}</div>}
        {!loading && items.length > 0 && !filtered.length && <div className="empty-state"><span className="state-symbol"><Icon name="search" /></span><h3>Nenhuma conta encontrada</h3><p>Revise os filtros aplicados a esta página.</p><button onClick={() => { setSearch(""); setStatusFilter("all"); }}>Limpar filtros</button></div>}
        {filtered.length > 0 && <AccountTable items={filtered} canWrite={permissions.canWrite} canAssign={permissions.canAssign} busy={busy} onEdit={edit} onDelete={account => { closeOverlay(); setDeleting(account); }} onAssign={startAssignment} />}
        <div className="panel-footer"><span className="list-note"><Icon name="document" />{filtered.length} {filtered.length === 1 ? "conta exibida" : "contas exibidas"} nesta página</span><nav aria-label="Páginas de contas" className="actions pager"><button disabled={!offset || loading || busy} onClick={() => void load(Math.max(0, offset - pageSize))}><Icon name="chevron" className="previous" />Anterior</button><span aria-current="page">Página {Math.floor(offset / pageSize) + 1}</span><button disabled={!hasNext || loading || busy} onClick={() => void load(offset + pageSize)}>Próxima<Icon name="chevron" /></button></nav></div>
      </section>}
    </main>
    <footer className="site-footer"><div><span><strong>Banco KRT</strong> • Gestão de contas</span><span className="local-label"><span className="status-dot" />Ambiente local de demonstração</span></div></footer>

    {formOpen && permissions.canWrite && <Overlay kind="drawer" title={editing ? "Editar conta" : "Abrir nova conta"} subtitle={editing?.holderName ?? "Cadastro de um novo titular"} mode={editing ? "Edição de conta" : "Abertura de conta"} busy={busy} onClose={closeOverlay}><AccountForm editing={editing} draft={draft} setDraft={setDraft} busy={busy} error={error} onSave={save} onClose={closeOverlay} /></Overlay>}

    {assignment && permissions.canAssign && <Overlay kind="drawer" title={assignment.kind === "portfolio" ? "Transferir carteira" : "Vincular titular"} subtitle={assignment.name} mode="Administração de acesso" busy={busy} onClose={closeOverlay}>
      <form className="overlay-form" onSubmit={event => {
        event.preventDefault();
        void write("PUT", `${base}/${assignment.id}/${assignment.kind}`, { subject }, () => { closeOverlay(); setNotice(assignment.kind === "portfolio" ? "Carteira atualizada." : "Titular vinculado."); });
      }}><div className="overlay-body"><fieldset className="form-fields" disabled={busy}>
        <div className="form-section-title"><Icon name={assignment.kind === "portfolio" ? "transfer" : "user"} /><div><h3>{assignment.kind === "portfolio" ? "Carteira do gerente" : "Acesso do titular"}</h3><p>{assignment.kind === "portfolio" ? "Defina o gerente responsável por esta conta." : "Vincule a conta ao usuário que poderá consultá-la."}</p></div></div>
        <div className="record-id"><span className="section-label">Conta selecionada</span><strong>{assignment.name}</strong><code>{assignment.id}</code></div>
        <div className="form-field"><label htmlFor="subject">ID no Keycloak do {assignment.kind === "portfolio" ? "gerente" : "titular"}</label><input id="subject" required pattern="[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}" placeholder="00000000-0000-0000-0000-000000000000" value={subject} onChange={event => setSubject(event.target.value)} aria-describedby="subject-help" data-initial-focus /><p id="subject-help" className="field-hint">Informe o identificador do usuário com o perfil correspondente.</p></div>
        <div className="feedback info"><Icon name="shield" /><div><strong>Permissão exclusiva do Operador</strong><p>{assignment.kind === "portfolio" ? "Ao transferir a conta, o gerente anterior perde acesso e o novo gerente passa a gerenciá-la." : "O cliente vinculado poderá consultar esta conta. As alterações continuam restritas aos perfis autorizados."}</p></div></div>
        {error && <div className="feedback error" role="alert"><Icon name="warning" /><p>{error}</p></div>}
      </fieldset></div><div className="overlay-footer actions"><button type="button" disabled={busy} onClick={closeOverlay}>Cancelar</button><button className="primary" type="submit" disabled={busy}><Icon name="check" />{busy ? "Salvando…" : "Confirmar vínculo"}</button></div></form>
    </Overlay>}

    {deleting && permissions.canWrite && <Overlay kind="danger" title={`Excluir a conta de ${deleting.holderName}?`} busy={busy} onClose={closeOverlay}>
      <div className="overlay-body"><p className="deletion-description">Confira os dados antes de confirmar. A conta deixará de aparecer na lista e esta ação não pode ser desfeita pela aplicação.</p><dl className="account-summary"><div><dt>CPF do titular</dt><dd><code className="cpf">{deleting.cpf}</code></dd></div><div><dt>Status operacional</dt><dd><span className={`badge ${deleting.status}`}><span className="status-dot" />{deleting.status === "active" ? "Ativa" : "Inativa"}</span></dd></div><div className="summary-id"><dt>Identificador da conta</dt><dd><code>{deleting.id}</code></dd></div></dl><div className="feedback warning"><Icon name="info" /><p>Se deseja apenas suspender a conta, cancele e altere o status para <strong>Inativa</strong> na edição.</p></div>{error && <div className="feedback error" role="alert"><Icon name="warning" /><p>{error}</p></div>}</div>
      <div className="overlay-footer actions"><button type="button" disabled={busy} onClick={closeOverlay} data-initial-focus>Cancelar</button><button type="button" className="destructive" disabled={busy} onClick={() => void write("DELETE", `${base}/${deleting.id}`, null, () => { closeOverlay(); setNotice("Conta excluída."); requestAnimationFrame(() => refreshButton.current?.focus()); }, deleting.version)}><Icon name="trash" />{busy ? "Excluindo…" : "Confirmar exclusão"}</button></div>
    </Overlay>}
  </>;
}
