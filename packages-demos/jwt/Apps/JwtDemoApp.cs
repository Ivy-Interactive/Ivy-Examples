namespace Jwt.Apps;

/// <summary>
/// Main JWT Demo Application showcasing JWT token generation, validation, and claims handling.
/// This app demonstrates the core features of JWT token management with Ivy.
/// </summary>
[App(icon: Icons.ShieldCheck, title: "JWT Demo")]
public class JwtDemoApp : ViewBase
{
    public override object? Build()
    {
        var jwtService = new JwtService();
        
        // State for managing the demo
        var tokenInput = this.UseState<string>();
        var generatedToken = this.UseState<string>();
        var validationResult = this.UseState<string>();

        // Generate a sample token
        var generateToken = new Action(() =>
        {
            var claims = new Dictionary<string, string>
            {
                ["sub"] = "user123",
                ["name"] = "John Doe",
                ["email"] = "john.doe@example.com",
                ["role"] = "admin"
            };
            var token = jwtService.GenerateToken(claims, 60);
            generatedToken.Set(token);
            tokenInput.Set(token);
        });

        // Validate the current token
        var validateToken = new Action(() =>
        {
            var token = tokenInput.Value;
            if (string.IsNullOrEmpty(token))
            {
                validationResult.Set("Please enter a token to validate.");
                return;
            }

            var result = jwtService.ValidateToken(token);
            if (result.IsValid)
            {
                var claimsText = result.Claims != null 
                    ? string.Join("\n", result.Claims.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                    : "No claims found";
                validationResult.Set($"✅ Token is valid!\n\nClaims:\n{claimsText}");
            }
            else
            {
                validationResult.Set($"❌ Token is invalid: {result.ErrorMessage}");
            }
        });

        // Clear all fields
        var clearAll = new Action(() =>
        {
            tokenInput.Set("");
            generatedToken.Set("");
            validationResult.Set("");
        });

        return Layout.Center()
               | (new Card(
                   Layout.Vertical().Gap(6).Padding(2)
                   | Text.H2("JWT Demo Application")
                   | Text.Block("This demo showcases JWT token operations using a simplified implementation.")
                   | new Separator()
                   | Text.H3("Token Operations:")
                   | Layout.Horizontal().Gap(4)
                       | new Button("Generate Sample Token", generateToken).Primary()
                       | new Button("Validate Token", validateToken).Secondary()
                       | new Button("Clear All", clearAll).Outline()
                   | new Separator()
                   | Text.H3("Generated Token:")
                   | generatedToken.ToInput(placeholder: "Generated token will appear here...")
                   | Text.H3("Token Input:")
                   | tokenInput.ToInput(placeholder: "Paste a JWT token here to validate...")
                   | Text.H3("Validation Result:")
                   | validationResult.ToInput(placeholder: "Validation results will appear here...")
                 )
                 .Width(Size.Units(120).Max(800)));
    }
}