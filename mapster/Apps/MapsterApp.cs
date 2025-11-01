namespace MapsterExample
{
    [App(icon: Icons.PersonStanding, title: "Mapster")]
    public class MapsterApp : ViewBase
    {
        public override object? Build()
        {
            TypeAdapterConfig.GlobalSettings.Scan(typeof(MapsterConfig).Assembly);

            var personJsonState = UseState(ToPrettyJson(new Person
            {
                FirstName = "Jane",
                LastName = "Doe",
                Age = 25
            }));

            var dtoJsonState = UseState(ToPrettyJson(new PersonDto
            {
                FullName = "Jane Doe",
                IsAdult = true
            }));

            // Helper function to validate DTO
            string? GetValidationError()
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<PersonDto>(dtoJsonState.Value);
                    if (dto != null)
                    {
                        var wordCount = dto.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        if (wordCount > 2 && dto.HasSingleWordName)
                        {
                            return "FullName contains 2 or more words but HasSingleWordName is true";
                        }
                    }
                }
                catch { }
                return null;
            }

            // Person -> PersonDto
            var toDtoButton = new Button("Person -> PersonDto")
            {
                OnClick = async (evt) =>
                {
                    try
                    {
                        var person = JsonSerializer.Deserialize<Person>(personJsonState.Value);
                        var dto = person.Adapt<PersonDto>();
                        dtoJsonState.Value = ToPrettyJson(dto);
                    }
                    catch (Exception ex)
                    {
                        dtoJsonState.Value = $"{{ \"error\": \"{ex.Message}\" }}";
                    }

                    await ValueTask.CompletedTask;
                }
            };

            // PersonDto -> Person
            var toPersonButton = new Button("PersonDto -> Person")
            {
                OnClick = async (evt) =>
                {
                    try
                    {
                        var dto = JsonSerializer.Deserialize<PersonDto>(dtoJsonState.Value);
                        var person = dto.Adapt<Person>();
                        personJsonState.Value = ToPrettyJson(person);
                    }
                    catch (Exception ex)
                    {
                        personJsonState.Value = $"{{ \"error\": \"{ex.Message}\" }}";
                    }

                    await ValueTask.CompletedTask;
                }
            };

            return Layout.Vertical()
               | new Card(
                       Layout.Vertical()
                       | Text.H2("Mapster Demo")
                       | Text.Muted("Mapster is a powerful object-to-object mapping library for .NET that automatically transfers data between similar objects. It provides flexible mapping configuration, supports validation, and handles complex transformations like combining FirstName + LastName into FullName or splitting FullName back into components.")
                       | (Layout.Horizontal().Gap(4)

                       // left card - Person
                       | new Card(
                        Layout.Vertical()
                        | Text.H4("Class: \"Person\" ")
                        | Text.Muted("Change the Person class below, click the button and see how the PersonDto class on the right changes")
                        | personJsonState.ToCodeInput()
                            .Height(Size.Auto())
                            .Language(Languages.Json)
                        | toDtoButton)

                       // right card - PersonDto
                       | new Card(
                        Layout.Vertical()
                        | Text.H4("Class: \"PersonDto\" ")
                        | Text.Muted("Change the PersonDto class below, click the button and see how the Person class on the left changes")
                        | dtoJsonState.ToCodeInput()
                            .Height(Size.Auto())
                            .Language(Languages.Json)
                            .Invalid(GetValidationError())
                            | toPersonButton
                        )
                       )
                       | new Spacer()
                       | Text.Small("This demo uses Mapster library for mapping objects.")
                       | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Mapster](https://github.com/MapsterMapper/Mapster)")
                   );
        }

        private static string ToPrettyJson(object? obj) =>
            obj == null
                ? ""
                : JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }
}
