import { NextResponse } from "next/server";
import { challenge, cookieOptions, flowCookie, random, redirectUri } from "../../../lib/server/oidc";
import { authOptions } from "../../../lib/server/server-options";

export function GET() {
  const verifier = random();
  const state = random();
  const url = new URL(authOptions.issuer + "/protocol/openid-connect/auth");
  url.search = new URLSearchParams({
    client_id: "contas-web", redirect_uri: redirectUri, response_type: "code", scope: "openid",
    state, code_challenge: challenge(verifier), code_challenge_method: "S256",
  }).toString();
  const response = NextResponse.redirect(url);
  response.cookies.set(flowCookie, state + "." + verifier, { ...cookieOptions, maxAge: 300 });
  return response;
}
