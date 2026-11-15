import { createHash, randomBytes } from "node:crypto";

import { authOptions } from "./server-options";

export const redirectUri = authOptions.origin + "/auth/callback";
export const cookieName = "contas_access";
export const flowCookie = "contas_flow";
export const cookieOptions = { httpOnly: true, sameSite: "lax", path: "/", secure: authOptions.origin.startsWith("https:") } as const;
export const random = () => randomBytes(32).toString("base64url");
export const challenge = (verifier: string) => createHash("sha256").update(verifier).digest("base64url");
