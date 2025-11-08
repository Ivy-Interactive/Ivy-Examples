namespace NewtonsoftJsonExample
{

    [App(title: "Newtonsoft.Json", icon: Icons.File)]
    public class NewtonsoftJsonApp : ViewBase
    {
        public override object? Build()
        {
            var client = UseService<IClientProvider>();
            var defaultUser = new UserData
            {
                FullName = "John Doe",
                Email = "johndoe@example.com",
                DateCreated = DateTime.Parse("2013-01-20T00:00:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
                Roles = new List<string> { "User", "Admin" }
            };

            var user = UseState<UserData>(() => new UserData());
            var date = UseState<DateTime>(DateTime.UtcNow);
            var roles = UseState<List<string>>(() => new List<string>(defaultUser.Roles));
            var availableRoles = UseState<List<string>>(() => new List<string>(defaultUser.Roles));
            var email = UseState("");
            var emailInvalid = UseState("");
            var isSerialized = UseState<bool>(true);

            var json = JsonConvert.SerializeObject(defaultUser, Formatting.Indented);
            var rawData = UseState<string>(json);

            UseEffect(() =>
            {
                user.Set(u => new UserData
                {
                    FullName = u.FullName,
                    Email = email.Value,
                    DateCreated = u.DateCreated,
                    Roles = new List<string>(u.Roles)
                });
            }, [email.ToTrigger()]);

            // Simple email validation (Ivy-style via Invalid)
            UseEffect(() =>
            {
                
                if (!(email.Value.Contains("@") && email.Value.Contains(".")))
                {
                    emailInvalid.Set("Please enter a valid email address");
                }
                else
                {
                    emailInvalid.Set("");
                }
            }, [email.ToTrigger()]);

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
                    var toSerialize = new UserData
                    {
                        FullName = user.Value.FullName,
                        Email = user.Value.Email,
                        DateCreated = date.Value,
                        Roles = new List<string>(roles.Value)
                    };
                    var rawInfo = JsonConvert.SerializeObject(toSerialize, Formatting.Indented);
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
                    date.Set(userData!.DateCreated);
                    email.Set(userData!.Email ?? "");
                    roles.Set(userData!.Roles ?? new List<string>());
                    // Update available role options by merging any new roles from JSON
                    var mergedRoles = new HashSet<string>(availableRoles.Value, StringComparer.OrdinalIgnoreCase);
                    foreach (var r in (userData!.Roles ?? Enumerable.Empty<string>()).Where(r => !string.IsNullOrWhiteSpace(r)))
                    {
                        mergedRoles.Add(r);
                    }
                    availableRoles.Set(mergedRoles.OrderBy(r => r).ToList());

                    isSerialized.Set(false);
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                }
            }

            return
                Layout.Horizontal().Gap(8)
                    | new Card(
                        Layout.Vertical()
						| Text.H4("Source JSON")
						| Text.Muted("Edit sample JSON (add roles, tweak fields) then click Deserialize to load it here.")
                        | rawData.ToCodeInput(variant: CodeInputs.Default, language: Languages.Json).Height(Size.Fit())
                            .Disabled(!isSerialized.Value)
                        | new Button("Deserialize", _ => HandleButtonClick())
                            .Disabled(!isSerialized.Value)
                            .Icon(Icons.ArrowRight, Align.Right)
                        | new Spacer()
                        | Text.Small("This demo uses Newtonsoft.Json library to serialize and deserialize JSON data.")
                        | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)")
                        )
                        .Width(Size.Half()).Height(Size.Fit().Min(Size.Full()))

                    | new Card(
                        Layout.Vertical()
						| Text.H4("User Editor")
						| Text.Muted("Modify user fields, pick date and roles, then click Serialize to push changes back to JSON.")

                        | Text.Label("Full name")
                        | new TextInput(user.Value?.FullName ?? string.Empty, e =>
                            {
                                user.Set(userData => new UserData
                                {
                                    FullName = e.Value,
                                    Email = userData.Email,
                                    DateCreated = userData.DateCreated,
                                    Roles = new List<string>(userData.Roles)
                                });
                            })
                            .Placeholder("Full name")
                            .Disabled(isSerialized.Value)

                        | Text.Label("Email")
                        | email.ToEmailInput()
                            .Placeholder("Email")
                            .Invalid(emailInvalid.Value)
                            .Disabled(isSerialized.Value)

                        | Text.Label("Date created")
                        | date.ToDateInput().Disabled(isSerialized.Value)
                        | Text.Label("Roles")
                        | roles.ToSelectInput(availableRoles.Value.ToOptions()).Variant(SelectInputs.Toggle).Disabled(isSerialized.Value)
                        | new Button("Serialize", _ => HandleButtonClick())
                            .Disabled(isSerialized.Value)
                            .Icon(Icons.ArrowLeft, Align.Left)
                    ).Width(Size.Half()).Height(Size.Fit().Min(Size.Full()));
        }
    }
}
