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

### Cambio al plan Gratuito — contrato pendiente

No hay evidencia autenticada de que `POST /api/billing/simulate-payment` acepte
`planId = "free"` ni de que actualice la sesión y el resumen de facturación al plan Gratuito.
Hasta que backend confirme ese contrato o proporcione una operación específica de downgrade,
el frontend no envía esa solicitud: muestra el cambio a Gratuito sin campos de pago y mantiene
la confirmación deshabilitada con una indicación para contactar al equipo.

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

### Role de cuidador y colaborador profesional

Por decisión de producto, un colaborador/profesional cumple el mismo papel conceptual que un
cuidador. El plan Profesional reutiliza `CreateTokenRequest.Role = "family_member"` y lo
muestra como “Colaborador/profesional”; el plan Familiar muestra el mismo role como
“Cuidador/familiar”. La etiqueta se resuelve con el plan actual de la cuenta.

### Revocación de tokens accepted — disponible en master

Los tokens `accepted` no se eliminan mediante DELETE. Master usa
`POST /api/tokens/{id}/revoke` a través de `TokenService.RevokeTokenAsync`; la UI conserva el
botón “Revocar” y su confirmación nativa del navegador. Esto resuelve la necesidad de
desvincular una vinculación aceptada sin ampliar `CanDelete`, que sigue limitado a `pending`.

La eliminación de un token `accepted` mediante `DELETE /api/tokens/{id}` devuelve 409:

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "An accepted token cannot be deleted.",
  "status": 409
}
```

## Solicitudes pendientes

1. Endpoint seguro de disponibilidad de correo durante el primer paso del registro.
2. Contrato para regenerar o refrescar un token.
3. Contrato autenticado para cambiar una cuenta de pago al plan Gratuito.
