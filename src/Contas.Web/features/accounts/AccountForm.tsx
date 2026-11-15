import { useState, type FormEvent } from "react";
import { type Account, type Draft, formatCpf, isValidCpf } from "./accounts";
import { Icon } from "../../components/ui/Icon";

export function AccountForm({ editing, draft, setDraft, busy, error, onSave, onClose }: {
  editing: Account | null; draft: Draft; setDraft: (draft: Draft) => void; busy: boolean; error: string;
  onSave: (event: FormEvent<HTMLFormElement>) => void; onClose: () => void;
}) {
  const [cpfTouched, setCpfTouched] = useState(false);
  const cpfValid = isValidCpf(draft.cpf);
  const cpfInvalid = !editing && cpfTouched && !cpfValid;
  return <form className="overlay-form" onSubmit={event => {
    if (!editing && !cpfValid) { event.preventDefault(); setCpfTouched(true); event.currentTarget.querySelector<HTMLInputElement>("#cpf")?.focus(); return; }
    onSave(event);
  }}>
    <div className="overlay-body"><fieldset className="form-fields" disabled={busy}>
      <div className="form-section-title"><Icon name="user" /><div><h3>Dados do titular</h3><p>{editing ? "Atualize os dados e a situação da conta." : "Preencha os dados para abrir uma nova conta."}</p></div></div>
      {editing && <div className="record-id"><span className="section-label">Identificador da conta</span><code>{editing.id}</code></div>}
      <div className="form-field">
        <div className="field-heading"><label htmlFor="holder">Nome do titular <span className="required" aria-hidden="true">*</span></label><span className="character-count" aria-hidden="true">{draft.holderName.length}/120</span></div>
        <input id="holder" placeholder="Ex.: Marina Duarte" value={draft.holderName} onChange={e => setDraft({ ...draft, holderName: e.target.value })} required minLength={2} maxLength={120} aria-describedby="holder-help" data-initial-focus />
        <p id="holder-help" className="field-hint">Mínimo de 2 e máximo de 120 caracteres.</p>
      </div>
      <div className="form-field">
        <div className="field-heading"><label htmlFor="cpf">CPF</label>{editing ? <span className="field-hint"><Icon name="lock" />Não editável</span> : cpfValid && <span className="valid-label"><Icon name="check" />Válido</span>}</div>
        <div className="input-with-icon"><input id="cpf" inputMode="numeric" autoComplete="off" placeholder="000.000.000-00" maxLength={14} required={!editing} readOnly={!!editing} value={editing?.cpf ?? draft.cpf}
          aria-invalid={cpfInvalid || undefined} aria-describedby="cpf-help" onBlur={() => setCpfTouched(true)} onChange={e => setDraft({ ...draft, cpf: formatCpf(e.target.value) })} /><Icon name={editing ? "lock" : "document"} /></div>
        <p id="cpf-help" className={`field-hint ${cpfInvalid ? "invalid-hint" : ""}`} role={cpfInvalid ? "alert" : undefined}>{editing ? "O CPF é o identificador do titular e não pode ser alterado." : cpfInvalid ? "Informe um CPF com 11 dígitos válidos." : "Digite os números. A pontuação é aplicada automaticamente."}</p>
      </div>
      {editing && <fieldset className="status-field"><legend>Status operacional</legend><div className="status-options">
        <label className={draft.status === "active" ? "selected active" : ""}><input type="radio" name="status" value="active" checked={draft.status === "active"} onChange={() => setDraft({ ...draft, status: "active" })} /><span className="status-dot" />Ativa{draft.status === "active" && <Icon name="check" />}</label>
        <label className={draft.status === "inactive" ? "selected inactive" : ""}><input type="radio" name="status" value="inactive" checked={draft.status === "inactive"} onChange={() => setDraft({ ...draft, status: "inactive" })} /><span className="status-dot" />Inativa{draft.status === "inactive" && <Icon name="check" />}</label>
      </div></fieldset>}
      <div className="feedback info"><Icon name="shield" /><div><strong>Regra operacional</strong><p>{editing ? "A alteração de status mantém a conta cadastrada. Você pode reativá-la quando necessário." : <>Novas contas começam com status <strong>Ativa</strong>. Confira os dados do titular antes de confirmar.</>}</p></div></div>
      {error && <div className="feedback error" role="alert"><Icon name="warning" /><p>{error}</p></div>}
    </fieldset></div>
    <div className="overlay-footer actions"><button type="button" disabled={busy} onClick={onClose}>Cancelar</button><button className="primary" type="submit" disabled={busy}><Icon name={editing ? "check" : "plus"} />{busy ? "Salvando…" : editing ? "Salvar alterações" : "Abrir conta"}</button></div>
  </form>;
}
