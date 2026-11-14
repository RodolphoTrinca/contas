import type { Account } from "../lib/accounts";
import { Icon } from "./Icon";

export function AccountTable({ items, canWrite, canAssign, busy, onEdit, onDelete, onAssign }: {
  items: Account[]; canWrite: boolean; canAssign: boolean; busy: boolean;
  onEdit: (account: Account) => void; onDelete: (account: Account) => void;
  onAssign: (account: Account, kind: "portfolio" | "owner") => void;
}) {
  return <table aria-label="Contas cadastradas" role="table">
    <thead role="rowgroup"><tr role="row"><th scope="col" role="columnheader">Titular da conta</th><th scope="col" role="columnheader">Identificador (CPF)</th><th scope="col" role="columnheader">Status operacional</th>{canWrite && <th scope="col" role="columnheader">Ações</th>}</tr></thead>
    <tbody role="rowgroup">{items.map(account => <tr key={account.id} role="row">
      <td className="holder-cell" role="cell"><div className="holder"><span className={`avatar ${account.status}`} aria-hidden="true">{account.holderName.trim().split(/\s+/).slice(0, 2).map(word => Array.from(word)[0]).join("").toLocaleUpperCase("pt-BR")}</span><div><strong>{account.holderName}</strong><span className="holder-detail">Conta cadastrada</span></div></div></td>
      <td className="cpf-cell" role="cell"><span className="mobile-label">CPF do titular</span><code className="cpf">{account.cpf}</code></td>
      <td className="status-cell" role="cell"><span className={`badge ${account.status}`}><span className="status-dot" />{account.status === "active" ? "Ativa" : "Inativa"}</span></td>
      {canWrite && <td className="row-actions" role="cell"><div className="account-actions"><div className="actions main-row-actions">
        <button className="compact edit-button" disabled={busy} onClick={() => onEdit(account)} aria-label={`Editar conta de ${account.holderName}`}><Icon name="edit" />Editar</button>
        <button className="compact danger delete-button" disabled={busy} onClick={() => onDelete(account)} aria-label={`Excluir conta de ${account.holderName}`}><Icon name="trash" /><span>Excluir</span></button>
      </div>{canAssign && <div className="actions assignment-actions"><button className="text-button" disabled={busy} onClick={() => onAssign(account, "portfolio")}><Icon name="transfer" />Transferir carteira</button><button className="text-button" disabled={busy} onClick={() => onAssign(account, "owner")}><Icon name="user" />Vincular titular</button></div>}</div></td>}
    </tr>)}</tbody>
  </table>;
}
