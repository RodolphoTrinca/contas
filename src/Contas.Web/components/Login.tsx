import Image from "next/image";
import { Icon } from "./Icon";

export function Login({ reason }: { reason: "required" | "expired" | "failed" }) {
  return <main className="login-shell">
    <div className="login-card">
      <aside className="login-identity">
        <Image className="login-logo" src="/brand/logo.png" width={144} height={80} alt="Banco KRT" unoptimized />
        <div className="login-presentation">
          <p className="eyebrow"><Icon name="shield" /> Portal de contas</p>
          <h2>Sistema Operacional Bancário</h2>
          <p>Gestão de contas e carteiras em um só lugar.</p>
        </div>
        <div className="identity-note"><Icon name="shield" /><div><strong>Acesso por perfil</strong><p>Uma experiência para cada responsabilidade: Operador, Gerente e Cliente.</p></div></div>
      </aside>
      <section className="login-body" aria-labelledby="login-title">
        <div className="login-heading"><h1 id="login-title">Entrar no sistema</h1><span className="context-chip">SSO<br />Corporativo</span></div>
        <p>Use sua credencial para acessar as contas disponíveis para o seu perfil.</p>
        <a className="button primary login-action" href="/auth/login"><Icon name="key" />Entrar com Keycloak<Icon name="arrow" /></a>
        <p className="login-assurance"><Icon name="lock" />Acesso centralizado com sua identidade Banco KRT.</p>
        {reason !== "required" && <div className="login-alerts"><p className="section-label">Avisos do sistema</p><div className={`feedback ${reason === "failed" ? "error" : "warning"}`} role="alert">
          <Icon name={reason === "failed" ? "warning" : "lock"} /><div><strong>{reason === "failed" ? "Falha de autenticação" : "Sessão interrompida"}</strong><p>{reason === "failed" ? "Não foi possível entrar. Tente novamente." : "Sua sessão expirou. Entre novamente para continuar."}</p></div>
        </div></div>}
        <div className="login-bottom"><span>Banco KRT <span className="text-muted">• Gestão de contas</span></span><span className="local-label">Ambiente local</span></div>
      </section>
    </div>
    <p className="login-caption">Banco fictício • Ambiente de demonstração</p>
  </main>;
}
