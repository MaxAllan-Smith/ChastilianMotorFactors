# Authentication

`ChastilianMotorFactors.Api` is two things in one app:

- **An authorization server**: users sign in on its Razor Pages, and client apps (SPA, mobile, desktop) get tokens from it using OpenID Connect.
- **A resource server**: its API controllers accept the access tokens it issued.

It is built on **ASP.NET Core Identity**, which stores accounts and passwords, and **OpenIddict 7**, which implements the OAuth 2.0 / OpenID Connect protocol.

## The flow

The only supported flow is **authorization code + PKCE**, for public clients, which have no client secret.

```
Client app                      This server                                   
──────────                      ───────────                                   
1. redirect user ─────────────► GET /connect/authorize?client_id&redirect_uri&code_challenge&scope…
                                  not signed in? → /Account/Login (or Register) → back to /connect/authorize
2. ◄─────────────────────────── redirect to redirect_uri?code=…&state=…
3. POST /connect/token ───────► code + code_verifier
4. ◄─────────────────────────── access_token, id_token, refresh_token (with offline_access)
5. call API with  Authorization: Bearer <access_token>
6. later: POST /connect/token  grant_type=refresh_token → new tokens (refresh tokens rotate)
7. sign out: GET /connect/endsession?post_logout_redirect_uri&id_token_hint
```

Discovery document: `/.well-known/openid-configuration`. Client libraries such as `oidc-client-ts` or MSAL read it automatically.

### Scopes

| Scope | Gives |
|---|---|
| `openid` | An id token with `sub`, the user's ID |
| `profile` | `name` in the id token and userinfo |
| `email` | `email` and `email_verified` |
| `offline_access` | A refresh token |
| `api` | Access to this API: puts the audience `chastilian-motor-factors-api` into the access token. **Without it, API calls are rejected.** |

## Where the code lives

| Path | Responsibility |
|---|---|
| `Infrastructure/Identity/ApplicationUser.cs` | The user account. Add profile properties here. |
| `Infrastructure/Identity/AuthServerOptions.cs`, `AuthServerClientOptions.cs` | The `AuthServer` configuration section |
| `Infrastructure/Identity/AuthServerConstants.cs` | Endpoint paths, the API scope and the API audience |
| `Infrastructure/Identity/ClientDescriptorFactory.cs` | Turns a configured client into an OpenIddict application (public client, PKCE required, implicit consent) |
| `Infrastructure/Identity/AuthServerSeeder.cs` | At startup, creates or updates the `api` scope and the configured clients |
| `Infrastructure/DependencyInjection.cs` | Registers Identity (password and lockout rules), the OpenIddict server and validation, and certificates |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | `IdentityDbContext`; also holds the OpenIddict tables |
| `Api/Identity/AuthorizationController.cs` | The `/connect/*` endpoints: who the user is and which claims go into tokens |
| `Api/Identity/ClaimDestinations.cs` | Which claims go into the access token and which into the id token |
| `Api/Pages/Account/*` | Login, Register and Logout pages; `Api/Pages/Index` shows the sign-in status |

Auth code in `Api/Identity` and `Api/Pages/Account` uses Identity and OpenIddict types directly. This is a deliberate, auth-only exception to the "controllers use Application types only" rule (see `CLAUDE.md`).

## Common tasks

### Protect an API controller

```csharp
[ApiController]
[Route("api/vehicles")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public sealed class VehiclesController : ControllerBase { … }
```

The scheme matters. A plain `[Authorize]` uses the Identity login cookie, so an API caller without a cookie gets redirected to the login page instead of receiving `401`. The user's ID is in the `sub` claim: `User.FindFirst(OpenIddictConstants.Claims.Subject)`.

### Add a client application

Add it to `AuthServer:Clients`: in `appsettings.json` for every environment, or in Azure App Service configuration for production only.

```json
"AuthServer": {
  "Clients": [
    {
      "ClientId": "web-app",
      "DisplayName": "Chastilian web app",
      "RedirectUris": [ "https://app.example.com/callback" ],
      "PostLogoutRedirectUris": [ "https://app.example.com/" ]
    }
  ]
}
```

In Azure App Service settings, nested keys use `__`, for example `AuthServer__Clients__0__ClientId`. Clients are created or updated at the next startup. Redirect URIs must match exactly. Removing a client from configuration does **not** delete it from the database.

Development seeds a `postman` client with Postman's callback URI (`https://oauth.pstmn.io/v1/callback`). In Postman, choose OAuth 2.0 → Authorization Code (With PKCE), with these settings:
- **Auth URL:** `https://localhost:7070/connect/authorize`
- **Token URL:** `https://localhost:7070/connect/token`
- **Client ID:** `postman`
- **Scope:** `openid profile email offline_access api`

### Add a claim to tokens

1. Set it in `AuthorizationController.SetUserClaimsAsync`. It runs on sign-in and on every token refresh, so the value stays current.
2. Give it a destination in `ClaimDestinations`. Without one, the claim isn't put into any token. By default claims go into the access token only; never put secrets in tokens.
3. If clients need it, also return it from the `UserInfo` action.

## Running locally

1. Apply migrations to LocalDB: `dotnet ef database update --project ChastilianMotorFactors.Infrastructure --startup-project ChastilianMotorFactors.Api`.
2. Run with the `https` launch profile. OpenIddict rejects plain HTTP requests.
3. Signing and encryption use OpenIddict's development certificates, which are created automatically in your Windows user certificate store.

## Production checklist (Azure App Service)

Startup fails on purpose if these are missing, rather than issuing tokens that break later.

1. **Database**: `ConnectionStrings:Database` is set, and migrations have been applied to the Azure SQL database.
2. **Certificates**: two X.509 certificates with private keys (they can be self-signed), one for signing and one for encrypting tokens.
   - Upload them to the App Service (TLS/SSL settings → Private Key Certificates).
   - Set `WEBSITE_LOAD_CERTIFICATES` to their thumbprints.
   - Set `AuthServer:SigningCertificateThumbprint` and `AuthServer:EncryptionCertificateThumbprint`.
   - Renew them before they expire. Tokens signed with a removed certificate stop validating.
   - This loads certificates from the Windows certificate store. **On a Linux App Service plan, certificates are files**, and the loading code in `DependencyInjection.AddCertificates` needs changing.
3. **HTTPS behind the proxy**: on Linux App Service, set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so the app sees requests as HTTPS. Without it, OpenIddict rejects them. Windows App Service doesn't need this.
4. **Clients**: production clients are configured under `AuthServer:Clients`.

## Security behaviour (verified locally end to end)

- **PKCE:** required for every authorization request. A wrong `code_verifier` is rejected.
- **Authorization codes:** single-use. Replaying a code also revokes the tokens issued from it.
- **Redirect URIs:** must match a registered URI exactly.
- **Login page:**
  - It only redirects to local URLs (no open redirects).
  - Every form needs an antiforgery token.
  - Error messages don't reveal whether an email exists.
  - Accounts lock after repeated failures (Identity default: 5 attempts, 5 minutes).
- **Token refresh** re-checks the account; deleted or locked-out users get `invalid_grant`. Refresh tokens rotate.
- **Access tokens** are encrypted (JWE), and the API only accepts those with the `api` audience.

## Not implemented yet

- **Email confirmation, password reset and 2FA**: they need an email sender. `RequireConfirmedAccount` is `false`, so `email_verified` is always `false`.
- **Consent screen**: every client is first-party with implicit consent. A client set to explicit consent gets `consent_required`.
- **Roles and admin account management**: the Identity role tables exist but are unused.
- **Other grant types**: client credentials and others aren't enabled.
