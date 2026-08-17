# Contratos confirmados y solicitudes pendientes del frontend

Este documento conserva evidencia obtenida contra producción. Las implementaciones de
registro, checkout, billing y verificación de correo pertenecen al trabajo ya fusionado en
`origin/master`; no se mantienen implementaciones paralelas en esta rama.

## Verificación de correo — confirmada el 17 de agosto de 2026

Rutas de master:

- `POST /api/auth/verify-email/confirm`
- `GET /api/auth/verify-email/status`
- `POST /api/auth/verify-email/resend`

Prueba real con una cuenta nueva:

- Status inicial: HTTP 200 con `{ "emailVerified": false }`.
- Reenvío: HTTP 200 con `{ "message": "Verification email sent" }`; el correo llegó.
- El enlace `/verify-email?token=...` verificó la cuenta.
- Status posterior: HTTP 200 con `{ "emailVerified": true }`.
- Token inválido, vencido y reutilizado se agrupan bajo HTTP 410.

Ejemplo observado de 410:

```json
{
  "type": "https://httpstatuses.com/410",
  "title": "The verification link is expired or has already been used.",
  "status": 410
}
```

## Billing simulado — confirmado el 17 de agosto de 2026

La implementación real vive en `origin/master`.

### POST /api/billing/simulate-payment

Request probado:

```json
{
  "planId": "individual",
  "billingCycle": "monthly"
}
```

Response observada, HTTP 201:

```json
{
  "transactionId": "ef3cc0c2-7141-4f7b-a9b2-7bc743ad4eef",
  "planId": "individual",
  "billingCycle": "monthly",
  "amount": 9.99,
  "currency": "MXN",
  "status": "succeeded",
  "simulated": true,
  "createdAt": "2026-08-17T10:52:56.5548495+00:00"
}
```

Después del pago, `GET /api/auth/session` devolvió una sesión con `planId = individual` y un
JWT renovado. El token previo conservaba claims del plan anterior; el cliente debe persistir
la sesión renovada.

### GET /api/billing/summary

Respondió HTTP 200. El cuerpo observado contiene:

```json
{
  "planId": "individual",
  "billingCycle": "monthly",
  "status": "active",
  "lastPayment": {
    "transactionId": "ef3cc0c2-7141-4f7b-a9b2-7bc743ad4eef",
    "planId": "individual",
    "billingCycle": "monthly",
    "amount": 9.99,
    "currency": "MXN",
    "status": "succeeded",
    "simulated": true,
    "createdAt": "2026-08-17T10:52:56.554+00:00"
  },
  "transactions": [
    {
      "transactionId": "ef3cc0c2-7141-4f7b-a9b2-7bc743ad4eef",
      "planId": "individual",
      "billingCycle": "monthly",
      "amount": 9.99,
      "currency": "MXN",
      "status": "succeeded",
      "simulated": true,
      "createdAt": "2026-08-17T10:52:56.554+00:00"
    }
  ],
  "simulated": true
}
```

### GET /api/billing/transactions

Respondió HTTP 200 con un arreglo de transacciones con la misma forma de `lastPayment`.

## Cuota de tokens — implementación de master

Master consulta `GET /api/tokens/quota` mediante `TokenService.GetQuotaAsync()` y usa:

```json
{
  "limit": 5,
  "used": 1,
  "remaining": 4
}
```

Esta rama conserva esa fuente autoritativa; no calcula el límite por nombre de plan.

## Solicitudes pendientes

1. Endpoint seguro de disponibilidad de correo durante el primer paso del registro.
2. Valor exacto de `CreateTokenRequest.Role` para “Colaborador/profesional”.
3. Contrato para regenerar o refrescar un token.
