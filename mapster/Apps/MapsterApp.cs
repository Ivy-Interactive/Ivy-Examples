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

            return Layout.Center()
               | new Card(
                       Layout.Vertical().Gap(12)
                       | Text.H4("Person")
                       | personJsonState.ToCodeInput()
                           .Width(Size.Units(100).Max(500))
                           .Height(Size.Auto())
                           .Language(Languages.Json)
                       | toDtoButton
                       | Text.H4("PersonDto")
                       | dtoJsonState.ToCodeInput()
                           .Width(Size.Units(100).Max(500))
                           .Height(Size.Auto())
                           .Language(Languages.Json)
                       | toPersonButton
                   )
                   .Width(Size.Units(120).Max(600));
        }

        private static string ToPrettyJson(object? obj) =>
            obj == null
                ? ""
                : JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }
}
