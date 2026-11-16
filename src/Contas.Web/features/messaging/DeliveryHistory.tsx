"use client";

import { useEffect, useRef, useState } from "react";
import { Brand } from "../../components/ui/Brand";
import { Icon } from "../../components/ui/Icon";
import { ThemeSwitcher } from "../../components/ui/ThemeSwitcher";
import { DeliveryMessage, type Delivery } from "./DeliveryMessage";

type Consumer = "fraud" | "cards";
type Query = { consumer: Consumer; cursors: (number | null)[] };
type Page = { items: Delivery[]; nextBefore: number | null };

const refreshIntervalMs = 15_000;
const requestTimeoutMs = 10_000;

export function DeliveryHistory() {
  const [query, setQuery] = useState<Query>({ consumer: "fraud", cursors: [null] });
  const [data, setData] = useState<Page | null>(null);
  const [error, setError] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const refresh = useRef<() => void>(() => {});

  function navigate(next: Query) {
    setData(null);
    setError(false);
    setQuery(next);
  }

  useEffect(() => {
    const controller = new AbortController();
    const before = query.cursors.at(-1);
    const url = `/${query.consumer}/messages${before == null ? "" : `?before=${before}`}`;
    let busy = false;

    async function load() {
      if (busy) return;
      busy = true;
      setRefreshing(true);
      try {
        const response = await fetch(url, {
          cache: "no-store",
          signal: AbortSignal.any([controller.signal, AbortSignal.timeout(requestTimeoutMs)]),
        });
        if (!response.ok) {
          if (!controller.signal.aborted) setError(true);
          return;
        }
        const page: Page = await response.json();
        if (!controller.signal.aborted) {
          setData(page);
          setError(false);
        }
      } catch {
        if (!controller.signal.aborted) setError(true);
      } finally {
        busy = false;
        if (!controller.signal.aborted) setRefreshing(false);
      }
    }

    refresh.current = () => { void load(); };
    refresh.current();
    const timer = setInterval(refresh.current, refreshIntervalMs);
    return () => {
      clearInterval(timer);
      controller.abort();
    };
  }, [query]);

  return <>
    <header className="topbar">
      <div className="topbar-inner">
        <div className="header-identity"><Brand /></div>
        <ThemeSwitcher />
      </div>
    </header>
    <main className="workspace history">
      <div className="page-intro">
        <div>
          <p className="eyebrow">Mensageria</p>
          <h1>Histórico de entregas</h1>
          <p className="intro-description">Confira os eventos recebidos e processados por Fraude e Cartões.</p>
        </div>
      </div>
      <section className="panel history-panel" aria-label="Histórico por consumidor">
        <div className="history-filter delivery-filter">
          <label htmlFor="consumer">Consumidor
            <select id="consumer" value={query.consumer}
              onChange={event => navigate({ consumer: event.target.value as Consumer, cursors: [null] })}>
              <option value="fraud">Fraude</option>
              <option value="cards">Cartões</option>
            </select>
          </label>
          <button type="button" disabled={refreshing} onClick={() => refresh.current()}>
            <Icon name="refresh" />Atualizar
          </button>
        </div>
        {!data && !error && <p className="history-message" role="status">Carregando mensagens…</p>}
        {error && <div className="feedback error history-message" role="alert">
          {data ? "Falha ao atualizar. Exibindo os últimos dados recebidos." : "Histórico indisponível. Tente atualizar."}
        </div>}
        {data?.items.length === 0 && <p className="history-message" role="status">Nenhuma mensagem recebida.</p>}
        {data && data.items.length > 0 && <ul className="history-list delivery-list" aria-label="Mensagens">
          {data.items.map(item => <DeliveryMessage key={item.eventId} item={item} />)}
        </ul>}
        {data && (data.nextBefore !== null || query.cursors.length > 1) && <div className="panel-footer">
          <span>Página {query.cursors.length} · {data.items.length} mensagens</span>
          <div className="actions">
            <button type="button" disabled={query.cursors.length === 1}
              onClick={() => navigate({ ...query, cursors: query.cursors.slice(0, -1) })}>Voltar</button>
            <button type="button" disabled={data.nextBefore === null}
              onClick={() => navigate({ ...query, cursors: [...query.cursors, data.nextBefore] })}>Ver mais antigas</button>
          </div>
        </div>}
      </section>
    </main>
  </>;
}
