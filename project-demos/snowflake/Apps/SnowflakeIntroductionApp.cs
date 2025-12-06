using Snowflake.Data.Client;
using SnowflakeExample.Services;
using SnowflakeExample.Connections;
using System.ComponentModel.DataAnnotations;

namespace SnowflakeExample.Apps;

[App(icon: Icons.Info, title: "Snowflake Introduction")]
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
        var verificationStatus = this.UseState<string?>(() => null);
        var isVerifying = this.UseState(false);
        var configuration = this.UseService<IConfiguration>();
        var refreshToken = this.UseRefreshToken();
        
        // Handle credential verification when form is submitted
        this.UseEffect(async () =>
        {
            var credentials = credentialsForm.Value;
            // Check if form was submitted (Account is not empty and dialog was just closed)
            if (!string.IsNullOrWhiteSpace(credentials.Account) && !isDialogOpen.Value && !isVerifying.Value && verificationStatus.Value == null)
            {
                isVerifying.Value = true;
                
                try
                {
                    var warehouse = configuration["Snowflake:Warehouse"] ?? "";
                    var database = configuration["Snowflake:Database"] ?? "SNOWFLAKE_SAMPLE_DATA";
                    var schema = configuration["Snowflake:Schema"] ?? "TPCH_SF1";
                    
                    var connectionString = $"account={credentials.Account};user={credentials.User};password={credentials.Password};warehouse={warehouse};db={database};schema={schema};";
                    var snowflakeService = new SnowflakeService(connectionString);
                    
                    var isValid = await snowflakeService.TestConnectionAsync();
                    
                    if (isValid)
                    {
                        // Save verified credentials to VerifiedCredentials service
                        VerifiedCredentials.SetCredentials(credentials.Account, credentials.User, credentials.Password);
                        
                        // Also save to environment variables for next startup
                        Environment.SetEnvironmentVariable("Snowflake__Account", credentials.Account, EnvironmentVariableTarget.User);
                        Environment.SetEnvironmentVariable("Snowflake__User", credentials.User, EnvironmentVariableTarget.User);
                        Environment.SetEnvironmentVariable("Snowflake__Password", credentials.Password, EnvironmentVariableTarget.User);
                        
                        verificationStatus.Value = "success";
                        isVerifying.Value = false;
                        
                        // Refresh page to enable all features
                        refreshToken.Refresh();
                    }
                    else
                    {
                        verificationStatus.Value = "error";
                        isVerifying.Value = false;
                    }
                }
                catch
                {
                    verificationStatus.Value = "error";
                    isVerifying.Value = false;
                }
            }
        }, [credentialsForm, isDialogOpen]);
        
        // Show success message if already verified
        var showSuccessMessage = VerifiedCredentials.IsVerified && verificationStatus.Value == null;
        
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

                   // Verification status message
                   | (showSuccessMessage
                       ? Layout.Vertical().Gap(2)
                           | Text.Small("✓ Connection verified successfully! All Snowflake apps are now available.")
                       : verificationStatus.Value == "success"
                       ? Layout.Vertical().Gap(2)
                           | Text.Small("✓ Connection successful! All Snowflake apps are now available.")
                       : verificationStatus.Value == "error"
                       ? Layout.Vertical().Gap(2)
                           | Text.Small("✗ Connection failed. Please check your credentials and try again.")
                       : null)

                   // Button to open dialog
                   | new Button("Enter Credentials")
                       .Icon(Icons.Key)
                       .Variant(ButtonVariant.Primary)
                       .Disabled(isVerifying.Value)
                       .HandleClick(_ => {
                           isDialogOpen.Value = true;
                           verificationStatus.Value = null;
                       })
                   
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
                           submitTitle: isVerifying.Value ? "Verifying..." : "Save",
                           width: Size.Fraction(0.3f)
                       )
                   : null);
    }
}

