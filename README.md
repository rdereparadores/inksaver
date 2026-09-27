# InkSaver

InkSaver es una pequeña aplicación para Windows que vive en el área de notificación
(junto al reloj) e imprime automáticamente una página cada *X* días en la impresora
que elijas. Así los cartuchos de las impresoras de inyección de tinta no se secan
aunque pases semanas sin imprimir nada.

## Características

- **Solo en la bandeja del sistema**: toda la configuración se hace desde el menú del
  icono (clic izquierdo o derecho).
- **Frecuencia configurable**: cada 1, 2, 3, 4, 5, 7, 10, 14, 21 o 30 días, o un valor
  personalizado entre 1 y 365 días.
- **Impresora configurable**: la predeterminada de Windows o cualquiera de las instaladas.
- **Qué imprimir**:
  - una *página de prueba de color* generada por InkSaver (bloques C, M, Y, K, mezclas
    RGB, degradados y un patrón de líneas finas para que trabajen todos los inyectores), o
  - la **primera página de un PDF** elegido por ti.
  - *Vista previa…* muestra exactamente lo que se va a imprimir.
- **Impresiones pendientes**: si el equipo estaba apagado (o suspendido) el día que tocaba,
  la página se imprime la próxima vez que se inicie, **2 minutos después del arranque**
  (para dar tiempo a que la impresora y la red estén listas). Se muestra un aviso antes.
- **Imprimir ahora**: imprime al momento y reinicia la cuenta de días.
- **Pausar** la impresión automática sin cerrar la aplicación (el icono se vuelve gris).
- **Iniciar con Windows** (activado por defecto la primera vez; se puede desactivar
  desde el menú). No requiere permisos de administrador.
- **Historial** de impresiones y errores (*Ver historial*).
- Interfaz en **español** o **inglés** según el idioma de Windows.
- Si una impresión automática falla (impresora desinstalada, PDF borrado…) se avisa con
  una notificación y se reintenta cada 30 minutos.

## Cómo se cuentan los días

Los intervalos son de **días naturales**: con una frecuencia de 3 días y una impresión el
lunes, la siguiente toca a partir del jueves a las 00:00. Si ese día el equipo está
apagado, se imprimirá en cuanto se vuelva a encender. Hasta la primera impresión, la
cuenta empieza el día en que se ejecutó InkSaver por primera vez.

## Instalación

1. Descarga `InkSaver.exe` de la sección *Releases* (o del artefacto `InkSaver-win-x64`
   de la última ejecución de *Actions*).
2. Colócalo en una carpeta permanente, por ejemplo `%LOCALAPPDATA%\InkSaver\`.
3. Ejecútalo. Aparecerá una gota de tinta en el área de notificación (puede estar
   dentro de la flecha ^ de iconos ocultos; puedes arrastrarla a la barra).

Es un único ejecutable autocontenido: no necesita instalar .NET. Requiere Windows 10
(1809) o posterior, o Windows 11.

Si mueves el `.exe` de carpeta, vuelve a ejecutarlo desde la nueva ubicación y el inicio
con Windows se actualizará solo.

### Desinstalar

Desmarca *Iniciar con Windows*, pulsa *Salir* y borra el `.exe` y la carpeta
`%APPDATA%\InkSaver`.

## Dónde guarda los datos

`%APPDATA%\InkSaver\`

- `settings.json`: configuración y fecha de la última impresión.
- `inksaver.log`: historial.

El inicio con Windows se registra en
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (valor `InkSaver`).

## Compilar

Necesitas el SDK de .NET 8.

```bash
# Tests
dotnet test InkSaver.sln

# Ejecutable único para Windows x64 -> src/InkSaver/bin/publish/win-x64/InkSaver.exe
dotnet publish src/InkSaver/InkSaver.csproj -p:PublishProfile=win-x64
```

El proyecto también compila desde Linux/macOS (`EnableWindowsTargeting`), pero la
aplicación y el proyecto `tests/InkSaver.Tests` solo se ejecutan en Windows; en esos
sistemas usa `dotnet test tests/InkSaver.Core.Tests`.

Cada *push* compila y prueba el proyecto en GitHub Actions; al subir una etiqueta `v*`
(por ejemplo `v1.0.0`) se crea una *release* con el `.exe` adjunto.

### Estructura

| Ruta | Contenido |
| --- | --- |
| `src/InkSaver.Core` | Configuración (`AppSettings`, `SettingsStore`) y reglas de programación (`Schedule`). Sin dependencias de Windows. |
| `src/InkSaver` | Aplicación WinForms: icono y menú de bandeja, impresión, página de prueba, renderizado de PDF (motor PDF integrado en Windows, `Windows.Data.Pdf`), inicio con Windows. |
| `tests/InkSaver.Core.Tests` | Tests multiplataforma de la lógica. |
| `tests/InkSaver.Tests` | Tests que requieren Windows (GDI+, PDF). |
| `tools/make_icons.py` | Genera los iconos (`python tools/make_icons.py`, requiere Pillow). |
