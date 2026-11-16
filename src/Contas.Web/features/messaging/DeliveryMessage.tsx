export type Delivery = {
  eventId: string;
  accountId: string;
  eventType: string;
  version: number;
  occurredAt: string;
  receivedAt: string;
  processedAt: string | null;
  outcome: string | null;
  payload: string | null;
};

const labels: Record<string, string> = {
  AccountCreated: "Conta criada",
  AccountUpdated: "Conta atualizada",
  AccountDeleted: "Conta excluída",
};
const date = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "medium" });

function messageBody(item: Delivery) {
  if (item.payload) {
    try { return JSON.stringify(JSON.parse(item.payload), null, 2); }
    catch { return item.payload; }
  }

  const { eventId, accountId, eventType, version, occurredAt } = item;
  return JSON.stringify({ eventId, accountId, eventType, version, occurredAt }, null, 2);
}

export function DeliveryMessage({ item }: { item: Delivery }) {
  return <li>
    <strong>{labels[item.eventType] ?? item.eventType}</strong>
    <span className={`badge ${item.processedAt ? "active" : "inactive"}`}>
      <span className="status-dot" />{item.processedAt ? "Processada" : "Recebida"}
    </span>
    <dl>
      <div><dt>Evento</dt><dd><code>{item.eventId}</code></dd></div>
      <div><dt>ID da conta</dt><dd><code>{item.accountId}</code></dd></div>
      <div><dt>Versão</dt><dd>{item.version}</dd></div>
      <div><dt>Recebida</dt><dd><time dateTime={item.receivedAt}>{date.format(new Date(item.receivedAt))}</time></dd></div>
      <div><dt>Processada</dt><dd>{item.processedAt
        ? <time dateTime={item.processedAt}>{date.format(new Date(item.processedAt))}</time>
        : "Aguardando"}</dd></div>
      <div><dt>Resultado</dt><dd>{item.outcome ?? "Aguardando processamento"}</dd></div>
    </dl>
    <details className="delivery-payload">
      <summary>Ver mensagem</summary>
      {!item.payload && <p className="text-muted">O JSON original não foi armazenado. Veja os campos registrados abaixo.</p>}
      <pre>{messageBody(item)}</pre>
    </details>
  </li>;
}
