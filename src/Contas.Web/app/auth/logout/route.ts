import { NextRequest, NextResponse } from "next/server";
import { cookieName } from "../../../lib/server/oidc";
import { authOptions } from "../../../lib/server/server-options";

export function POST(request: NextRequest) {
  if (request.headers.get("origin") !== authOptions.origin) return new Response(null, { status: 403 });
  const logout = new URL(authOptions.issuer + "/protocol/openid-connect/logout");
  logout.search = new URLSearchParams({ client_id: "contas-web", post_logout_redirect_uri: authOptions.origin + "/" }).toString();
  const response = NextResponse.redirect(logout, 303);
  response.cookies.delete(cookieName);
  return response;
}
