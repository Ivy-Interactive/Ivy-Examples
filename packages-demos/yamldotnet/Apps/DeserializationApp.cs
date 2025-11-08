using YamlDotNet.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YamlDotNet.Apps;

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

        return Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Deserialize YAML to a Person Object")
            | Text.Block("Edit the YAML below to see how YamlDotNet deserializes it into a Person object:")
            | new Separator()

            | (Layout.Horizontal().Gap(4)
                | Text.H3("YAML Input").Width(Size.Full())
                | Text.H3("Person Object").Width(Size.Full()))

            | (Layout.Horizontal().Gap(4).Padding(2)
                | yamlInput.ToCodeInput()
                    .Width(Size.Full())
                    .Height(Size.Auto())
                    .Placeholder("Enter your YAML here...")

                | new Separator()

                | (string.IsNullOrEmpty(errorMessage.Value)
                    ? personOutput.ToCodeInput()
                        .Width(Size.Full())
                        .Height(Size.Auto())
                        .ShowCopyButton(true)
                    : Text.Block($"Error: {errorMessage.Value}")
                        .Color(Colors.Red)))

            // Convert Button
            | new Button("Deserialize to Person")
                .HandleClick(() => DeserializeToPerson(yamlInput.Value, personOutput, errorMessage))

            | new Separator()
            | Text.Small("This demo uses YamlDotNet library to deserialize YAML into Person objects.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [YamlDotNet](https://github.com/aaubry/YamlDotNet)");
    }

    private void DeserializeToPerson(string yamlInput, IState<string> personOutput, IState<string> errorMessage)
    {
        try
        {
            errorMessage.Value = string.Empty;

            // Create deserializer with UnderscoredNamingConvention
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            // Deserialize YAML to Person object
            var person = deserializer.Deserialize<Person>(yamlInput);

            // Format the Person object as C# code
            var formattedPerson = FormatPersonAsCode(person);
            personOutput.Value = formattedPerson;
        }
        catch (Exception ex)
        {
            errorMessage.Value = $"Error: {ex.Message}";
            personOutput.Value = string.Empty;
        }
    }

    private string FormatPersonAsCode(Person person)
    {
        var result = $"{{\n    Name = \"{person.Name}\",\n";
        result += $"    Age = {person.Age},\n";
        result += $"    HeightInInches = {person.HeightInInches}f,\n";
        result += $"    Addresses = new Dictionary<string, Address>\n    {{\n";

        foreach (var address in person.Addresses)
        {
            result += $"        {{\"{address.Key}\", new Address\n        {{\n";
            result += $"            Street = \"{address.Value.Street}\",\n";
            result += $"            City = \"{address.Value.City}\",\n";
            result += $"            State = \"{address.Value.State}\",\n";
            result += $"            Zip = \"{address.Value.Zip}\"\n";
            result += $"        }}}},\n";
        }

        result += "    }\n};\n\n";

        return result;
    }
}
