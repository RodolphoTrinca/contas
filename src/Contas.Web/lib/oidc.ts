import { createHash, randomBytes } from "node:crypto";

export const origin = process.env.APP_ORIGIN ?? "http://localhost:8080";
export const issuer = process.env.OIDC_ISSUER ?? origin + "/auth/realms/contas";
export const internal = process.env.OIDC_INTERNAL ?? issuer;
export const api = process.env.API_INTERNAL ?? "http://127.0.0.1:5080";
export const redirectUri = origin + "/auth/callback";
export const cookieName = "contas_access";
export const flowCookie = "contas_flow";
export const cookieOptions = { httpOnly: true, sameSite: "lax", path: "/", secure: origin.startsWith("https:") } as const;
export const random = () => randomBytes(32).toString("base64url");
export const challenge = (verifier: string) => createHash("sha256").update(verifier).digest("base64url");
