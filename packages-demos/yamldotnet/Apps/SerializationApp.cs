using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Models;

namespace YamlDotNet.Apps;

[App(icon: Icons.Code, title: "YAML Serialization")]
public class SerializationApp : ViewBase
{
    public override object? Build()
    {
        var personCode = this.UseState(@"Name = ""Abe Lincoln"",
Age = 25,
HeightInInches = 6f + 4f / 12f,
Addresses = new Dictionary<string, Address> {
    { ""home"", new Address {
        Street = ""2720 Sundown Lane"",
        City = ""Kentucketsville"",
        State = ""Calousiyorkida"",
        Zip = ""99978""
    }},
    { ""work"", new Address {
        Street = ""1600 Pennsylvania Avenue NW"",
        City = ""Washington"",
        State = ""District of Columbia"",
        Zip = ""20500""
    }}
}");

        var yamlOutput = this.UseState<string>();
        var errorMessage = this.UseState<string>();

        return Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Serialize a Person Object to YAML")
            | Text.Block("Edit the C# Person object below to see how YamlDotNet serializes it into YAML.")
            | new Separator()

            | (Layout.Horizontal().Gap(4)
                | Text.H3("C# Person Code").Width(Size.Full())
                | Text.H3("YAML Output").Width(Size.Full()))

            | (Layout.Horizontal().Gap(4).Padding(2)
                | personCode.ToCodeInput()
                    .Width(Size.Full())
                    .Height(Size.Auto())
                    .Language(Languages.Csharp)
                    .Placeholder("Enter your Person C# code here...")

                | new Separator()

                | (string.IsNullOrEmpty(errorMessage.Value)
                    ? yamlOutput.ToCodeInput()
                        .Width(Size.Full())
                        .Height(Size.Auto())
                        .ShowCopyButton(true)
                    : Text.Block($"Error: {errorMessage.Value}")
                        .Color(Colors.Red)))

            // Convert Button
            | new Button("Convert to YAML")
                .HandleClick(() => ConvertToYaml(personCode.Value, yamlOutput, errorMessage))

            | new Separator()
            | Text.Small("This demo uses YamlDotNet library to serialize Person objects to YAML format.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [YamlDotNet](https://github.com/aaubry/YamlDotNet)");
    }

    private void ConvertToYaml(string personCode, IState<string> yamlOutput, IState<string> errorMessage)
    {
        try
        {
            errorMessage.Value = string.Empty;

            // Parse the user input to create Person object
            var person = ParsePersonCode(personCode);

            // Serialize to YAML using YamlDotNet
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            yamlOutput.Value = serializer.Serialize(person);
        }
        catch (Exception ex)
        {
            errorMessage.Value = $"Error: {ex.Message}";
            yamlOutput.Value = string.Empty;
        }
    }

    private Person ParsePersonCode(string code)
    {
        // Simple parsing - in real app you might want more robust parsing
        var person = new Person();
        
        // Extract Name
        var nameMatch = System.Text.RegularExpressions.Regex.Match(code, @"Name\s*=\s*""([^""]+)""");
        if (nameMatch.Success)
            person.Name = nameMatch.Groups[1].Value;

        // Extract Age
        var ageMatch = System.Text.RegularExpressions.Regex.Match(code, @"Age\s*=\s*(\d+)");
        if (ageMatch.Success && int.TryParse(ageMatch.Groups[1].Value, out int age))
            person.Age = age;

        // Extract HeightInInches
        var heightMatch = System.Text.RegularExpressions.Regex.Match(code, @"HeightInInches\s*=\s*([^,}]+)");
        if (heightMatch.Success)
        {
            var heightExpression = heightMatch.Groups[1].Value.Trim();
            try
            {
                // Try to evaluate the expression
                var height = EvaluateExpression(heightExpression);
                person.HeightInInches = height;
            }
            catch
            {
                // If evaluation fails, try to parse as a simple number
                if (float.TryParse(heightExpression, out float simpleHeight))
                    person.HeightInInches = simpleHeight;
            }
        }

        // Extract Addresses (simplified parsing)
        person.Addresses = new Dictionary<string, Address>();
        
        // Extract home address
        var homeAddress = ExtractAddress(code, "home");
        if (homeAddress != null)
            person.Addresses["home"] = homeAddress;

        // Extract work address  
        var workAddress = ExtractAddress(code, "work");
        if (workAddress != null)
            person.Addresses["work"] = workAddress;

        return person;
    }

    private Address? ExtractAddress(string code, string addressType)
    {
        // Simple approach - find the address block
        var addressBlockPattern = $@"""{addressType}"",\s*new\s+Address\s*\{{([^}}]*)\}}";
        var addressBlockMatch = System.Text.RegularExpressions.Regex.Match(code, addressBlockPattern);
        
        if (!addressBlockMatch.Success) return null;
        
        var addressBlock = addressBlockMatch.Groups[1].Value;
        
        // Simple patterns for each field
        var street = ExtractProperty(addressBlock, "Street");
        var city = ExtractProperty(addressBlock, "City");
        var state = ExtractProperty(addressBlock, "State");
        var zip = ExtractProperty(addressBlock, "Zip");
        
        return new Address
        {
            Street = street ?? "",
            City = city ?? "",
            State = state ?? "",
            Zip = zip ?? ""
        };
    }

    private string? ExtractProperty(string block, string propertyName)
    {
        var pattern = $@"{propertyName}\s*=\s*""([^""]+)""";
        var match = System.Text.RegularExpressions.Regex.Match(block, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    private float EvaluateExpression(string expression)
    {
        expression = expression.Replace("f", "").Replace("F", "").Trim();

        var dataTable = new System.Data.DataTable();
        var result = dataTable.Compute(expression, null);
        return Convert.ToSingle(result);
    }
}
