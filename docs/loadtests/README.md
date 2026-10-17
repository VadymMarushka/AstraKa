# Load tests - horoscope caching, before/after

Measures the effect of the `HybridCache`-backed horoscope cache (see `docs/decisions.md` ADR #14)
using [k6](https://k6.io/). Both runs use the **fake** horoscope provider
(`Horoscope:Provider=Fake`, artificial latency, no network call) - **never the real third-party
API** (freehoroscopeapi.com). The fake's latency is set to 400 ms, the latency of the real API
measured with curl. The only thing that changes between the two runs is `Horoscope:CacheEnabled`.

## 1. Install k6 (Windows)

```powershell
winget install k6 --source winget
```

Reopen the terminal, then confirm with `k6 version`.

## 2. Run "before" - caching off

In a PowerShell window, from the repo root:

```powershell
$env:Horoscope__Provider = "Fake"
$env:Horoscope__FakeLatencyMs = "400"
$env:Horoscope__CacheEnabled = "false"
dotnet run --project api/src/AstraKa.Api
```

`$env:` variables only apply to this window, so the API must be started from it. Before load
testing, open `http://localhost:5215/api/v1/zodiac-signs/1/horoscope` once and check that the
response is the fake's text ("The stars favor Aries today."), not a real horoscope.

In a **second** PowerShell window:

```powershell
k6 run api/loadtests/horoscope.js
```

If the API listens on another URL, pass it with `--env BASE_URL=...`.

## 3. Run "after" - caching on

Stop the API (`Ctrl+C`) and, in the **same** window (Provider and FakeLatencyMs are already set):

```powershell
$env:Horoscope__CacheEnabled = "true"
dotnet run --project api/src/AstraKa.Api
```

Run k6 again exactly the same way.

## Why the numbers change

With caching off, every request calls the fake provider and waits its 400 ms - at 50 concurrent
VUs, throughput is bounded by how many of those 400 ms calls can run at once.

With caching on, only the *first* request for each of the 12 signs on a given UTC day is a cache
miss and pays the 400 ms; `HybridCache.GetOrCreateAsync`'s stampede protection means several
concurrent misses for the *same* sign trigger only one factory call. Every other request is served
from the in-process cache: the lookup itself takes well under a millisecond, and what remains of
the response time is the HTTP pipeline, JSON serialization and k6 competing for the same CPU.

## Results

Measured on 2026-10-17. k6 and the API ran on the same Windows machine (shared CPU).
50 VUs, 30 s, anonymous `GET /api/v1/zodiac-signs/{1..12}/horoscope`, fake provider with
`FakeLatencyMs=400`.

| Metric | Before (`CacheEnabled=false`) | After (`CacheEnabled=true`) | Change |
|---|---|---|---|
| Throughput (req/s) | 118 | 5,971 | ×50 |
| Requests in 30 s | 3,580 | 179,170 | ×50 |
| Avg latency | 420.17 ms | 7.66 ms | ×55 faster |
| Median latency | 414.11 ms | 4.85 ms | ×85 faster |
| p95 latency | 430.42 ms | 14.42 ms | ×30 faster |
| Max latency | 816.13 ms | 4.92 s (cold start, see below) | — |
| Error rate | 0.00% | 0.00% | — |

Screenshots of the k6 summaries: [`before.png`](before.png), [`after.png`](after.png).

## Observations

- **Before matches the theory.** With every request waiting ~0.42 s on the provider, 50 VUs can
  do at most 50 / 0.42 ≈ 119 req/s - the measured 118 req/s. Throughput was bound entirely by
  waiting on the external call, not by our code or the database.
- **After, the bottleneck moves to the CPU.** A cache hit does no I/O at all (no provider call, no
  DB query - the sign lookup lives inside the cache factory), so throughput is limited only by
  Kestrel, serialization and k6 itself competing for the same CPU. Absolute numbers depend on the
  machine; the relative difference is what this test demonstrates.
- **The 4.92 s max in the "after" run is a cold-start outlier**, not a cache problem: the first
  requests after startup pay for JIT compilation, the first database connection and filling the
  cache for 12 signs. It affects a handful of the 179,170 requests and does not move p95 (14.42 ms).
  A warm-up request per sign before the run would remove it from the summary.
- **Zero errors in both runs** - the cache changes performance, not behaviour; responses are
  identical apart from timing.