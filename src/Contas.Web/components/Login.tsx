import { Brand } from "./Brand";

export function Login({ reason }: { reason: "required" | "expired" | "failed" }) {
  return (
    <main className="login-shell">
      <div className="login-card">
        <div className="login-identity">
          <Brand />
          <div><p className="eyebrow">Portal de contas</p><p className="login-wordmark">Gestão de contas</p></div>
        </div>
        <section className="login-body" aria-labelledby="login-title">
          <h1 id="login-title">Entrar no sistema</h1>
          <p>Use sua credencial para acessar as contas disponíveis para o seu perfil.</p>
          {reason !== "required" && <p className={`feedback ${reason === "failed" ? "error" : "warning"}`} role="alert">
            {reason === "failed" ? "Não foi possível entrar. Tente novamente." : "Sua sessão expirou. Entre novamente para continuar."}
          </p>}
          <a className="button primary" href="/auth/login">Entrar com Keycloak <span aria-hidden="true">→</span></a>
        </section>
      </div>
    </main>
  );
}
