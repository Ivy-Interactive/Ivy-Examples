namespace YamlDotNetExample;

[App(icon: Icons.Code, title: "YAML Deserialization")]
public class DeserializationApp : ViewBase
{
    public override object? Build()
    {
        var yamlInput = this.UseState(@"name: George Washington
age: 89
height_in_inches: 5.75
addresses:
  home:
    street: 400 Mockingbird Lane
    city: Louaryland
    state: Hawidaho
    zip: 99970
  work:
    street: 1600 Pennsylvania Avenue NW
    city: Washington
    state: District of Columbia
    zip: 20500");

        var personOutput = this.UseState<string>();
        var errorMessage = this.UseState<string>();
        var resultOutput = this.UseState<string>();

        return Layout.Vertical().Align(Align.TopCenter)
            | (Layout.Vertical().Width(Size.Fraction(0.7f))
            | Text.H2("Deserialize YAML to a Person Object")
            | Text.Muted("Edit the YAML below to see how YamlDotNet deserializes it into a Person object:")
            | new Card(
                Layout.Vertical().Gap(4).Padding(2)
                | (Layout.Horizontal().Gap(4)
                    | Text.Label("YAML Input").Width(Size.Full())
                    | Text.Label("Person Object").Width(Size.Full()))

                | (Layout.Horizontal().Gap(4)
                    | yamlInput.ToCodeInput()
                        .Width(Size.Full())
                        .Height(Size.Auto())
                        .Placeholder("Enter your YAML here...")

                    | resultOutput.ToCodeInput()
                        .Width(Size.Full())
                        .Height(Size.Auto())
                        .ShowCopyButton(string.IsNullOrEmpty(errorMessage.Value)))

                // Convert Button
                | new Button("Deserialize to Person")
                    .HandleClick(() => DeserializeToPerson(yamlInput.Value, personOutput, errorMessage, resultOutput))
                )

            | Text.Small("This demo uses YamlDotNet library to deserialize YAML into Person objects.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [YamlDotNet](https://github.com/aaubry/YamlDotNet)"));
    }

    private void DeserializeToPerson(string yamlInput, IState<string> personOutput, IState<string> errorMessage, IState<string> resultOutput)
    {
        try
        {
            errorMessage.Value = string.Empty;

            // Create deserializer with UnderscoredNamingConvention
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            // First parse YAML as dictionary to check which fields exist
            var yamlDict = deserializer.Deserialize<Dictionary<object, object>>(yamlInput);
            
            // Then deserialize to Person object
            var person = deserializer.Deserialize<Person>(yamlInput);

            // Format the Person object as C# code, only including fields present in YAML
            var formattedPerson = FormatPersonAsCode(person, yamlDict);
            personOutput.Value = formattedPerson;
            resultOutput.Value = formattedPerson;
        }
        catch (Exception ex)
        {
            errorMessage.Value = ex.Message;
            resultOutput.Value = $"Error: {ex.Message}";
            personOutput.Value = string.Empty;
        }
    }

    private string FormatPersonAsCode(Person person, Dictionary<object, object> yamlDict)
    {
        var lines = new List<string> { "{" };
        var personFields = new List<string>();
        
        // Check if name exists in YAML
        if (yamlDict.ContainsKey("name") && yamlDict["name"] != null && !string.IsNullOrWhiteSpace(person.Name))
            personFields.Add($"    Name = \"{person.Name}\"");
        
        // Check if age exists in YAML
        if (yamlDict.ContainsKey("age") && yamlDict["age"] != null)
            personFields.Add($"    Age = {person.Age}");
        
        // Check if height_in_inches exists in YAML
        if (yamlDict.ContainsKey("height_in_inches") && yamlDict["height_in_inches"] != null)
            personFields.Add($"    HeightInInches = {person.HeightInInches}f");
        
        // Check if addresses exist in YAML
        if (yamlDict.ContainsKey("addresses") && yamlDict["addresses"] != null)
        {
            var addressesDict = yamlDict["addresses"] as Dictionary<object, object>;
            if (addressesDict != null && addressesDict.Count > 0 && person.Addresses != null && person.Addresses.Count > 0)
            {
                var addressLines = new List<string>();
                addressLines.Add("    Addresses = new Dictionary<string, Address>");
                addressLines.Add("    {");
                
                foreach (var address in person.Addresses)
                {
                    // Check if this address key exists in YAML
                    if (addressesDict.ContainsKey(address.Key) && addressesDict[address.Key] != null)
                    {
                        var addressDict = addressesDict[address.Key] as Dictionary<object, object>;
                        if (addressDict != null)
                        {
                            var addrFields = new List<string>();
                            
                            // Only include fields that exist in YAML address dict
                            if (addressDict.ContainsKey("street") && addressDict["street"] != null && 
                                !string.IsNullOrWhiteSpace(address.Value.Street))
                                addrFields.Add($"            Street = \"{address.Value.Street}\"");
                            if (addressDict.ContainsKey("city") && addressDict["city"] != null && 
                                !string.IsNullOrWhiteSpace(address.Value.City))
                                addrFields.Add($"            City = \"{address.Value.City}\"");
                            if (addressDict.ContainsKey("state") && addressDict["state"] != null && 
                                !string.IsNullOrWhiteSpace(address.Value.State))
                                addrFields.Add($"            State = \"{address.Value.State}\"");
                            if (addressDict.ContainsKey("zip") && addressDict["zip"] != null && 
                                !string.IsNullOrWhiteSpace(address.Value.Zip))
                                addrFields.Add($"            Zip = \"{address.Value.Zip}\"");
                            
                            if (addrFields.Count > 0)
                            {
                                addressLines.Add($"        {{\"{address.Key}\", new Address");
                                addressLines.Add("        {");
                                
                                // Add fields with commas except the last one
                                for (int i = 0; i < addrFields.Count; i++)
                                {
                                    addressLines.Add(addrFields[i] + (i < addrFields.Count - 1 ? "," : ""));
                                }
                                
                                addressLines.Add("        }},");
                            }
                        }
                    }
                }
                
                if (addressLines.Count > 2) // More than just opening lines
                {
                    addressLines.Add("    }");
                    personFields.Add(string.Join("\n", addressLines));
                }
            }
        }
        
        // Add person fields with commas except the last one
        for (int i = 0; i < personFields.Count; i++)
        {
            lines.Add(personFields[i] + (i < personFields.Count - 1 ? "," : ""));
        }
        
        lines.Add("};");
        return string.Join("\n", lines) + "\n";
    }
}
