import http from 'k6/http';
import { check } from 'k6';

// Base URL for the API under test. Override with `--env BASE_URL=...` - see docs/loadtests/README.md.
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5215';

export const options = {
  vus: 50,
  duration: '30s',
  thresholds: {
    // Deliberately generous - the point of this script is the before/after numbers in the
    // README's results table, not a pass/fail gate. The "before" run (caching off, every request
    // pays the fake provider's ~300ms) is expected to run close to these limits; "after" should
    // run well under them.
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.05'],
  },
};

export default function () {
  // Anonymous GETs spread across all 12 signs - with caching on, only the first request per sign
  // (per UTC day) should ever reach the (fake) provider; everything else is a cache hit.
  const signId = Math.floor(Math.random() * 12) + 1;
  const res = http.get(`${BASE_URL}/api/v1/zodiac-signs/${signId}/horoscope`);
  check(res, { 'status is 200': (r) => r.status === 200 });
}
