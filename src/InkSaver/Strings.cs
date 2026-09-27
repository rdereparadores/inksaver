using System.Globalization;

namespace InkSaver;

/// <summary>User-facing texts. Spanish when Windows is in Spanish, English otherwise.</summary>
internal static class Strings
{
    private static readonly bool Spanish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es";

    private static string T(string es, string en) => Spanish ? es : en;

    public const string AppName = "InkSaver";

    public static string AlreadyRunning => T(
        "InkSaver ya se está ejecutando. Búscalo en el área de notificación de la barra de tareas.",
        "InkSaver is already running. Look for it in the taskbar notification area.");

    // --- Tray menu -------------------------------------------------------------------------------

    public static string NextPrint(DateTime due) => T($"Próxima impresión: {due:D}", $"Next print: {due:D}");

    public static string PrintPending => T("Impresión pendiente: se hará en breve", "Print pending: it will run shortly");

    public static string PrintingNow => T("Imprimiendo…", "Printing…");

    public static string PausedStatus => T("Impresión automática en pausa", "Automatic printing paused");

    public static string LastPrint(DateTimeOffset? last) => last is { } value
        ? T($"Última impresión: {value.LocalDateTime:g}", $"Last print: {value.LocalDateTime:g}")
        : T("Última impresión: nunca", "Last print: never");

    public static string PrintNow => T("Imprimir ahora", "Print now");

    public static string PrinterMenu => T("Impresora", "Printer");

    public static string DefaultPrinter(string? name) => string.IsNullOrEmpty(name)
        ? T("Predeterminada de Windows (ninguna)", "Windows default (none)")
        : T($"Predeterminada de Windows ({name})", $"Windows default ({name})");

    public static string UnavailablePrinter(string name) => T($"{name} (no disponible)", $"{name} (not available)");

    public static string NoPrintersInstalled => T("No hay impresoras instaladas", "No printers installed");

    public static string FrequencyMenu => T("Frecuencia", "Frequency");

    public static string EveryDays(int days) => days == 1
        ? T("Cada día", "Every day")
        : T($"Cada {days} días", $"Every {days} days");

    public static string CustomFrequency => T("Personalizada…", "Custom…");

    public static string CustomFrequencySelected(int days) => T(
        $"Personalizada: cada {days} días…", $"Custom: every {days} days…");

    public static string SourceMenu => T("Qué imprimir", "What to print");

    public static string GeneratedPage => T("Página de prueba de color", "Colour test page");

    public static string ChoosePdf => T("Archivo PDF…", "PDF file…");

    public static string PdfSelected(string fileName) => T($"PDF: {fileName}", $"PDF: {fileName}");

    public static string Preview => T("Vista previa…", "Preview…");

    public static string PauseAutomaticPrinting => T("Pausar impresión automática", "Pause automatic printing");

    public static string StartWithWindows => T("Iniciar con Windows", "Start with Windows");

    public static string ShowHistory => T("Ver historial", "Show history");

    public static string Exit => T("Salir", "Exit");

    // --- Notifications ---------------------------------------------------------------------------

    public static string WelcomeTitle => T("InkSaver está activo", "InkSaver is running");

    public static string WelcomeBody(int days, string printer) => T(
        $"Imprimirá una página {EveryDaysLower(days)} en «{printer}». Haz clic en este icono para configurarlo.",
        $"It will print one page {EveryDaysLower(days)} on \"{printer}\". Click this icon to configure it.");

    public static string PendingTitle => T("Impresión de mantenimiento pendiente", "Maintenance print pending");

    public static string PendingBody(int minutes, string printer) => T(
        $"Toca imprimir la página de mantenimiento. Se imprimirá en {minutes} minutos en «{printer}».",
        $"The maintenance page is due. It will print in {minutes} minutes on \"{printer}\".");

    public static string PrintedTitle => T("Página de mantenimiento enviada", "Maintenance page sent");

    public static string PrintedBody(string printer, DateTime next) => T(
        $"Enviada a «{printer}». Próxima impresión: {next:D}.",
        $"Sent to \"{printer}\". Next print: {next:D}.");

    public static string PrintFailedTitle => T("No se pudo imprimir", "Printing failed");

    public static string RetryIn(int minutes) => T(
        $"Se volverá a intentar en {minutes} minutos.", $"It will be retried in {minutes} minutes.");

    public static string SettingsSaveFailed(string message) => T(
        $"No se pudo guardar la configuración: {message}", $"Settings could not be saved: {message}");

    public static string StartupChangeFailed(string message) => T(
        $"No se pudo cambiar el inicio con Windows: {message}", $"Could not change start with Windows: {message}");

    // --- Errors ----------------------------------------------------------------------------------

    public static string NoDefaultPrinter => T(
        "No hay ninguna impresora predeterminada en Windows. Elige una impresora en el menú de InkSaver.",
        "Windows has no default printer. Choose a printer in the InkSaver menu.");

    public static string PrinterNotFound(string name) => T(
        $"La impresora «{name}» no está instalada o no está disponible.",
        $"The printer \"{name}\" is not installed or not available.");

    public static string PdfNotChosen => T("No se ha elegido ningún PDF.", "No PDF file has been chosen.");

    public static string PdfNotFound(string path) => T($"No se encuentra el PDF «{path}».", $"PDF file \"{path}\" not found.");

    public static string PdfHasNoPages => T("El PDF no tiene páginas.", "The PDF has no pages.");

    public static string PdfUnreadable(string message) => T(
        $"No se pudo abrir el PDF (¿está protegido con contraseña o dañado?). {message}",
        $"The PDF could not be opened (is it password protected or damaged?). {message}");

    // --- Dialogs ---------------------------------------------------------------------------------

    public static string IntervalDialogTitle => T("Frecuencia de impresión", "Print frequency");

    public static string IntervalDialogPrompt => T("Imprimir una página cada", "Print one page every");

    public static string DaysUnit => T("días", "days");

    public static string Ok => T("Aceptar", "OK");

    public static string Cancel => T("Cancelar", "Cancel");

    public static string ChoosePdfTitle => T("Elige el PDF que se imprimirá", "Choose the PDF to print");

    public static string PdfFilter => T("Documentos PDF (*.pdf)|*.pdf", "PDF documents (*.pdf)|*.pdf");

    public static string PreviewTitle => T("InkSaver - Vista previa", "InkSaver - Preview");

    // --- Generated test page ---------------------------------------------------------------------

    public static string TestPageTitle => T("Página de mantenimiento de la impresora", "Printer maintenance page");

    public static string TestPagePrintedOn(DateTime when, string printer) => T(
        $"Impresa el {when:D} a las {when:t} en «{printer}»",
        $"Printed on {when:D} at {when:t} on \"{printer}\"");

    public static string TestPagePrimaryColours => T("Colores de los cartuchos (CMYK)", "Cartridge colours (CMYK)");

    public static string TestPageSecondaryColours => T("Mezclas (RGB)", "Mixes (RGB)");

    public static string TestPageGradients => T("Degradados", "Gradients");

    public static string TestPageNozzles => T("Patrón de inyectores", "Nozzle pattern");

    public static string TestPageFooter => T(
        "InkSaver imprime esta página periódicamente para que la tinta no se seque en los cabezales.",
        "InkSaver prints this page periodically so the ink does not dry out in the print head.");

    private static string EveryDaysLower(int days) => days == 1
        ? T("cada día", "every day")
        : T($"cada {days} días", $"every {days} days");
}
