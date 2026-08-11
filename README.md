# AnxietyWatch Web

Frontend web oficial de AnxietyWatch. La fuente operativa vive en:

```text
https://github.com/anxietywatch-org/anxietywatch-web
```

## Local

```powershell
dotnet restore AnxietyWatch.Web.sln
dotnet run --project AnxietyWatch.Web/AnxietyWatch.Web.csproj
```

La API se configura en `AnxietyWatch.Web.Client/wwwroot/appsettings.json`.

```json
{
  "Api": {
    "BaseUrl": "https://api.mangoon.xyz/"
  }
}
```

## CI

GitHub Actions ejecuta `Web CI` en `master`, `main`, pull requests y manualmente.

El pipeline:

- restaura dependencias .NET 10;
- compila `AnxietyWatch.Web.sln` en `Release`;
- publica `AnxietyWatch.Web`;
- sube el artifact `anxietywatch-web`.

## Producción

DigitalOcean App Platform debe apuntar a:

```text
Repository: anxietywatch-org/anxietywatch-web
Branch: master
```

Dominio frontend:

```text
https://mangoon.xyz
```

API permitida por CORS:

```text
https://api.mangoon.xyz
```

Después de cada deploy, validar:

```powershell
curl.exe -fsS https://mangoon.xyz
curl.exe -fsS https://api.mangoon.xyz/api/plans
```

Luego crear un usuario temporal desde la UI y confirmar que el dashboard ya no queda detenido en `Restaurando tu sesión...`.
