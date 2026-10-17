# Load tests - horoscope caching, before/after

Measures the effect of the `HybridCache`-backed horoscope cache (see `docs/decisions.md` ADR #14)
using [k6](https://k6.io/). Both runs use the **fake** horoscope provider
(`Horoscope:Provider=Fake`, ~300ms artificial latency, no network call) - **never the real
third-party API** (freehoroscopeapi.com). The only thing that changes between the two runs is
`Horoscope:CacheEnabled`.

## 1. Install k6 (Windows)

```powershell
winget install k6.k6
```
(or, with Chocolatey: `choco install k6`). Confirm with `k6 version`.

## 2. Start the API with the fake provider

In a dedicated PowerShell window, from the repo root:

```powershell
$env:Horoscope__Provider = "Fake"
dotnet run --project api/src/AstraKa.Api
```

Leave this running for both measurements below. Note the URL it listens on (printed at startup,
typically `http://localhost:5215`) - pass it to k6 via `--env BASE_URL=...` if it differs from the
script's default.

## 3. Run "before" - caching off

In the **same** PowerShell window as step 2, stop the app (`Ctrl+C`), then:

```powershell
$env:Horoscope__Provider = "Fake"
$env:Horoscope__CacheEnabled = "false"
dotnet run --project api/src/AstraKa.Api
```

In a **second** PowerShell window:

```powershell
k6 run api/loadtests/horoscope.js
```

Record the results in the table below under "Before".

## 4. Run "after" - caching on

Stop the app again, then:

```powershell
$env:Horoscope__Provider = "Fake"
$env:Horoscope__CacheEnabled = "true"   # or just omit this line - true is the default
dotnet run --project api/src/AstraKa.Api
```

Run k6 again exactly the same way:

```powershell
k6 run api/loadtests/horoscope.js
```

Record the results under "After".

## Results

Fill in from each run's k6 summary output (`http_reqs` for RPS, `http_req_duration` for
avg/p95, `http_req_failed` for the error rate):

| Metric | Before (`CacheEnabled=false`) | After (`CacheEnabled=true`) |
|---|---|---|
| RPS (req/s) | | |
| Avg latency | | |
| p95 latency | | |
| Error rate | | |

## Why the numbers should change

With caching off, every single request calls the fake provider directly and pays its ~300ms
artificial latency - at 50 concurrent VUs, overall throughput is bounded by how many of those
300ms calls can run at once, and average/p95 latency sit close to 300ms (plus overhead).

With caching on, only the *first* request for each of the 12 signs on a given UTC day is a cache
miss and pays the 300ms; `HybridCache.GetOrCreateAsync`'s stampede protection means even several
concurrent misses for the *same* sign only trigger one factory call, not one per request. Every
other request - which, with only 12 signs and 50 VUs over 30s, is the overwhelming majority -
returns from the in-process cache in well under a millisecond. Expect RPS to rise sharply and
average/p95 latency to drop close to the no-op baseline once the cache has warmed (within the
first second or two of the run).
