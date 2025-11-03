namespace NewtonsoftJsonExample
{

    [App(title: "Newtonsoft Demo", icon: Icons.File)]
    public class NewtonsoftJsonApp : ViewBase
    {
        public override object? Build()
        {
            var client = UseService<IClientProvider>();
            var user = UseState<UserData>(() => new UserData());
            var isSerialized = UseState<bool>(true);

            string json = @"{
                  'FullName': 'John Doe',
                  'Email': 'johndoe@example.com',
                  'IsActive': true,
                  'DateCreated': '2013-01-20T00:00:00Z',
                  'Roles': [
                    'User',
                    'Admin'
                  ]
                }";

            var rawData = UseState<string>(json);

            void HandleButtonClick()
            {
                if (isSerialized.Value)
                {
                    DeserializeData();
                }
                else
                {
                    SerializeData();
                }
            }

            void SerializeData()
            {
                try
                {
                    var rawInfo = JsonConvert.SerializeObject(user.Value, Formatting.Indented);
                    rawData.Set(rawInfo);
                    isSerialized.Set(true);
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                }
            }

            void DeserializeData()
            {
                try
                {
                    var userData = JsonConvert.DeserializeObject<UserData>(rawData.Value);
                    user.Set(userData!);

                    isSerialized.Set(false);
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                }
            }

            return
                Layout.Horizontal().Center()
                    | new Card(
                        Layout.Vertical()
                        | Text.H4("Simple Json Data")
                        | rawData.ToCodeInput(variant: CodeInputs.Default, language: Languages.Json).Height(80)
                            .Disabled(!isSerialized.Value)
                        )
                        .Width(Size.Half()).Height(150)
                    | new Button(isSerialized.Value ? "Deserialize" : "Serialize",
                        _ => HandleButtonClick())
                        .Icon(isSerialized.Value ? Icons.ArrowRight : Icons.ArrowLeft,
                            isSerialized.Value ? Align.Right : Align.Left)

                    | new Card(
                        Layout.Vertical().Scroll().Padding(10)
                        | Text.H4(user.Value != null ? user.Value.FullName : string.Empty)

                        | new TextInput(user.Value?.FullName ?? string.Empty, placeholder: "Full name", onChange: e =>
                            {
                                user.Set(userData =>
                                {
                                    userData.FullName = e.Value;
                                    return userData;
                                });
                            }).WithMargin(top: 5, left: 0, right: 0, bottom: 0)

                        | new TextInput(user.Value?.Email ?? string.Empty, placeholder: "Email", onChange: e =>
                            {
                                user.Set(userData =>
                                {
                                    userData.Email = e.Value;
                                    return userData;
                                });
                            }).WithMargin(top: 5, left: 0, right: 0, bottom: 0)
                        | Text.P(user.Value?.DateCreated.ToLongDateString()).WithMargin(top: 5, left: 0, right: 0, bottom: 0)

                        | Text.H4("Roles")
                        | new List(user.Value.Roles.Select(x => new ListItem(x)).ToList()).WithMargin(top: -7, left: -4, right: 0, bottom: 0)
                    ).Width(Size.Half()).Height(150);
        }
    }
}
