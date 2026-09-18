# Configuración de secretos (JWT y base de datos)

**Desde 2026-09-05 la clave de firma JWT ya no está en `appsettings*.json`.**
Si acabas de hacer `pull` y la API te emite tokens que dejan de servir al
reiniciar, es esto: configúrala una vez con el comando de abajo.

## Por qué

`Jwt:SigningKey` estaba versionada en `src/turning.API/appsettings.json` y en
`appsettings.Development.json`. Cualquiera con acceso al repositorio (incluido su
historial) podía firmar tokens válidos para cualquier usuario y cualquier rol.
Las dos claves que estuvieron publicadas quedaron **revocadas**: la API las
rechaza aunque lleguen por variable de entorno.

## Cómo configurarla (desarrollo)

Recomendado — user-secrets, que guarda el valor fuera del árbol de trabajo
(en `%APPDATA%\Microsoft\UserSecrets\`), así no hay forma de commitearlo:

```powershell
# generar una clave nueva
$key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))

# guardarla para este proyecto
dotnet user-secrets --project src/turning.API set "Jwt:SigningKey" $key
```

Alternativa por variable de entorno (el doble guion bajo separa la sección):

```powershell
$env:Jwt__SigningKey = "<clave-de-al-menos-32-caracteres>"
```

Cada quien usa su propia clave en local; no hay que compartirla. Solo importa que
sea la misma dentro de un mismo despliegue, porque quien emite el token y quien
lo valida son el mismo proceso.

## Qué pasa si no la configuras

| Entorno | Comportamiento |
|---|---|
| `Development` | La API **arranca igual**, con una clave efímera distinta en cada arranque, y avisa con un `WRN` en consola. Los tokens emitidos dejan de valer al reiniciar. |
| Cualquier otro | La API **no arranca**: el mensaje de error indica qué falta y cómo configurarlo. |

Reglas que se validan al arrancar (`JwtOptions.Validate`): la clave debe existir,
tener al menos 32 caracteres (HMAC-SHA256 exige 256 bits) y no ser ninguna de las
claves revocadas; `Issuer`/`Audience` no pueden ir vacíos y `ExpirationMinutes`
debe ser positivo.

## Lo que sí sigue en appsettings

`Jwt:Issuer`, `Jwt:Audience` y `Jwt:ExpirationMinutes` no son secretos y siguen
versionados.

La connection string de desarrollo también sigue en `appsettings*.json`: apunta a
LocalDB con `Trusted_Connection=True`, o sea que no contiene credenciales. Para
apuntar a otro servidor, sobreescríbela por entorno en vez de editar el archivo:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=...;User Id=...;Password=..."
```

**Ninguna connection string con usuario y contraseña debe commitearse.**

## Pendiente

La clave que estuvo en el historial de git seguirá siendo visible ahí. Como nunca
se usó fuera de desarrollo local, no hay que rotar nada en producción — pero si
algún despliegue llegó a usarla, hay que rotar la clave y invalidar los tokens
emitidos.
