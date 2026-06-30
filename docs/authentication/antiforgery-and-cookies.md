# Auth, Cookies, and Antiforgery in Alpinarc

This document explains the browser flow for the authentication and favorites feature in this branch.
It focuses on three different values that are easy to confuse:

- the auth cookie
- the antiforgery cookie
- the XSRF request token cookie

The short version is:

- the auth cookie proves who you are
- the antiforgery system proves the request came from Alpinarc's own frontend
- the XSRF token is the value the frontend copies into a header so the server can verify that proof

## The three pieces

### 1. Auth cookie

The auth cookie is the login session.

In this branch it is named `AlpinArc.Auth`.

Important cookie properties:

- `HttpOnly = true`
  - JavaScript cannot read it
  - the browser still sends it automatically
- `SameSite = Lax`
  - reduces cross-site sending in many cases
  - still allows normal same-origin app traffic
- `SecurePolicy = SameAsRequest`
  - uses `Secure` on HTTPS
  - stays usable on local HTTP development
- `Path = /` by default
  - makes the cookie available to the whole app

What it does:

- tells the API that the browser is signed in
- identifies the current user
- is sent automatically by the browser on requests to the same origin

What it is not:

- it is not a CSRF token
- it does not prove the request came from Alpinarc's frontend
- it should never be treated as a secret that only your frontend can use

### 2. Antiforgery cookie

The antiforgery cookie is part of CSRF protection.

In this branch it is named `AlpinArc.Xsrf`.

Important cookie properties:

- `HttpOnly = true`
  - JavaScript cannot read the antiforgery cookie
- `SameSite = Lax`
  - keeps normal app requests working
  - reduces cross-site leakage
- `SecurePolicy = SameAsRequest`
  - works on HTTPS in deployment
  - still works on local HTTP when developing
- `IsEssential = true` through ASP.NET Core defaults
  - the app needs this cookie for the security system to function

What it does:

- stores the server-side cookie half of the antiforgery pair
- is marked `HttpOnly`
- is not readable from JavaScript
- is used by ASP.NET Core when validating the request token

What it is not:

- it is not the login session
- it is not something the frontend reads directly
- it is not meant to be copied into application code

### 3. XSRF request token cookie

This branch also exposes a readable cookie named `XSRF-TOKEN`.

What it does:

- gives the frontend a token value it can read
- lets the frontend copy that value into the `X-XSRF-TOKEN` header
- is the browser-side piece that makes antiforgery validation possible for API requests

Important cookie properties:

- `HttpOnly = false`
  - JavaScript must be able to read it
  - otherwise the frontend could not copy it into the request header
- `SameSite = Lax`
  - keeps it aligned with the rest of the app's same-origin flow
- `Secure = Request.IsHttps`
  - sent securely in HTTPS
  - stays usable in local HTTP development
- `Path = /`
  - available to the whole app

What it is not:

- it is not the auth cookie
- it is not the same thing as the antiforgery cookie
- it does not identify the user

## Why there are two antiforgery-related cookies

The names are unfortunate, because both cookies are related to the same protection system:

- `AlpinArc.Xsrf` is the internal cookie that ASP.NET Core uses for validation
- `XSRF-TOKEN` is the readable token exposed to the frontend

The frontend can only send the token back in a header if it can read it.
That is why the request token is exposed in a non-HttpOnly cookie.

The server still keeps the real antiforgery state in the HttpOnly cookie.
The browser cannot read that one.

### Why these properties matter

These are the properties that make the system behave the way it does:

- `HttpOnly` controls whether JavaScript can read the cookie
- `SameSite` controls when the browser includes the cookie on cross-site requests
- `Secure` controls whether the cookie is restricted to HTTPS
- `Path` controls which URLs on the site receive the cookie

Without those properties, the browser would either expose too much to JavaScript or send the cookie in places we do not want.

So the server checks a pair:

- the antiforgery cookie
- the request token sent in the header

Both must line up for the request to pass.

## The complete request flow

Here is the actual flow in this branch.

### Step 1: the page needs auth-aware behavior

When the frontend loads a page like:

- `/login`
- `/register`
- `/lodges/[id]`
- `/favorites`

it may need to know whether there is an active session or it may need to perform a state-changing request.

Before it can do that safely, it needs a CSRF token pair.

### Step 2: the frontend calls `/api/auth/csrf`

The frontend requests:

`GET /api/auth/csrf`

That endpoint calls ASP.NET Core antiforgery token generation.

The API responds by setting cookies on the browser:

- `AlpinArc.Xsrf`
- `XSRF-TOKEN`

The browser stores both values.

### Step 3: the frontend reads `XSRF-TOKEN`

Because `XSRF-TOKEN` is not `HttpOnly`, browser JavaScript on Alpinarc can read it.

That is what the frontend uses when it sends a mutating request.

The frontend does not read `AlpinArc.Xsrf`.
That cookie is intentionally hidden from JavaScript.

### Step 4: the browser sends a protected request

For a request like:

- `POST /api/auth/login`
- `POST /api/auth/logout`
- `POST /api/me/lodge-favorites/5`
- `DELETE /api/me/lodge-favorites/5`

the browser sends:

- `AlpinArc.Auth` automatically, if the user is logged in
- `AlpinArc.Xsrf` automatically, because it is a cookie for the same origin
- `X-XSRF-TOKEN` explicitly in the request header, copied from `XSRF-TOKEN`

### Step 5: the server validates the request

ASP.NET Core antiforgery middleware checks that the request token in the header matches the antiforgery cookie state for that browser session.

If the pair matches:

- the request is accepted
- the controller action runs

If the pair does not match:

- the request is rejected
- the controller action does not run

## Why the malicious site cannot just use the same cookies

This is the part that usually causes confusion.

The malicious site does not get to read your cookies for Alpinarc.

It can only try to make your browser send a request.

That distinction matters.

### What a malicious site can do

If you are logged into Alpinarc and visit a malicious site, that site can try to trigger a request to Alpinarc.

Examples:

- submit a form
- navigate to a URL
- load an image or script tag that causes a browser request

If the browser decides to include cookies for Alpinarc, those cookies may go along with the request.

The malicious site itself still does not see the cookie values.

### What a malicious site cannot do

It cannot:

- read `document.cookie` for `alpinarc.local`
- read `XSRF-TOKEN` from Alpinarc
- read the response from Alpinarc unless cross-origin access is explicitly allowed
- guess the correct request token reliably

That is why antiforgery works.

The attacker may be able to cause the browser to send the auth cookie.
It cannot produce the matching request token header because it cannot read the token value from Alpinarc's origin.

## What would go wrong without antiforgery

Imagine this sequence:

1. You log into Alpinarc.
2. You open a malicious site in another tab.
3. That site submits a hidden form to `https://alpinarc.local/api/me/lodge-favorites/5`.
4. Your browser includes your Alpinarc auth cookie automatically.
5. The API sees an authenticated session.
6. The favorite is added, even though you never clicked anything on Alpinarc.

That is a CSRF attack.

The attacker is not stealing your password.
The attacker is abusing your existing browser session.

Without antiforgery, any state-changing endpoint that trusts only the auth cookie is vulnerable to this.

## What blocks the attack now

With antiforgery in place, the attacker still cannot do step 4 and complete the request.

The browser might send the auth cookie.
But the request also needs the `X-XSRF-TOKEN` header.

Only Alpinarc's own frontend can read the `XSRF-TOKEN` cookie and put that value into the header.

The malicious site cannot do that because it cannot read Alpinarc's cookies.

So the server sees:

- valid auth cookie
- missing or wrong antiforgery header

and rejects the request.

## Why same-origin matters here

This branch intentionally uses same-origin requests.

That is what lets the frontend:

- call `/api/...` directly through the Next.js proxy
- read the `XSRF-TOKEN` cookie from the same origin
- send the token back in the `X-XSRF-TOKEN` header

Same-origin is the trust boundary.

The malicious site is outside that boundary.

## One subtle point about `HttpOnly`

`HttpOnly` does not mean "the browser cannot send this cookie."

It means "JavaScript cannot read this cookie."

That is the reason `AlpinArc.Xsrf` can still participate in antiforgery validation even though the frontend cannot inspect it directly.

The browser sends it automatically, but scripts on another origin cannot steal it and reuse it.

## Concrete example from this branch

Suppose you are on a lodge detail page.

### Legitimate flow

1. You open `/lodges/5`.
2. The frontend checks your auth state.
3. If you are signed in, it fetches `/api/me/lodge-favorites/5`.
4. The page renders the favorite button.
5. You click the button.
6. The frontend sends the request with the CSRF header.
7. The API accepts the request and saves the favorite.

### Malicious flow without antiforgery

1. You are logged in to Alpinarc.
2. You visit `evil.example`.
3. That site submits a POST request to Alpinarc.
4. Your browser includes the auth cookie.
5. Alpinarc accepts the request.

### Malicious flow with antiforgery

1. You are logged in to Alpinarc.
2. You visit `evil.example`.
3. That site submits a POST request to Alpinarc.
4. Your browser may include the auth cookie.
5. The attacker still cannot supply the correct `X-XSRF-TOKEN` header.
6. Alpinarc rejects the request.

## Summary

The three values are different:

- `AlpinArc.Auth` means the browser is authenticated
- `AlpinArc.Xsrf` is the hidden antiforgery cookie the server uses to validate requests
- `XSRF-TOKEN` is the readable token the frontend copies into a header

The browser can send cookies automatically.
The malicious site cannot read Alpinarc's cookies.
That is why the attacker cannot reproduce the antiforgery header and cannot forge a valid request.
