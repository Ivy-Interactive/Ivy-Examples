using SmartFormat.Utilities;

namespace IvySmartFormat
{
    /// <summary>Provides a simple dictionary-based localization for SmartFormat.</summary>
    public class DictLocalizationProvider : ILocalizationProvider
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations;

        public DictLocalizationProvider()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>()
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["You selected"] = "You selected {Value:list:{}|, | and }",
                    ["How would you like to be notified?"] = "How would you like to be notified?",
                    ["Push Notification"] = "Push Notification",
                    ["Phone"] = "Phone",
                    ["Select Language:"] = "Select Language:",
                    ["You have"] = "You {0:cond:don't|} have {0:plural:any new messages|1 message|{0} messages}.",
                    ["Change the number:"] = "Change the number:",
                    ["Login with:"] = "Login with:",
                    ["Hello"] = "Hello, {FirstName} {LastName}! You are logged in as {IsAdmin:choose(true|false):administrator|a user}.",
                    ["Select products:"] = "Select products:",
                    ["Select currency:"] = "Select currency:",
                },
                ["sv"] = new Dictionary<string, string>
                {
                    ["You selected"] = "Du har valt {Value:list:{}|, | och }",
                    ["How would you like to be notified?"] = "Hur vill du bli meddelad?",
                    ["Push Notification"] = "Push-notis",
                    ["Phone"] = "Telefon",
                    ["Select Language:"] = "Välj språk:",
                    ["You have"] = "Du {0:cond:har inte|} {0:plural:några nya meddelanden|1 meddelande|{0} meddelanden}.",
                    ["Change the number:"] = "Ändra numret:",
                    ["Login with:"] = "Logga in med:",
                    ["Hello"] = "Hej, {FirstName} {LastName}! Du är inloggad som {IsAdmin:choose(true|false):administratör|användare}.",
                    ["Select products:"] = "Välj produkter:",
                    ["Select currency:"] = "Välj valuta:",
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["You selected"] = "Has seleccionado {Value:list:{}|, | y }",
                    ["How would you like to be notified?"] = "¿Cómo te gustaría recibir notificaciones?",
                    ["Push Notification"] = "Notificación Push",
                    ["Phone"] = "Teléfono",
                    ["Select Language:"] = "Seleccionar idioma:",
                    ["You have"] = "Tienes {0:cond:no|} {0:plural:algún mensaje nuevo|1 mensaje|{0} mensajes}.",
                    ["Change the number:"] = "Cambiar el número:",
                    ["Login with:"] = "Iniciar sesión con:",
                    ["Hello"] = "Hola, {FirstName} {LastName}! Has iniciado sesión como {IsAdmin:choose(true|false):administrador|usuario}.",
                    ["Select products:"] = "Seleccionar productos:",
                    ["Select currency:"] = "Seleccionar moneda:",
                }
            };
        }

        public string? GetString(string name)
        {
            return GetTranslation(name, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        }

        public string? GetString(string name, string cultureName)
        {
            return GetTranslation(name, cultureName);
        }

        public string? GetString(string name, CultureInfo cultureInfo)
        {
            return GetTranslation(name, cultureInfo.TwoLetterISOLanguageName);
        }

        private string? GetTranslation(string name, string cultureName)
        {
            if (!_translations.TryGetValue(cultureName, out var entry)) return null;
            return entry.TryGetValue(name, out var localized) ? localized : null;
        }
    }
}
