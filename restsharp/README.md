# RestSharp Demo Application

This is an interactive Ivy demo application showcasing Restsharp (Simple REST and HTTP API Client for .NET) integration with the Ivy framework.

## Features

This demo application demonstrates the core features of Restsharp (working with rest Api's):

- Call a rest api with Get, Post, Put, Patch and Delete methods
- Assign the request headers
- Assign the request body
- Show the response and status code
- Formatting text as json



## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Ivy Framework

### Running the Application

1. Navigate to the restsharp directory:
   ```bash
   cd restsharp
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. The application will open in your default browser with the RestSharp demo interface.

## RestSharp Integration with Ivy

This demo showcases how to integrate Rest api request concepts with Ivy applications:

### Key Components

- **RestsharpApp**: Main application with interactive UI for calling apis with RestSharp

### RestSharp Operations Demonstrated

- **Sending requests**
   ```csharp
    var client = new RestClient(options);
    var request = new RestRequest();
    var response = client.ExecuteGet(request);;
   ```

## Security Notes

⚠️ **Important Security Considerations:**

- This demo uses a simplified calling the rest apis implementation for educational purposes only
- The demo uses hardcoded values for demonstration - never use this approach in production
- Store secret keys securely (e.g., in Azure Key Vault, environment variables, or secure configuration)

## Educational Value

This demo helps developers understand:

1. **RestSharp Basics**: How to send GET, POST, PUT, PATCH, and DELETE requests
2. **Headers & Body Management**: Assigning headers and body content to requests
3. **Response Handling**: Reading responses, status codes, and formatting as JSON
4. **Error Handling**: Dealing with failed requests
5. **Ivy Integration**: How to build interactive UIs for API calls using Ivy’s state management and UI components

## Contributing

This is part of the Ivy Examples collection. To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Resources

- [Ivy Framework Documentation](https://docs.ivy.app)
- [RestSharp Package on GitHub](https://github.com/restsharp/RestSharp)
- [REST API Tutorial](https://restfulapi.net/)
- [Ivy Discord Community](https://discord.gg/sSwGzZAYb6)

## License

This project is part of the Ivy Examples collection and follows the same licensing terms.
