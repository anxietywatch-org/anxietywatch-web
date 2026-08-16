# Resumen de cambios frontend desde FE-01

Este documento resume los cambios aplicados a partir de la tarea FE-01. Distingue funcionalidad del cliente ya implementada de maquetas que siguen pendientes de contratos o endpoints del backend.

> **Nota (2026-08-16):** al reconciliar este trabajo con `origin/master` se detectó que la
> verificación de correo (BE-01/FE-02) ya había sido implementada y fusionada por otra persona
> del equipo en el PR "codex/support-tickets-ui" (commit `55b7fc4`, 14 de agosto), con
> `VerifyEmail.razor` y `AuthService.ConfirmEmailVerificationAsync(EmailVerificationConfirmRequest)`
> llamando a `POST /api/auth/verify-email/confirm`. La versión propia que se había construido en
> paralelo (con `VerifyEmailAsync(string token)`) se descartó para no duplicar la funcionalidad;
> por eso la sección 5 original de este documento ya no aplica y se reemplaza por la nota de abajo.
> El resto de este documento (FE-01, FE-06, FE-07, pruebas, facturación/checkout) sí es trabajo
> nuevo, sin equivalente en `origin/master` al momento de escribir esto.

## 1. FE-01: sesión después de cambiar la contraseña

### Comportamiento final

- `ProfileService.ChangePasswordAsync` ahora espera una respuesta exitosa de `POST api/auth/change-password`, ejecuta `IAuthSessionManager.ClearAsync()` y solo entonces completa la operación.
- `Profile.razor` espera a que termine `ChangePasswordAsync` y navega a `/login?passwordChanged=true` con `replace: true`.
- Se eliminó el código posterior al cambio que reiniciaba el formulario y mostraba éxito dentro del dashboard, porque la navegación hace que ese código sea innecesario.
- Esta secuencia elimina el token y el usuario de `TokenStore` a través de `SessionManager.ClearAsync()` antes de permitir la navegación posterior.
- `Auth.razor` lee `passwordChanged` mediante `SupplyParameterFromQuery` y muestra el aviso reutilizando `auth-alert auth-alert--success`: “Contraseña cambiada. Inicia sesión nuevamente.”

### Archivos

- `AnxietyWatch.Web.Client/Services/ProfileService.cs`
- `AnxietyWatch.Web.Client/Pages/Dashboard/Profile.razor`
- `AnxietyWatch.Web.Client/Pages/Auth/Auth.razor`

## 2. FE-06: comparación de identificadores de plan

- Se agregó `IPlanService.IsCurrentPlan(PlanDto plan, string? currentPlanId)`.
- La comparación usa `StringComparison.OrdinalIgnoreCase`, por lo que valores como `FREE` y `free` representan el mismo plan.
- `Plan.razor` usa la misma función para identificar el plan actual, mostrar su distintivo y deshabilitar su acción.

### Archivos

- `AnxietyWatch.Web.Client/Services/PlanService.cs`
- `AnxietyWatch.Web.Client/Pages/Dashboard/Plan.razor`

## 3. FE-07: manejo de `Retry-After`

- Se centralizó el cálculo en `ParseRetryAfter(HttpResponseHeaders)` para evitar diferencias entre `ReadApiResultAsync<T>` y `ReadApiAsync<T>`.
- Un valor `Delta` se convierte con `Math.Max(0, (int)Delta.Value.TotalSeconds)`.
- Una fecha HTTP absoluta se calcula contra `DateTimeOffset.UtcNow` y también se limita a un mínimo de cero.
- La ausencia del header conserva `RetryAfterSeconds = null`.
- Ninguno de los dos flujos públicos devuelve segundos negativos.

### Archivo

- `AnxietyWatch.Web.Client/Services/HttpApiExtensions.cs`

## 4. Pruebas de servicios y errores HTTP

`origin/master` ya trae `AnxietyWatch.Web.Client.Tests` con `AuthHandlerTests.cs` y
`AuthSessionManagerTests.cs` (sin Moq). Se agregó Moq al `.csproj` existente y
`ServiceTests.cs` como archivo adicional del mismo proyecto, en vez de crear uno nuevo.

### Casos agregados

- Cambio de contraseña: verifica que `ClearAsync()` se invoque y termine antes de completar `ChangePasswordAsync`.
- Planes: verifica `FREE` frente a `free` en ambos sentidos.
- `Retry-After`: delta positivo, delta negativo limitado a cero, fecha futura, fecha pasada limitada a cero y header ausente.
- Cada caso de `Retry-After` comprueba tanto `ApiResult<T>` como `ApiException`.
- El theory de errores HTTP incluye ahora `410 Gone` y `429 TooManyRequests`, además de `400`, `401` y `409`.
- Se adaptó la construcción de `ProfileService` en las pruebas a su dependencia de `IAuthSessionManager`.

### Archivo

- `AnxietyWatch.Web.Client.Tests/ServiceTests.cs`

## 5. BE-01: verificación de correo — ya entregada por otra rama del equipo

No forma parte de este changeset. Ver la nota al inicio del documento: `origin/master` ya trae
`VerifyEmail.razor` y `AuthService.ConfirmEmailVerificationAsync` desde el 14 de agosto.

## 6. BE-02 y BE-03: tipos y cliente de facturación

> Estado: contratos preparatorios pendientes de confirmación del backend.

- Se agregaron los DTO `SimulatePaymentRequest`, `SimulatePaymentResponse`, `BillingSummaryDto` y `BillingTransactionDto`.
- Todos sus nombres JSON están declarados con `JsonPropertyName`.
- Se agregó `IBillingService` y su implementación `BillingService` con:
  - `POST api/billing/simulate-payment`.
  - `GET api/billing/summary`.
  - `GET api/billing/transactions`.
- Todos los métodos aceptan `CancellationToken` y usan `ReadApiAsync<T>`.
- No se modificaron `TokenService` ni su interfaz; la cuota de tokens depende de BE-04.

### Archivos

- `AnxietyWatch.Web.Client/Models/Api/BillingDtos.cs`
- `AnxietyWatch.Web.Client/Services/IBillingService.cs`

## 7. Maqueta de checkout simulado

> Estado: maqueta. No procesa pagos reales y el endpoint puede no existir todavía.

- Se agregó `/dashboard/checkout` con `DashboardLayout`.
- La advertencia “Esto es un pago simulado” permanece visible en todos los estados de la página.
- El formulario solo permite seleccionar un `PlanDto` y el ciclo `monthly` o `annual`.
- No hay campos de tarjeta, CVV, vencimiento ni otros datos bancarios.
- La confirmación llama a `IBillingService.SimulatePaymentAsync`.
- Un `401` limpia la sesión y redirige a `/login`.
- Un `404` o `501` muestra “Aún no disponible”; los demás errores de API usan `ApiErrorMessages`.
- Tras una respuesta exitosa se actualiza de forma optimista el `PlanId` del usuario almacenado mediante `SessionManager.UpdateUserAsync`.
- Se mantiene `TODO(BE-02)` para confirmar la forma autoritativa de renovar la sesión después del cambio de plan.
- `BillingService` se registró en DI con una advertencia TODO para permitir que la maqueta renderice y maneje normalmente la indisponibilidad del backend.
- `AuthHandler` agrega `api/billing` a su lista de rutas autenticadas ya existente (que ahora
  también incluye `api/support`, sumado por el equipo en `origin/master`).
- `Plan.razor` ofrece “Elegir en simulación” únicamente para planes no gratuitos que no sean el plan actual. La acción existente de consulta y soporte se conserva.

### Archivos

- `AnxietyWatch.Web.Client/Pages/Dashboard/Checkout.razor`
- `AnxietyWatch.Web.Client/Pages/Dashboard/Checkout.razor.css`
- `AnxietyWatch.Web.Client/Pages/Dashboard/Plan.razor`
- `AnxietyWatch.Web.Client/Pages/Dashboard/Plan.razor.css`
- `AnxietyWatch.Web.Client/Program.cs`
- `AnxietyWatch.Web.Client/Services/AuthHandler.cs`

## 8. Documentación de contratos pendientes

- Se documentaron BE-01, BE-02, BE-03 y BE-04 con método, ruta, request, response propuesta y ejemplos de errores `ApiProblemDetails`.
- BE-04 declara explícitamente la forma esperada `{ items: LinkTokenDto[], quota: { limit, used, remaining } }`.
- El documento indica que `TokenService.cs` y `Tokens.razor` no deben migrarse hasta confirmar el contrato de cuota.
- Se listaron los cinco puntos que frontend todavía espera del backend.

### Archivo

- `docs/frontend-contracts.md`

## 9. Métrica de ejercicios fuera del MVP

- Se ocultó la tarjeta KPI “Ejercicios completados” de `Dashboard.razor` para no presentar como disponible una función fuera del alcance del MVP.
- El grid de escritorio pasó de cuatro a tres columnas; los breakpoints existentes de dos y una columna se conservaron.
- `DashboardSummaryDto.ExercisesCompleted` no fue eliminado ni modificado.

### Archivos

- `AnxietyWatch.Web.Client/Pages/Dashboard/Dashboard.razor`
- `AnxietyWatch.Web.Client/Pages/Dashboard/Dashboard.razor.css`

## Verificación realizada

- `dotnet build` de la solución completa: correcto, 0 advertencias, 0 errores.
- `dotnet test` sobre `AnxietyWatch.Web.Client.Tests`: 27/27 pruebas correctas (5 de
  `AuthHandlerTests`/`AuthSessionManagerTests` ya existentes en `origin/master` + 22 de
  `ServiceTests.cs`).
- El SDK exacto fijado en `global.json` (`10.0.201`) no está instalado en esta máquina; para
  poder compilar y probar se apuntó temporalmente `global.json` a
  `10.0.400-preview.0.26322.102` (única SDK 10.x disponible) y se restauró a `10.0.201` al
  terminar. No queda ningún cambio de `global.json` en los commits.
- `origin/master` estaba 14 commits por delante del `master` local al momento de este trabajo;
  se hizo `git pull --ff-only` antes de finalizar las ramas para reconciliar (ver nota al inicio
  del documento).

## Pendientes de backend

1. Confirmar request y response de `POST /api/billing/simulate-payment`.
2. Confirmar resumen e historial de facturación.
3. Confirmar la estructura autoritativa de cuota de tokens.
4. Confirmar cómo renovar la sesión después de cambiar el plan.

`POST /api/auth/verify-email/confirm` ya fue confirmado por backend el 2026-08-15 (ver sección 5).
