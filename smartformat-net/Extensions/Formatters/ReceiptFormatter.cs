using IvySmartFormat.Apps;
using SmartFormat;
using SmartFormat.Core.Extensions;
using System.Text;

/// <summary>Custom SmartFormat formatter that generates a detailed receipt for an Order object.</summary>
public class ReceiptFormatter : IFormatter
{
    private const string SEPARATOR = "--------------------------------";
    public string Name => "receipt";
    string IFormatter.Name { get => Name; set { } }
    public bool CanAutoDetect { get; set; } = false;

    public bool TryEvaluateFormat(IFormattingInfo info)
    {

        if (info.CurrentValue is not Order order)
            return false;

        var addressParts = order.Address.Split(',');
        var receipt = new StringBuilder();
        receipt.AppendLine(SEPARATOR);
        receipt.AppendLine($"----------- RECEIPT ----------");
        receipt.AppendLine(SEPARATOR);
        receipt.AppendLine($"Date: {order.OrderDate:yyyy-MM-dd HH:mm}");
        receipt.AppendLine($"Customer: {order.CustomerName}");
        receipt.AppendLine($"Address: {addressParts[0]},");
        receipt.AppendLine($"              {addressParts[1]},{addressParts[2]}");
        receipt.AppendLine($"Phone: +{Mask(order.Phone.ToString())}");
        receipt.AppendLine(SEPARATOR);
        receipt.AppendLine("Items:\n");

        decimal total = 0;
        foreach (var item in order.Items)
        {
            decimal itemTotal = item.Quantity * item.Price;
            total += itemTotal;
            receipt.AppendLine($"{item.Name} x{item.Quantity}  ");
            receipt.AppendLine($"             {FormatCurrency(item.Price, order.Culture)} => {FormatCurrency(itemTotal, order.Culture)}\n");
        }
        if(total == 0)
        {
            receipt.AppendLine("  🛒🛒🛒🛒🛒🛒🛒🛒🛒\n");
        }
        receipt.AppendLine(SEPARATOR);
        receipt.AppendLine($"                     TOTAL: {FormatCurrency(total, order.Culture)}");
        receipt.AppendLine(SEPARATOR);
        receipt.AppendLine(SEPARATOR);
        receipt.AppendLine("-               Thank you!                -");
        receipt.AppendLine(SEPARATOR);

        info.Write(receipt.ToString());
        return true;
    }
    private string FormatCurrency(decimal amount, string culture)
    {
        try
        {
            return Smart.Format(CultureInfo.GetCultureInfo(culture), "{0:C}", amount);
        }
        catch (CultureNotFoundException)
        {
            return amount.ToString("C"); // Fallback to default culture
        }
    }
    private static string Mask(string text)
    {
        return text.Length <= 2 ? new string('*', text.Length) : text[0] + new string('*', text.Length - 2) + text[^1];
    }
}
