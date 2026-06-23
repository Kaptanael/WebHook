# Token-Based Authentication Reference

## Token Format

Tokens are **base64url-encoded** strings containing newline-separated fields:

```
<BaseUrl>\n<ApiKey>\n<ClientId>\n<ClientSecret>\n<ApplicationName>\n<CompanyId>
```

### BaseUrl Format
- `S|<domain>` → decoded as `https://<domain>`
- `H|<domain>` → decoded as `http://<domain>`
- Example: `S|api.example.com` → `https://api.example.com`

### Example Token Structure (decoded)
```
S|api.example.com
sk_test_abc123...
client_id_123
client_secret_xyz
TestApp
1
```

## Token Encoding

To create a base64url-encoded token:

```javascript
// Raw token parts
const baseUrl = "S|api.example.com";  // HTTPS
const apiKey = "sk_test_12345678";
const clientId = "client_id_123";
const clientSecret = "client_secret_456";
const appName = "TestApp";
const companyId = "1";

// Join with newlines
const decoded = `${baseUrl}\n${apiKey}\n${clientId}\n${clientSecret}\n${appName}\n${companyId}`;

// Encode to base64url
const token = btoa(decoded)
  .replace(/\+/g, '-')     // + → -
  .replace(/\//g, '_')     // / → _
  .replace(/=/g, '');      // Remove padding
```

## API Endpoint: POST /api/webhook

### Headers Required
- `Content-Type: application/json`
- `X-Endpoint-Token: <base64url-token>`
- `X-Signature: <HMAC-SHA256 signature>`
- `X-Timestamp: <unix-timestamp-seconds>`

### Request Body
```json
{
  "EventType": "order.created",
  "Client": "<your-app-name>",
  "Payload": {
    "orderId": 42,
    "amount": 19.99
  }
}
```

### Signature Computation

The signature is computed as HMAC-SHA256 of the request payload, using the **ApiKey** (from decoded token) as the key:

```javascript
// 1. Get the raw JSON payload (exact bytes)
const payload = JSON.stringify({ EventType: "order.created", Client: "TestApp", Payload: { orderId: 42 } });

// 2. Get current unix timestamp
const timestamp = Math.floor(Date.now() / 1000);

// 3. Create the message to sign: "timestamp.payload"
const message = `${timestamp}.${payload}`;

// 4. Compute HMAC-SHA256 with ApiKey from decoded token
async function computeSignature(apiKey, message) {
  const key = await crypto.subtle.importKey(
    'raw',
    new TextEncoder().encode(apiKey),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign']
  );
  const signature = await crypto.subtle.sign('HMAC', key, new TextEncoder().encode(message));
  // Return as hex string
  return Array.from(new Uint8Array(signature))
    .map(b => b.toString(16).padStart(2, '0'))
    .join('');
}

const sig = await computeSignature(apiKey, message);
// Use in X-Signature header
```

## Example: Complete Test Request

```bash
# Token decoded as:
# BaseUrl: https://api.example.com
# ApiKey: sk_test_12345678
# ClientId: client_123
# ClientSecret: secret_456
# AppName: TestApp
# CompanyId: 1

TOKEN="S1xhcGkuZXhhbXBsZS5jb20Kc2tfdGVzdF8xMjM0NTY3OApjbGllbnRfMTIzCnNlY3JldF80NTYKVGVzdEFwcAox"

PAYLOAD='{"EventType":"order.created","Client":"TestApp","Payload":{"orderId":42}}'
TIMESTAMP=$(date +%s)

# Compute HMAC-SHA256(timestamp.payload) with ApiKey
# Using openssl:
SIGNATURE=$(echo -n "${TIMESTAMP}.${PAYLOAD}" | openssl dgst -sha256 -hmac "sk_test_12345678" -hex | cut -d' ' -f2)

curl -X POST http://localhost:5062/api/webhook \
  -H "Content-Type: application/json" \
  -H "X-Endpoint-Token: ${TOKEN}" \
  -H "X-Signature: ${SIGNATURE}" \
  -H "X-Timestamp: ${TIMESTAMP}" \
  -d "${PAYLOAD}"
```

## Token Validation Flow

When you POST to `/api/webhook`:

1. **Header Validation** → Checks timestamp (must be recent, ±5min tolerance) and signature format
2. **Token Decode** → Decodes the X-Endpoint-Token to extract ApiKey, ClientId, ClientSecret, CompanyId
3. **Token Verification** → Calls MVP API with the ClientId/ClientSecret to verify the token and get API tokens
4. **Signature Verification** → Recomputes HMAC-SHA256(timestamp.payload) with the decoded ApiKey and compares to X-Signature header
5. **Fan-out** → If all checks pass, creates WebhookEvent records for all active endpoints in the company

## Error Responses

### 401 Unauthorized
```json
{
  "success": false,
  "error": "Invalid token." | "Invalid signature." | "Timestamp expired." | "Invalid headers."
}
```

### 400 Bad Request
```json
{
  "success": false,
  "error": "Payload cannot be empty." | "Invalid event type."
}
```

## Connection Management

When you first submit an event with a token:

1. The system calls `EnsureConnectionAsync(token)`
2. This creates a **WebHookConnection** record if one doesn't exist
3. The connection stores the MVP API tokens (access + refresh) for later use
4. Subsequent requests with the same token will reuse this connection

## Testing Checklist

- [ ] Token decodes properly (6 newline-separated fields)
- [ ] BaseUrl format is correct (S| or H| prefix)
- [ ] CompanyId is an integer
- [ ] Timestamp is within ±300 seconds of current time
- [ ] Signature is HMAC-SHA256(timestamp.payload) with ApiKey from token
- [ ] Payload is valid JSON
- [ ] EventType matches a subscribed endpoint for the company
- [ ] MVP API can verify the ClientId/ClientSecret combination
