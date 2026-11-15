"use client";

import { useEffect, useState } from "react";
import { Brand } from "../../components/ui/Brand";
import { Login } from "../../components/auth/Login";
import { ThemeSwitcher } from "../../components/ui/ThemeSwitcher";

type Change = { id: string; accountId: string; action: string; actor: string; occurredAt: string };
type HistoryPage = { items: Change[]; nextBefore: string | null };
type Query = { accountId: string; cursors: (string | null)[] };

const labels: Record<string, string> = {
  AccountCreated: "Abertura de conta",
  AccountUpdated: "Edição de conta",
  AccountDeleted: "Exclusão de conta",
  PortfolioAssigned: "Atribuição de carteira",
  OwnerLinked: "Vínculo de titular",
};
const dateFormat = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "medium" });

export default function AccountHistory() {
  const [filter, setFilter] = useState("");
  const [query, setQuery] = useState<Query>({ accountId: "", cursors: [null] });
  const [data, setData] = useState<HistoryPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState(0);
  const [error, setError] = useState("");

  function load(next: Query) {
    setLoading(true);
    setData(null);
    setError("");
    setQuery(next);
  }

  useEffect(() => {
    const controller = new AbortController();
    const params = new URLSearchParams();
    const before = query.cursors.at(-1);
    if (before) params.set("before", before);
    if (query.accountId) params.set("accountId", query.accountId);

    async function fetchHistory() {
      try {
        const response = await fetch(`/backend/history?${params}`, {
          cache: "no-store",
          signal: controller.signal,
        });
        const result = await response.json();
        if (controller.signal.aborted) return;
        setStatus(response.status);
        if (response.ok) setData(result.data);
        else setError(result.error?.message ?? "Não foi possível consultar o histórico.");
      } catch {
        if (!controller.signal.aborted) setError("Não foi possível consultar o histórico.");
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }
    void fetchHistory();
    return () => controller.abort();
  }, [query]);

  if (status === 401) return <Login reason="required" />;

  return <>
    <header className="topbar">
      <div className="topbar-inner">
        <Brand />
        <div className="header-session">
          <ThemeSwitcher />
          <form method="post" action="/auth/logout"><button className="logout">Sair</button></form>
        </div>
      </div>
    </header>
    <main className="workspace history">
      <nav className="workspace-nav" aria-label="Área de trabalho">
        <a href="/">Contas</a>
        {status === 200 && <a href="/historico" aria-current="page">Histórico de alterações</a>}
      </nav>
      <h1>Histórico de alterações</h1>
      {status !== 403 && <p className="text-muted">Consulte mudanças em todas as contas, inclusive nas excluídas.</p>}
      <section className="panel history-panel" aria-label="Registros de auditoria" aria-busy={loading}>
        {status !== 0 && status !== 403 && <form className="history-filter" onSubmit={event => {
          event.preventDefault();
          load({ accountId: filter.trim(), cursors: [null] });
        }}>
          <label htmlFor="history-account">ID da conta
            <input id="history-account" value={filter} onChange={event => setFilter(event.target.value)}
              placeholder="UUID da conta" />
          </label>
          <button type="submit">Filtrar</button>
          <button type="button" onClick={() => {
            setFilter("");
            load({ accountId: "", cursors: [null] });
          }}>Todas as contas</button>
        </form>}
        {loading && <p className="history-message" role="status">Carregando histórico…</p>}
        {error && <div className="feedback error history-message" role="alert">
          {status === 403 ? "O histórico está disponível apenas para o Operador." : error}
          {status >= 500 || status === 0 ? <button onClick={() => load({ ...query })}>Tentar novamente</button> : null}
        </div>}
        {data?.items.length === 0 && <p className="history-message" role="status">Nenhuma alteração registrada para esta consulta.</p>}
        {data && data.items.length > 0 && <ul className="history-list" aria-label="Alterações">
          {data.items.map(change => <li key={change.id}>
            <strong>{labels[change.action] ?? change.action}</strong>
            <time dateTime={change.occurredAt}>{dateFormat.format(new Date(change.occurredAt))}</time>
            <dl>
              <div><dt>ID da conta</dt><dd><code>{change.accountId}</code></dd></div>
              <div><dt>Autor</dt><dd><code>{change.actor === "system:seed" ? "Sistema (seed)" : change.actor}</code></dd></div>
            </dl>
          </li>)}
        </ul>}
        {data && (data.nextBefore || query.cursors.length > 1) && <div className="panel-footer">
          <span>Página {query.cursors.length}</span>
          <div className="actions">
            <button disabled={query.cursors.length === 1} onClick={() =>
              load({ ...query, cursors: query.cursors.slice(0, -1) })}>Anterior</button>
            <button disabled={!data.nextBefore} onClick={() =>
              load({ ...query, cursors: [...query.cursors, data.nextBefore] })}>Próxima</button>
          </div>
        </div>}
      </section>
    </main>
  </>;
}
