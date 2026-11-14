import type { Metadata } from "next";
import "./style.css";

export const metadata: Metadata = { title: "Banco KRT | Gestão de contas", icons: { icon: "/brand/logo.png" }, description: "Gestão de contas de clientes" };
export default function Layout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="pt-BR"><head><link rel="stylesheet" href="/brand/tokens.css" /></head><body>{children}</body></html>;
}
