# Contratos pendientes del frontend

Este documento describe los contratos que el frontend propone para el MVP. Son maquetas y tipos preparatorios: **no confirma que los endpoints existan ni que los flujos funcionen contra el backend**.

Los errores se esperan como `application/problem+json`, usando al menos `title` y `status`, según `ApiProblemDetails`.

## BE-01: verificación de correo — ya implementada

**Ya no es un contrato pendiente.** Se implementó y fusionó en `origin/master` el 14 de agosto
(PR "codex/support-tickets-ui", commit `55b7fc4`), en paralelo a este trabajo. La ruta real es
`POST /api/auth/verify-email/confirm`, consumida por `AuthService.ConfirmEmailVerificationAsync`
recibiendo `EmailVerificationConfirmRequest` desde `Pages/Auth/VerifyEmail.razor`. Este
documento ya no describe ese contrato; consulta esos archivos directamente para el detalle
actual del payload y los estados manejados en la UI.

## BE-02: pago simulado

**Método y ruta:** `POST /api/billing/simulate-payment`

**Request JSON**

```json
{
  "planId": "individual",
  "billingCycle": "monthly"
}
```

`billingCycle` se limita actualmente a `monthly` o `annual`. No se envían datos bancarios.

**Response 200 propuesta**

```json
{
  "transactionId": "txn_sim_01JABC123",
  "planId": "individual",
  "billingCycle": "monthly",
  "amount": 9.99,
  "currency": "USD",
  "status": "completed",
  "simulated": true,
  "createdAt": "2026-08-15T14:30:00Z"
}
```

**Errores que el frontend puede presentar**

| Estado | Cuerpo de ejemplo |
| --- | --- |
| `401` | `{"title":"La sesión no es válida.","status":401}` |
| `409` | `{"title":"El cambio de plan entra en conflicto con el estado actual.","status":409}` |
| `410` | `{"title":"La opción de plan ya no está disponible.","status":410}` |
| `429` | `{"title":"Demasiados intentos de pago simulado.","status":429}` |
| `503` | `{"title":"La simulación no está disponible.","status":503}` |

La pantalla de checkout es una maqueta. No representa un pago real ni confirma la actualización de la sesión.

## BE-03: resumen de facturación

**Método y ruta:** `GET /api/billing/summary`

**Request JSON:** no aplica; la petición `GET` no lleva cuerpo.

**Response 200 propuesta**

```json
{
  "planId": "individual",
  "billingCycle": "monthly",
  "status": "active",
  "lastPayment": {
    "transactionId": "txn_sim_01JABC123",
    "planId": "individual",
    "billingCycle": "monthly",
    "amount": 9.99,
    "currency": "USD",
    "status": "completed",
    "simulated": true,
    "createdAt": "2026-08-15T14:30:00Z"
  },
  "simulated": true
}
```

`lastPayment` puede ser `null`.

**Errores que el frontend puede presentar**

| Estado | Cuerpo de ejemplo |
| --- | --- |
| `401` | `{"title":"La sesión no es válida.","status":401}` |
| `409` | `{"title":"El estado de facturación es inconsistente.","status":409}` |
| `410` | `{"title":"El resumen ya no está disponible.","status":410}` |
| `429` | `{"title":"Demasiadas consultas.","status":429}` |
| `503` | `{"title":"Facturación no disponible.","status":503}` |

## BE-03: historial de transacciones

**Método y ruta:** `GET /api/billing/transactions`

**Request JSON:** no aplica; la petición `GET` no lleva cuerpo.

**Response 200 propuesta**

```json
[
  {
    "transactionId": "txn_sim_01JABC123",
    "planId": "individual",
    "billingCycle": "monthly",
    "amount": 9.99,
    "currency": "USD",
    "status": "completed",
    "simulated": true,
    "createdAt": "2026-08-15T14:30:00Z"
  }
]
```

**Errores que el frontend puede presentar**

| Estado | Cuerpo de ejemplo |
| --- | --- |
| `401` | `{"title":"La sesión no es válida.","status":401}` |
| `409` | `{"title":"El historial no puede consultarse en el estado actual.","status":409}` |
| `410` | `{"title":"El historial solicitado ya no está disponible.","status":410}` |
| `429` | `{"title":"Demasiadas consultas.","status":429}` |
| `503` | `{"title":"El historial no está disponible.","status":503}` |

## BE-04: tokens con cuota

**Método y ruta:** `GET /api/tokens`

**Request JSON:** no aplica; la petición `GET` no lleva cuerpo.

**Response 200 esperada, pendiente de confirmación**

```json
{
  "items": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "code": "CODE-1234",
      "role": "self",
      "expiresAt": "2026-08-16T14:30:00Z",
      "status": "pending"
    }
  ],
  "quota": {
    "limit": 5,
    "used": 1,
    "remaining": 4
  }
}
```

El frontend espera explícitamente la forma `{ items: LinkTokenDto[], quota: { limit, used, remaining } }`. Los significados, límites y reglas de cómputo de esos tres enteros deben ser autoritativos del backend.

> **No migrar todavía:** `Services/TokenService.cs` y `Pages/Dashboard/Tokens.razor` **NO se van a tocar hasta que este contrato quede confirmado**. Actualmente el servicio consume un arreglo de `LinkTokenDto`; cambiarlo antes de confirmar BE-04 rompería el flujo existente.

**Errores que el frontend puede presentar**

| Estado | Cuerpo de ejemplo |
| --- | --- |
| `401` | `{"title":"La sesión no es válida.","status":401}` |
| `409` | `{"title":"La cuota está en un estado inconsistente.","status":409}` |
| `410` | `{"title":"La cuota ya no está disponible.","status":410}` |
| `429` | `{"title":"Se alcanzó el límite de solicitudes.","status":429}` |
| `503` | `{"title":"El servicio de tokens no está disponible.","status":503}` |

## Pendiente de confirmar

1. payload y respuesta del pago simulado
2. resumen e historial de facturación
3. estructura autoritativa de cuota de tokens
4. forma de renovar la sesión tras cambiar de plan

> La verificación de correo ya no está en esta lista: se implementó y fusionó en
> `origin/master` el 14 de agosto (ver sección BE-01 arriba).
