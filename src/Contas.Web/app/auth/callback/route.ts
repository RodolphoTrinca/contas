import { NextRequest, NextResponse } from "next/server";
import { cookieName, cookieOptions, flowCookie, internal, origin, redirectUri } from "../../../lib/oidc";

export async function GET(request: NextRequest) {
  const [state, verifier] = request.cookies.get(flowCookie)?.value.split(".") ?? [];
  const code = request.nextUrl.searchParams.get("code");
  const response = NextResponse.redirect(origin + "/?login=failed");
  response.cookies.delete(flowCookie);
  if (!state || !verifier || state !== request.nextUrl.searchParams.get("state") || !code)
    return response;
  try {
    const tokenResponse = await fetch(internal + "/protocol/openid-connect/token", {
      method: "POST",
      body: new URLSearchParams({
        grant_type: "authorization_code", client_id: "contas-web", code, code_verifier: verifier, redirect_uri: redirectUri,
      }),
      cache: "no-store",
      signal: AbortSignal.timeout(8000),
    });
    if (!tokenResponse.ok) return response;
    const token = await tokenResponse.json();
    if (typeof token.access_token !== "string" || typeof token.expires_in !== "number") return response;
    response.headers.set("Location", origin + "/");
    response.cookies.set(cookieName, token.access_token, { ...cookieOptions, maxAge: token.expires_in });
  } catch {
    return response;
  }
  return response;
}
