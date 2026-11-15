import "server-only";

type AuthOptions = Readonly<{
  origin: string;
  issuer: string;
  internalIssuer: string;
}>;

type AccountsApiOptions = Readonly<{ baseUrl: string }>;

const origin = process.env.APP_ORIGIN ?? "http://localhost:8080";
const issuer = process.env.OIDC_ISSUER ?? origin + "/auth/realms/contas";

export const authOptions: AuthOptions = {
  origin,
  issuer,
  internalIssuer: process.env.OIDC_INTERNAL ?? issuer,
};

export const accountsApiOptions: AccountsApiOptions = {
  baseUrl: process.env.API_INTERNAL ?? "http://127.0.0.1:5080",
};
