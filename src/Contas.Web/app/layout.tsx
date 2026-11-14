import type { Metadata } from "next";
import "./style.css";

export const metadata: Metadata = { title: "Contas", description: "Gestão de contas de clientes" };
export default function Layout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="pt-BR"><body>{children}</body></html>;
}
