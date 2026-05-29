# Paynest

Paynest es una aplicacion movil desarrollada con .NET MAUI para gestionar flujos de cobranza entre dos tipos de usuario:

- **Cliente:** consulta deudas, cuotas, calendario, saldo interno, pagos y recibos.
- **Cobrador:** gestiona clientes, deudas, cobros, agenda, pagos y perfil.

La app consume una API backend mediante endpoints protegidos con autenticacion, mantiene sesion local y puede trabajar contra backend real o con mocks para desarrollo de interfaz.

## Tecnologias

- .NET MAUI
- C#
- XAML
- MVVM
- Android / iOS
- HTTP APIs
- SecureStorage / Preferences
- CommunityToolkit.Mvvm

El SDK usado por el proyecto esta fijado en `global.json`:

```json
{
  "sdk": {
    "version": "10.0.203"
  }
}
```

## Estructura principal

```text
Features/
  Auth/        Login, registro y recuperacion de password
  Client/      Flujo del cliente/deudor
  Cobrador/    Flujo del cobrador/freelancer
  Onboarding/  Pantallas de perfil/onboarding
  Splash/      Restauracion de sesion inicial

Core/          Interfaces, modelos y validaciones
Infrastructure/ Clientes HTTP y constantes de API
Services/      Servicios compartidos de sesion, refresco y recibos
Resources/     Imagenes, fuentes y assets
Platforms/     Configuracion por plataforma
```

La arquitectura recomendada esta documentada en [ARCHITECTURE.md](ARCHITECTURE.md).

## Configuracion local

El proyecto usa `Paynest.Local.props` para definir URLs de backend y activar/desactivar mocks.

Si no existe, crea el archivo desde el ejemplo:

```bash
cp Paynest.Local.props.example Paynest.Local.props
```

Ejemplo:

```xml
<Project>
  <PropertyGroup>
    <PaynestAndroidEmulatorBaseUrl>http://10.0.2.2:5080</PaynestAndroidEmulatorBaseUrl>
    <PaynestiOSSimulatorBaseUrl>http://localhost:5080</PaynestiOSSimulatorBaseUrl>
    <PaynestPhysicalDeviceBaseUrl>http://192.168.0.145:5080</PaynestPhysicalDeviceBaseUrl>
    <PaynestBaseUrl>http://192.168.0.145:5080</PaynestBaseUrl>
    <PaynestUseMocks>false</PaynestUseMocks>
  </PropertyGroup>
</Project>
```

Notas:

- En Android Emulator, `10.0.2.2` apunta al `localhost` de la computadora.
- En dispositivo fisico, usa la IP LAN de la computadora donde corre el backend.
- Para usar backend real, deja `PaynestUseMocks` en `false`.
- Para pruebas visuales sin backend, puedes usar `PaynestUseMocks=true`.

## Ejecutar en Android Emulator

Con el backend corriendo en la maquina local:

```bash
dotnet build Paynest.csproj -f net10.0-android -v:minimal
```

Para instalar y correr desde CLI:

```bash
dotnet build Paynest.csproj -f net10.0-android -t:Run -v:minimal
```

## Generar APK

APK Debug instalable:

```bash
dotnet build Paynest.csproj -f net10.0-android -c Debug -p:AndroidPackageFormat=apk -p:PaynestUseMocks=false -v:minimal
```

El APK suele quedar en:

```text
bin/Debug/net10.0-android/com.jairemiliano.paynest-Signed.apk
```

APK Release:

```bash
dotnet publish Paynest.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=apk -p:PaynestUseMocks=false -v:minimal
```

## Flujos principales

### Cliente

- Login y registro.
- Persistencia de sesion.
- Vinculacion con cobrador por codigo o QR.
- Consulta de deudas activas.
- Consulta de cuotas y calendario.
- Wallet interna / Saldo Paynest.
- Abono de saldo.
- Pago de cuotas.
- Consulta de recibos.
- Configuracion de recordatorios.

### Cobrador

- Login y registro.
- Panel principal.
- Generacion de codigo/QR para vincular clientes.
- Gestion de clientes.
- Creacion de deudas.
- Registro de pagos.
- Consulta de cobros.
- Agenda.
- Perfil y cierre de sesion.

## Contratos relevantes

### Vincular cobrador desde cliente

```http
POST /api/v1/client/collectors/link
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "collectorCode": "PAY-X7XTM5"
}
```

La app tambien acepta QR con formato:

```text
paynest://collector/link?code=PAY-X7XTM5
```

### Obtener invitacion del cobrador

```http
GET /api/v1/collector/invite
Authorization: Bearer <token>
```

Respuesta esperada:

```json
{
  "collectorId": "string",
  "collectorCode": "PAY-X7XTM5",
  "code": "PAY-X7XTM5",
  "qrPayload": "paynest://collector/link?code=PAY-X7XTM5",
  "expiresAt": null,
  "createdAt": "datetime",
  "status": "active"
}
```

## Autenticacion y sesion

La app usa `AuthStateService` para:

- Guardar `accessToken` y `refreshToken`.
- Restaurar sesion al abrir la app.
- Refrescar token cuando es necesario.
- Redirigir al login al cerrar sesion.
- Construir el shell principal segun el rol del usuario.

## Mocks

Los mocks estan disponibles para desarrollo visual, pero por defecto el proyecto queda conectado a backend real.

Punto de configuracion:

```xml
<PaynestUseMocks>false</PaynestUseMocks>
```

## Comandos utiles

Limpiar build:

```bash
dotnet clean Paynest.csproj -f net10.0-android
```

Compilar Android:

```bash
dotnet build Paynest.csproj -f net10.0-android -v:minimal
```

Ver workloads instalados:

```bash
dotnet workload list
```

## Estado del proyecto

El proyecto ya cuenta con:

- Flujo cliente conectado a backend.
- Flujo cobrador conectado a backend.
- Persistencia de sesion.
- Vinculacion cliente/cobrador.
- Gestion de deudas y pagos.
- Wallet interna.
- Recibos.
- Estados de carga, vacios y error.
- UI adaptada para cliente y cobrador.

