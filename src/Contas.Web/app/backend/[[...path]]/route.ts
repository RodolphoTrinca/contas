import { NextRequest, NextResponse } from "next/server";
import { cookieName } from "../../../lib/server/oidc";
import { accountsApiOptions, authOptions } from "../../../lib/server/server-options";

type Context = { params: Promise<{ path?: string[] }> };

async function forward(request: NextRequest, context: Context) {
  const token = request.cookies.get(cookieName)?.value;
  if (!token) return fail(401, "auth.required", "Entre para acessar as contas.");
  if (request.method !== "GET" && request.headers.get("origin") !== authOptions.origin)
    return fail(403, "auth.origin_forbidden", "Origem não autorizada.");
  const path = (await context.params).path ?? [];
  const id = /^[a-f0-9-]{36}$/i;
  const allowed = path.length === 0 ||
    (path.length === 1 && (id.test(path[0]) || (["permissions", "history"].includes(path[0]) && request.method === "GET"))) ||
    (path.length === 2 && request.method === "PUT" && id.test(path[0]) && ["portfolio", "owner"].includes(path[1]));
  if (!allowed) return fail(404, "request.not_found", "Rota não encontrada.");
  const target = accountsApiOptions.baseUrl + "/api/v1/accounts" + (path.length ? "/" + path.join("/") : "") + request.nextUrl.search;
  const headers = new Headers({ Authorization: "Bearer " + token });
  for (const name of ["content-type", "idempotency-key"]) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }
  try {
    const result = await fetch(target, {
      method: request.method, headers,
      body: request.method === "GET" || request.method === "DELETE" ? undefined : await request.text(),
      cache: "no-store", signal: AbortSignal.timeout(10000),
    });
    const output = new Headers({ "Cache-Control": "no-store" });
    for (const name of ["content-type", "location", "idempotency-replayed"]) {
      const value = result.headers.get(name);
      if (value) output.set(name, value);
    }
    return new Response(result.body, { status: result.status, headers: output });
  } catch {
    return fail(503, "service.unavailable", "Serviço temporariamente indisponível.");
  }
}

function fail(status: number, code: string, message: string) {
  return NextResponse.json({ data: null, error: { code, message }, traceId: crypto.randomUUID() },
    { status, headers: { "Cache-Control": "no-store" } });
}

export { forward as GET, forward as POST, forward as PUT, forward as DELETE };
