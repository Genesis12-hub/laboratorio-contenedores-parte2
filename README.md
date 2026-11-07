# Laboratorio de contenedores — Parte 2

Aplicación mínima en .NET 10 orquestada con Aspire 13.6.4. El proyecto `AppHost` en `infra` levanta Redis y PostgreSQL en contenedores y arranca `Laboratorio.Api`. `ServiceDefaults` agrega las comprobaciones de salud y la telemetría OpenTelemetry de la plantilla oficial.

La estructura y los paquetes salen de las plantillas y de la documentación actual de Aspire 13.6.4 (`aspire.dev` y NuGet). No reproduce código de un video de clase.

## Proyectos

| Proyecto | Ruta | Referencias |
| --- | --- | --- |
| `AppHost` | `infra/AppHost` | Proyecto `src/Laboratorio.Api`. Paquetes `Aspire.Hosting.Redis` y `Aspire.Hosting.PostgreSQL` 13.6.4. |
| `Laboratorio.Api` | `src/Laboratorio.Api` | Proyecto `src/ServiceDefaults`. Paquetes `Aspire.StackExchange.Redis` y `Aspire.Npgsql` 13.6.4. |
| `ServiceDefaults` | `src/ServiceDefaults` | Plantilla oficial: `/health`, `/alive` y OpenTelemetry. |

`Laboratorio.slnx` agrupa `infra` y `src`. El SDK queda fijado en `global.json` (10.0.114, `latestPatch`). Las versiones de NuGet están fijadas en cada `.csproj` y en `packages.lock.json`. `nuget.config` usa solo nuget.org.

La API expone:

- `GET /health` — incluye las comprobaciones de Redis y PostgreSQL.
- `GET /alive` — comprobación de vida.
- `GET /status` — ejecuta `PING` en Redis y `SELECT 1` en PostgreSQL, sin devolver contraseñas ni cadenas de conexión.

## Requisitos

- .NET SDK 10.0.114 o un parche posterior de 10.0. En Ubuntu 24.04: `sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0`
- Docker Engine en ejecución, accesible por el socket unix local. Aspire descarga `redis:8.6` y `postgres:18.3`.
- El panel de Aspire queda en localhost y pide el token que imprime la consola. No hace falta desactivar esa autenticación.

Aspire genera las contraseñas de Redis y PostgreSQL en user-secrets del AppHost, fuera de este repositorio (`UserSecretsId` del `.csproj`). No las copies al código ni a `appsettings.json`.

## Ejecutar

```bash
dotnet restore Laboratorio.slnx
dotnet build Laboratorio.slnx
dotnet run --project infra/AppHost/AppHost.csproj --launch-profile https
```

El perfil `https` abre el panel en `https://localhost:17023`. Entra con el enlace de login que muestra la consola y revisa los recursos `postgres`, `laboratorio`, `redis` y `laboratorio-api`.

Con la API ya en marcha, en otra terminal:

```bash
curl -fsS http://localhost:5034/health
curl -fsS http://localhost:5034/alive
curl -fsS http://localhost:5034/status
```

Esos puertos salen del perfil de `Laboratorio.Api`. Si Aspire muestra otros, usa los de la columna URLs del panel.

## Validación en esta ejecución

La comprobación se hizo en Linux Ubuntu 24.04.5 x64 (máquina remota). No se ejecutó en Windows ni en WSL, y no se instaló nada en el equipo local del estudiante. En esa máquina `systemd` no estaba activo, así que Docker se inició con `dockerd` solo en el socket unix y los puertos de los contenedores quedaron en `127.0.0.1`.

Versiones observadas:

- .NET SDK 10.0.114
- ASP.NET Core / runtime 10.0.14
- Aspire.ProjectTemplates, paquetes de hosting/cliente y `aspire` CLI 13.6.4
- Docker 29.1.3, containerd 2.2.1, runc 1.3.4

`dotnet build Laboratorio.slnx` terminó con 0 advertencias y 0 errores. La solución no incluye proyecto de pruebas; `dotnet test` restaura y no ejecuta tests.

Con el AppHost en marcha, los contenedores reales fueron `redis:8.6` y `postgres:18.3`. Dentro de Redis, `redis-cli ping` respondió `PONG`. PostgreSQL aceptaba conexiones, existía la base `laboratorio` y `SELECT 1` devolvió `1`.

La API respondió:

```text
GET http://127.0.0.1:5034/health -> 200 Healthy
GET http://127.0.0.1:5034/alive  -> 200 Healthy
GET http://127.0.0.1:5034/status -> {"service":"Laboratorio.Api","redis":"PONG","redisLatencyMs":1.0755,"postgres":"ready"}
GET https://127.0.0.1:7092/health -> 200 Healthy
```

El panel mostró `postgres`, `laboratorio`, `redis` y `laboratorio-api` en estado Running. La captura de esa ventana está en `docs/capturas/01_aspire_dashboard.png`. El certificado de desarrollo no quedó totalmente confiado en este Linux, por eso el navegador marca el panel como "Not secure"; el inicio de sesión del panel siguió activo y el token no se guardó en el repositorio.
