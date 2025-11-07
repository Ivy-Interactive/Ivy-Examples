# JWT Demo Application

This is an interactive Ivy demo application showcasing JWT (JSON Web Tokens) concepts and integration with the Ivy framework.

## Features

This demo application demonstrates the core features of JWT token management:

### 🔐 JWT Token Generator
- Generate JWT tokens with predefined claims
- Support for standard claims (sub, name, email, role)
- Real-time token generation with expiration

### ✅ JWT Token Validator
- Validate JWT tokens using a simplified implementation
- Display detailed validation results including:
  - Token validity status
  - Expiration information
  - Issuer and audience details
  - All claims contained in the token
- Interactive token input and validation

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Ivy Framework

### Running the Application

1. Navigate to the jwt directory:
   ```bash
   cd jwt
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. The application will open in your default browser with the JWT demo interface.

## JWT Integration with Ivy

This demo showcases how to integrate JWT concepts with Ivy applications:

### Key Components

- **JwtService**: Core service class handling JWT operations
- **JwtDemoApp**: Main application with interactive UI for token generation and validation

### JWT Operations Demonstrated

1. **Token Generation**
   ```csharp
   var token = jwtService.GenerateToken(claims, expirationMinutes);
   ```

2. **Token Validation**
   ```csharp
   var result = jwtService.ValidateToken(token);
   ```

## Security Notes

⚠️ **Important Security Considerations:**

- This demo uses a simplified JWT implementation for educational purposes only
- In production applications, always use a proper JWT library like the JWT package
- The demo uses hardcoded values for demonstration - never use this approach in production
- Always validate tokens before trusting their contents
- Use appropriate token expiration times based on your security requirements
- Store secret keys securely (e.g., in Azure Key Vault, environment variables, or secure configuration)

## JWT Claims Reference

### Standard Claims (RFC 7519)
- `iss` (Issuer): Who issued the token
- `sub` (Subject): Who the token is about
- `aud` (Audience): Who the token is intended for
- `exp` (Expiration Time): When the token expires
- `nbf` (Not Before): When the token becomes valid
- `iat` (Issued At): When the token was issued
- `jti` (JWT ID): Unique identifier for the token

### Common Custom Claims
- `name`: User's full name
- `email`: User's email address
- `role`: User's role or permission level
- `permissions`: Array of specific permissions
- `department`: User's department
- `locale`: User's preferred language
- `timezone`: User's timezone

## Educational Value

This demo helps developers understand:

1. **JWT Structure**: How JWT tokens are composed of header, payload, and signature
2. **Claims Management**: How to add, read, and validate claims
3. **Token Lifecycle**: Generation, validation, and expiration handling
4. **Security Best Practices**: Proper token validation and secret management
5. **Ivy Integration**: How to build interactive UIs for JWT operations using Ivy's state management and UI components

## Contributing

This is part of the Ivy Examples collection. To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Resources

- [Ivy Framework Documentation](https://docs.ivy.app)
- [JWT Package on GitHub](https://github.com/jwt-dotnet/jwt)
- [JWT Specification (RFC 7519)](https://tools.ietf.org/html/rfc7519)
- [Ivy Discord Community](https://discord.gg/sSwGzZAYb6)

## License

This project is part of the Ivy Examples collection and follows the same licensing terms.
