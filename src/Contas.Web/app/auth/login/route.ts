import { NextResponse } from "next/server";
import { challenge, cookieOptions, flowCookie, issuer, random, redirectUri } from "../../../lib/oidc";

export function GET() {
  const verifier = random();
  const state = random();
  const url = new URL(issuer + "/protocol/openid-connect/auth");
  url.search = new URLSearchParams({
    client_id: "contas-web", redirect_uri: redirectUri, response_type: "code", scope: "openid",
    state, code_challenge: challenge(verifier), code_challenge_method: "S256",
  }).toString();
  const response = NextResponse.redirect(url);
  response.cookies.set(flowCookie, state + "." + verifier, { ...cookieOptions, maxAge: 300 });
  return response;
}
