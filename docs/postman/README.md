# AstraKa Postman collection

`AstraKa.postman_collection.json` walks the whole API end to end, in Collection Runner order:
anonymous zodiac/horoscope lookups, onboarding, Exercises CRUD, building a Course day by day,
publishing it, running a Cohort (Constellation), a Wanderer enrolling, reports, then cleanup.
Every request has a `pm.test` assertion; re-running the whole collection is safe (unique names
use `{{$timestamp}}`).

You need **three real Auth0 accounts** (any email/password combination works, as long as they are
three distinct accounts): one that stays a Wanderer, one that becomes a Starshepherd, one that
becomes an Oracle. Create them by just logging in three times in the next step.

## 1. Import the collection

Postman → Import → select `AstraKa.postman_collection.json`.

## 2. Get the three access tokens

The collection's own **Authorization** tab (click the collection name → Authorization) already has
the full Auth0 "Authorization Code (With PKCE)" configuration filled in. This config exists only
to fetch tokens through Postman's UI - no request in the collection actually uses it to authenticate;
every request uses a plain Bearer token from a collection variable instead.

For each of your three accounts:

1. Open the collection's Authorization tab.
2. Click **Get New Access Token**. A browser window opens to Auth0's login page - the
   `prompt=login` parameter forces this every time, even if you were already logged in as a
   different account.
3. Log in as the account you want this token for.
4. Postman shows the fetched token. Copy the **Access Token** value.
5. Go to the collection's **Variables** tab and paste it into the **CURRENT VALUE** column (not
   "initial value") of the matching variable:
   - first account → `wandererToken`
   - second account → `starshepherdToken`
   - third account → `oracleToken`
6. Repeat from step 2 for the next account.

**Put the token only in the "current value" column, never "initial value" - then save the
collection (Ctrl+S / Cmd+S).** Saving is expected and safe here: Postman only ever writes "initial
value" into the exported/committed collection file, so pressing Ctrl+S to keep your pasted token
from disappearing on reload does not put it at risk of being committed or synced - "current value"
stays on your machine, local to your own Postman installation, and is never written to the
exported JSON or shared with anyone else, even if your workspace is otherwise a shared/team one.
The three token variables are also marked as `secret` type, which additionally keeps Postman from
displaying or syncing them in plaintext. If you ever export the collection again, double check the
exported JSON still shows empty values for `wandererToken` / `starshepherdToken` / `oracleToken`
before committing it.

Access tokens from this Auth0 tenant are short-lived (~24h) - if requests start failing with 401
after a while, just repeat step 2 for that account.

If the `audience`/`prompt` extra authorization parameters don't come through after import on your
Postman version, open the Authorization tab's advanced/additional-parameters section and add them
by hand: `audience=https://api.astraka.local`, `prompt=login`.

## 3. Onboard the Wanderer and Starshepherd accounts

Nothing extra to do here - the collection's own **"2. Me"** folder onboards both accounts for you
(`POST /me/wanderer`, `POST /me/starshepherd`) the first time you run it. Running it again is safe:
those requests accept both `201` (just onboarded) and `409` (already onboarded).

## 4. Make the third account an Oracle

Oracle is **never** created through the API - there is no "onboard as Oracle" endpoint, on purpose
(an admin-style role shouldn't be self-service). Instead, run `docs/sql/grant-oracle.sql` against
the `AstraKa` database (not `AstraKa_Tests`) with the third account's Auth0 `sub` and email filled
in at the top of the script. To find the `sub`: log in as that account once in the Collection
Runner (so it has an access token), then decode the JWT (e.g. at jwt.io) and copy its `sub` claim
(`auth0|...`), or simply call `GET /api/v1/me` with that token after running the script once with a
best guess - if the email matches but the row didn't take effect yet, re-check the `sub`.

Run the script once before using `oracleToken` in the collection.

## 5. Run the Collection Runner

Collection → **Run** (or the Runner icon) → select `AstraKa API` → Run. Keep folders in their
existing order (1 through 10) - the story depends on it: later folders use ids saved by earlier
ones.

A few things that are expected, not failures:

- "2. Me" → the two onboarding requests show `201` the first time, `409` every run after that.
- "7. Cohorts" skips the "another Starshepherd" scenario on purpose - see that folder's
  description. Only one real Starshepherd identity (`starshepherdToken`) is configured here.
- "10. Cleanup" has two requests that are *supposed* to return `409` (deleting the enrolled
  cohort, deleting the course that owns it) - the API's own business rules block those deletes,
  and that is exactly what those two requests demonstrate. The rest of cleanup (`200`/`204`)
  removes everything else this run created. Seeded reference data (zodiac signs, the seeded
  Starshepherds and courses like `HOMEWARD`) is never touched.

## Tokens are never committed

The collection file always ships with `wandererToken`, `starshepherdToken` and `oracleToken` as
empty, `secret`-typed variables. Setting them only as "current value" (then saving with Ctrl+S)
and never "initial value" is what keeps real tokens out of git - "current value" never leaves your
own Postman installation, saved or not, and never ends up in a re-exported collection file.
