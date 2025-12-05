using Snowflake.Data.Client;
using SnowflakeExample.Services;
using System.ComponentModel.DataAnnotations;

namespace SnowflakeExample.Apps;

[App(icon: Icons.Info, title: "Snowflake Introduction", isVisible: false)]
public class SnowflakeIntroductionApp : ViewBase
{
    private record SnowflakeCredentialsRequest
    {
        [Required]
        public string Account { get; init; } = "";
        
        [Required]
        public string User { get; init; } = "";
        
        [Required]
        public string Password { get; init; } = "";
    }
    
    public override object? Build()
    {
        var isDialogOpen = this.UseState(false);
        var credentialsForm = this.UseState(() => new SnowflakeCredentialsRequest());
        
        return Layout.Center()
               | new Card(
                   Layout.Vertical().Gap(4).Padding(4)
                   | Text.H3("Getting Started with Snowflake")
                   | Text.Muted("Follow these steps to configure your Snowflake connection:")

                   // Instructions section
                   | Layout.Vertical().Gap(3)
                       | Text.Markdown("**1. Sign up or log in** to [Snowflake](https://www.snowflake.com/)")
                       | Text.Markdown("**2. Navigate to your Account** settings")
                       | Text.Markdown("**3. Copy your Account Identifier** (e.g., `xy12345.us-east-1`)")
                       | Text.Markdown("**4. Note your Username and Password**")
                       | Text.Markdown("**5. Enter your credentials below**")

                   // Button to open dialog
                   | new Button("Enter Credentials")
                       .Icon(Icons.Key)
                       .Variant(ButtonVariant.Primary)
                       .HandleClick(_ => isDialogOpen.Set(true))
                   
                   | new Spacer()
                   | Text.Small("Important: Never publish credentials in public repositories or share them with unauthorized parties.")
                   | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Snowflake .NET Connector](https://github.com/snowflakedb/snowflake-connector-net)")
                   ).Width(Size.Fraction(0.4f))
               | (isDialogOpen.Value
                   ? credentialsForm.ToForm()
                       .Builder(e => e.Account, e => e.ToTextInput())
                       .Label(e => e.Account, "Account Identifier")
                       .Builder(e => e.User, e => e.ToTextInput())
                       .Label(e => e.User, "Username")
                       .Builder(e => e.Password, e => e.ToPasswordInput())
                       .Label(e => e.Password, "Password")
                       .ToDialog(isDialogOpen,
                           title: "Enter Snowflake Credentials",
                           submitTitle: "Save",
                           width: Size.Fraction(0.3f)
                       )
                   : null);
    }
}

