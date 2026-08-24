# Cambios frontend desde disponibilidad de correo

Este documento resume los cambios realizados desde la incorporación del contrato
`POST /api/auth/email-availability`. El alcance incluye disponibilidad de correo durante el
registro, downgrade autenticado al plan Gratuito y rotación de tokens de vinculación.

## 1. Disponibilidad de correo en registro

### Contrato de servicio

- Se agregó `IAuthService.CheckEmailAvailabilityAsync(string email, CancellationToken)`.
- `AuthService` envía `POST /api/auth/email-availability` con el body `{ "email": "..." }`.
- La respuesta se procesa con `ReadApiAsync<EmailAvailabilityResponse>`.
- Se creó `Models/Auth/EmailAvailabilityResponse.cs` con la propiedad booleana `Available`
  mapeada desde `available`.

### Flujo de registro

- `ContinueToPlan()` pasó de síncrono a async.
- Antes de avanzar al paso de selección de plan consulta la disponibilidad del correo.
- Si `Available` es `false`, permanece en el paso 1 y muestra:
  `Este correo ya tiene una cuenta. Inicia sesión o usa otro correo.`
- Si `Available` es `true`, limpia el error y avanza normalmente al paso 2.
- Mientras espera la respuesta, el botón queda deshabilitado y muestra
  `Comprobando disponibilidad...`.
- Un error de red o API en esta comprobación orientativa no bloquea el registro; se permite
  avanzar y `POST /api/auth/register` conserva la autoridad final.
- El manejo de HTTP 409 en `CompleteRegistrationAsync()` permanece intacto como protección
  frente a carreras entre la consulta y el registro definitivo.

### Pruebas

- Se creó `ServiceTests.cs` con cobertura para respuestas `available: true` y
  `available: false`.
- La prueba valida método HTTP, ruta, body enviado y deserialización del resultado.
- Se actualizó el stub `IAuthService` de `AuthTokenFlowTests.cs` para la nueva firma.

## 2. Downgrade al plan Gratuito

### Contrato de servicio

- Se agregó `IBillingService.DowngradeToFreeAsync(CancellationToken)`.
- `BillingService` envía `POST /api/billing/downgrade-to-free` sin body.
- Se creó `DowngradeToFreeResponse` en `Models/Api/BillingDtos.cs` con:
  `PlanId`, `PreviousPlanId`, `Changed` y `DowngradedAt`.
- El endpoint de pago simulado continúa reservado para Individual, Familiar y Profesional.

### Flujo en Mi plan

- El modal del plan Gratuito vuelve a permitir confirmar el cambio sin solicitar tarjeta,
  vencimiento, CVV ni ciclo de facturación.
- `ConfirmFreeDowngradeAsync()` evita ejecuciones simultáneas mediante `isChangingPlan`.
- Tras el downgrade refresca la sesión con `SessionManager.RefreshSessionAsync()` para obtener
  el plan y privilegios actuales.
- Después vuelve a cargar el catálogo para actualizar la tarjeta de plan activo sin recargar
  manualmente la página.
- Si `Changed` es `true`, muestra `Tu plan se cambió a Gratuito.`.
- Si `Changed` es `false`, trata la operación idempotente como éxito y muestra
  `Ya tienes el plan Gratuito.`.
- Conserva el patrón de errores del checkout: 401 limpia la sesión, errores de API usan
  `ApiErrorMessages.For` y errores inesperados muestran un mensaje de reintento.

### Pruebas

- `ServiceTests.cs` cubre `changed: true` y `changed: false`.
- Las pruebas validan POST, ruta sin body y deserialización completa del DTO.

## 3. Rotación de tokens

### Contrato de servicio

- Se agregó `ITokenService.RotateTokenAsync(Guid id, CancellationToken)`.
- `TokenService` envía `POST /api/tokens/{id}/rotate` sin body.
- La respuesta se deserializa como `LinkTokenDto`.

### Flujo en Tokens de vinculación

- Se añadió la acción `Regenerar` con un icono propio de rotación en la tabla.
- Solo aparece para tokens cuyo status backend es `pending`.
- Los tokens `pending` vencidos visualmente también se pueden regenerar.
- Los tokens `accepted` y `deleted` no ofrecen esta acción.
- Se agregó `isRotating` y se incluyó en `mutationInProgress` para bloquear mutaciones
  concurrentes desde la interfaz.
- Antes de rotar se usa la confirmación nativa ya empleada por la revocación.
- En éxito, el token devuelto reemplaza al anterior en la misma posición de `allTokens` y se
  recalcula `visibleTokens`.
- Se reutiliza el modal de creación para mostrar el nuevo código completo, permitir copiarlo,
  informar la nueva expiración y advertir que el código anterior dejó de ser válido.

### Conflictos y fallos inciertos

- Ante HTTP 409 se vuelve a consultar la lista y se informa que el token cambió de estado
  durante una operación concurrente.
- Ante un fallo de transporte o resultado incierto no se reintenta automáticamente el rotate.
- Primero se ejecuta `GET /api/tokens` para recuperar el código vigente real.
- Cualquier nuevo intento queda bajo acción manual del usuario después de revisar la lista.

### Pruebas

- `ServiceTests.cs` incluye un caso exitoso que valida método, ruta sin body y token devuelto.
- Incluye también un caso HTTP 409 que confirma la propagación de `ApiException` con ese status.

## Documentación contractual

Se actualizó `docs/frontend-contracts.md` para registrar:

- `POST /api/auth/email-availability` como contrato resuelto.
- `POST /api/billing/downgrade-to-free` como operación separada e idempotente.
- `POST /api/tokens/{id}/rotate`, sus estados permitidos y su comportamiento ante carreras.
- La obligación de refrescar la sesión después de un downgrade.
- La recomendación de recargar tokens antes de reintentar tras una respuesta perdida.

## Archivos modificados

- `AnxietyWatch.Web.Client/Services/IAuthService.cs`
- `AnxietyWatch.Web.Client/Services/AuthService.cs`
- `AnxietyWatch.Web.Client/Models/Auth/EmailAvailabilityResponse.cs` (nuevo)
- `AnxietyWatch.Web.Client/Pages/Auth/Auth.razor`
- `AnxietyWatch.Web.Client/Services/BillingService.cs`
- `AnxietyWatch.Web.Client/Models/Api/BillingDtos.cs`
- `AnxietyWatch.Web.Client/Pages/Dashboard/Plan.razor`
- `AnxietyWatch.Web.Client/Services/TokenService.cs`
- `AnxietyWatch.Web.Client/Pages/Dashboard/Tokens.razor`
- `AnxietyWatch.Web.Client.Tests/ServiceTests.cs` (nuevo)
- `AnxietyWatch.Web.Client.Tests/AuthTokenFlowTests.cs`
- `docs/frontend-contracts.md`

## Validación

- `dotnet build AnxietyWatch.Web.sln --configuration Release`: 0 errores, 0 advertencias.
- `dotnet test AnxietyWatch.Web.Client.Tests/AnxietyWatch.Web.Client.Tests.csproj --configuration Release --no-build`: 20/20 pruebas aprobadas.
- Las pruebas manuales con cuentas reales quedaron pendientes por falta de credenciales y de un
  flujo autenticado de vinculación disponible en esta sesión.
