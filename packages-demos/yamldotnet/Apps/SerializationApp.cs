namespace YamlDotNetExample;

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
        var resultOutput = this.UseState<string>();

        return Layout.Vertical().Align(Align.TopCenter)
            | (Layout.Vertical().Width(Size.Fraction(0.7f))
            | Text.H2("Serialize a Person Object to YAML")
            | Text.Muted("Edit the C# Person object below to see how YamlDotNet serializes it into YAML.")

            | new Card(
                Layout.Vertical().Gap(4).Padding(2)
                | (Layout.Horizontal().Gap(4)
                    | Text.Label("C# Person Code").Width(Size.Full())
                    | Text.Label("YAML Output").Width(Size.Full()))

                | (Layout.Horizontal().Gap(4)
                    | personCode.ToCodeInput()
                        .Width(Size.Full())
                        .Height(Size.Auto())
                        .Language(Languages.Csharp)
                        .Placeholder("Enter your Person C# code here...")

                    | resultOutput.ToCodeInput()
                        .Width(Size.Full())
                        .Height(Size.Auto())
                        .ShowCopyButton(string.IsNullOrEmpty(errorMessage.Value)))

                // Convert Button
                | new Button("Convert to YAML")
                    .HandleClick(() => ConvertToYaml(personCode.Value, yamlOutput, errorMessage, resultOutput))
            )

            | Text.Small("This demo uses YamlDotNet library to serialize Person objects to YAML format.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [YamlDotNet](https://github.com/aaubry/YamlDotNet)"));
    }

    private void ConvertToYaml(string personCode, IState<string> yamlOutput, IState<string> errorMessage, IState<string> resultOutput)
    {
        try
        {
            errorMessage.Value = string.Empty;
            
            // Validate address keys - only "home" and "work" are allowed
            var addressKeyValidation = ValidateAddressKeys(personCode);
            if (!string.IsNullOrEmpty(addressKeyValidation))
            {
                errorMessage.Value = addressKeyValidation;
                resultOutput.Value = $"Error: {addressKeyValidation}";
                yamlOutput.Value = string.Empty;
                return;
            }
            
            var person = ParsePersonCode(personCode);
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var data = new Dictionary<string, object?>();
            
            if (System.Text.RegularExpressions.Regex.IsMatch(personCode, @"Name\s*=") && !string.IsNullOrWhiteSpace(person.Name))
                data["name"] = person.Name;
            if (System.Text.RegularExpressions.Regex.IsMatch(personCode, @"Age\s*="))
                data["age"] = person.Age;
            if (System.Text.RegularExpressions.Regex.IsMatch(personCode, @"HeightInInches\s*="))
                data["heightInInches"] = person.HeightInInches;
            
            if (person.Addresses?.Count > 0)
            {
                var addresses = new Dictionary<string, object>();
                foreach (var addr in person.Addresses)
                {
                    var addrData = new Dictionary<string, object?>();
                    if (!string.IsNullOrWhiteSpace(addr.Value.Street)) addrData["street"] = addr.Value.Street;
                    if (!string.IsNullOrWhiteSpace(addr.Value.City)) addrData["city"] = addr.Value.City;
                    if (!string.IsNullOrWhiteSpace(addr.Value.State)) addrData["state"] = addr.Value.State;
                    if (!string.IsNullOrWhiteSpace(addr.Value.Zip)) addrData["zip"] = addr.Value.Zip;
                    if (addrData.Count > 0) addresses[addr.Key] = addrData;
                }
                if (addresses.Count > 0) data["addresses"] = addresses;
            }
            
            var yaml = serializer.Serialize(data);
            yamlOutput.Value = yaml;
            resultOutput.Value = yaml;
        }
        catch (Exception ex)
        {
            errorMessage.Value = ex.Message;
            resultOutput.Value = $"Error: {ex.Message}";
            yamlOutput.Value = string.Empty;
        }
    }

    private string ValidateAddressKeys(string code)
    {
        if (!code.Contains("Addresses =")) return string.Empty;
        
        // Find all address keys in the dictionary
        var matches = System.Text.RegularExpressions.Regex.Matches(code, @"""(?<key>[^""]+)"",\s*new\s+Address");
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var key = match.Groups["key"].Value;
            if (key != "home" && key != "work")
            {
                return $"Invalid address key: '{key}'. Only 'home' and 'work' keys are allowed for Address objects.";
            }
        }
        
        return string.Empty;
    }

    private Person ParsePersonCode(string code)
    {
        var person = new Person();
        
        var m = System.Text.RegularExpressions.Regex.Match(code, @"Name\s*=\s*""([^""]+)""");
        if (m.Success) person.Name = m.Groups[1].Value;

        m = System.Text.RegularExpressions.Regex.Match(code, @"Age\s*=\s*(\d+)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out int age))
            person.Age = age;

        m = System.Text.RegularExpressions.Regex.Match(code, @"HeightInInches\s*=\s*([^,}]+)");
        if (m.Success)
        {
            var expr = m.Groups[1].Value.Trim().Replace("f", "").Replace("F", "");
            try { person.HeightInInches = Convert.ToSingle(new System.Data.DataTable().Compute(expr, null)); }
            catch { if (float.TryParse(expr, out float h)) person.HeightInInches = h; }
        }

        if (code.Contains("Addresses ="))
        {
            person.Addresses = new Dictionary<string, Address>();
            foreach (var key in new[] { "home", "work" })
            {
                var addr = ExtractAddress(code, key);
                if (addr != null && (!string.IsNullOrWhiteSpace(addr.Street) || !string.IsNullOrWhiteSpace(addr.City) || 
                    !string.IsNullOrWhiteSpace(addr.State) || !string.IsNullOrWhiteSpace(addr.Zip)))
                    person.Addresses[key] = addr;
            }
        }

        return person;
    }

    private Address? ExtractAddress(string code, string key)
    {
        var m = System.Text.RegularExpressions.Regex.Match(code, $@"""{key}"",\s*new\s+Address\s*\{{([^}}]*)\}}");
        if (!m.Success) return null;
        
        var block = m.Groups[1].Value;
        var street = ExtractProperty(block, "Street");
        var city = ExtractProperty(block, "City");
        var state = ExtractProperty(block, "State");
        var zip = ExtractProperty(block, "Zip");
        
        if (street == null && city == null && state == null && zip == null) return null;
        return new Address { Street = street ?? "", City = city ?? "", State = state ?? "", Zip = zip ?? "" };
    }

    private string? ExtractProperty(string block, string name)
    {
        var m = System.Text.RegularExpressions.Regex.Match(block, $@"{name}\s*=\s*""([^""]+)""");
        return m.Success ? m.Groups[1].Value : null;
    }
}
