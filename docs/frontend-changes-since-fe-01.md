# Cambios frontend supervivientes tras sincronizar con master

`origin/master` incorporó las implementaciones de onboarding de correo, registro con pago
simulado, billing, planes y cuota autoritativa de tokens. Esta rama no duplica esos cambios.

## Fix de registro pendiente

En una rama local separada, `fix/frontend-registro-correo-duplicado`, se conserva únicamente:

- Catch HTTP 409 alrededor de `AuthService.RegisterAsync`.
- Retorno automático al paso “Datos”.
- Error junto al correo: “Este correo ya tiene una cuenta. Inicia sesión o usa otro correo.”
- Un 409 posterior de `SimulatePaymentAsync` no vuelve al paso 1.

## Fixes de tokens pendientes

Esta rama conserva tres ajustes de UI que master no incluía:

1. `generatedToken` tiene prioridad sobre `isLimitReached`, por lo que el código recién creado
   sigue visible aunque alcance la cuota.
2. Las opciones de rol se filtran por plan y `selectedRole` se normaliza si deja de ser válido.
3. La tabla copia el token mediante `CopyTokenAsync` en lugar de abrir el modal de compartir;
   el modal y su estado local se eliminan.

La cuota sigue viniendo de `TokenService.GetQuotaAsync()`. Se refresca después de cargar,
crear y eliminar tokens.

## Copy de episodios y wearable

- Gratuito conserva “Registrar episodio” como acción principal.
- Individual, Familiar y Profesional muestran el registro manual como complemento.
- No se añadieron llamadas de telemetría desde Blazor ni estado ficticio del wearable.
- El formulario manual continúa disponible en todos los planes.

## Evidencia de producción preservada

`docs/frontend-contracts.md` conserva las respuestas reales de billing y verificación de
correo. La implementación correspondiente vive en master, no en ramas locales paralelas.

## Cambios descartados por solapamiento con master

- Implementación local de checkout y billing.
- DTOs locales de billing.
- Registro de `BillingService` local.
- Cambio local de `VerifyEmail.razor`, salvo la evidencia documentada del contrato 410.
- Tercer paso de registro y pago local, reemplazados por el onboarding de master.
