import { NextRequest, NextResponse } from "next/server";
import { cookieName, issuer, origin } from "../../../lib/oidc";

export function POST(request: NextRequest) {
  if (request.headers.get("origin") !== origin) return new Response(null, { status: 403 });
  const logout = new URL(issuer + "/protocol/openid-connect/logout");
  logout.search = new URLSearchParams({ client_id: "contas-web", post_logout_redirect_uri: origin + "/" }).toString();
  const response = NextResponse.redirect(logout, 303);
  response.cookies.delete(cookieName);
  return response;
}
