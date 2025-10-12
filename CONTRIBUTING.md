# Contributing to Ivy Examples

Thank you for your interest in contributing to Ivy Examples! This repository showcases the power and versatility of the [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) through real-world examples. We welcome contributions from developers of all skill levels.

## Ways to Contribute

### 1. Bug Fixes

- Fix issues in existing examples
- Improve error handling
- Update deprecated package references
- Enhance performance

### 2. New Examples

- Showcase integration with popular .NET packages
- Demonstrate real-world use cases
- Create educational examples for beginners
- Build complex applications showing advanced patterns

### 3. Documentation

- Improve README files for existing examples
- Add code comments and explanations
- Create tutorials and guides
- Translate documentation

### 4. UI/UX Improvements

- Enhance visual design of examples
- Improve responsive layouts
- Add accessibility features
- Modernize styling and interactions

## Getting Started

### Prerequisites

Before contributing, ensure you have:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later

### Setting Up Your Development Environment

1. **Fork the repository** on GitHub
2. **Clone your fork**:

   ```bash
   git clone https://github.com/YOUR_USERNAME/Ivy-Examples.git
   cd Ivy-Examples
   ```

3. **Create a new branch** for your contribution:

   ```bash
   git checkout -b feature/your-feature-name
   ```

## Creating a New Example

### Example Structure

Each example should follow this consistent structure:

```txt
your-example-name/
├── Apps/                 # Main application components
│   └── YourApp.cs       # Primary app class
├── Connections/         # Database connections (optional)
├── Models/              # Data models (optional)
├── Services/            # Business logic services (optional)
├── Program.cs           # Application entry point
├── GlobalUsings.cs      # Global using statements
├── YourExample.csproj   # Project file
├── YourExample.sln      # Solution file (optional)
├── Dockerfile           # Docker configuration
└── README.md           # Example documentation
```

## How to Run

1. Navigate to the example:

   ```bash
   cd your-example-name
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Run the application:

   ```bash
   dotnet watch
   ```

4. Open your browser to the URL shown in the terminal

## Learn More

- [Ivy Documentation](https://docs.ivy.app)

### 7. Create Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5010

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["YourExample.csproj", "./"]
RUN dotnet restore "YourExample.csproj"
COPY . .
RUN dotnet build "YourExample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "YourExample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YourExample.dll"]
```

## Contribution Guidelines

### Example Quality Standards

- **Functional**: Example must run without errors
- **Educational**: Code should be clear and well-commented
- **Practical**: Demonstrate real-world usage patterns
- **Complete**: Include all necessary files and documentation
- **Tested**: Verify the example works as expected

### Package Selection Criteria

When choosing packages to showcase:

- Popular and actively maintained
- Well-documented with clear APIs
- Useful for business applications
- Compatible with .NET 9.0+
- Avoid experimental or unstable packages
- Avoid packages with licensing issues

## Code Review Process

### Before Submitting

1. **Test your example** thoroughly
2. **Run the application** and verify all features work
3. **Check for errors** and warnings
4. **Review your code** for clarity and best practices
5. **Update documentation** as needed

### Submission Checklist

- [ ] Example runs without errors
- [ ] README.md is complete and accurate
- [ ] Code follows C# conventions
- [ ] All files are included
- [ ] Dockerfile works correctly
- [ ] No sensitive information is committed

### Pull Request Guidelines

- Use a descriptive title: `Add [Package Name] example` or `Fix issue in [Example Name]`
- Provide a clear description of what your contribution does
- Reference any related issues
- Include screenshots if your example has a visual component

## Reporting Issues

### Bug Reports

When reporting bugs, include:

- **Example name** and version
- **Steps to reproduce** the issue
- **Expected behavior** vs actual behavior
- **Environment details** (OS, .NET version, etc.)
- **Error messages** or logs

### Feature Requests

For new example suggestions:

- **Package name** and purpose
- **Use case** description
- **Why it would be valuable** to the community
- **Relevant links** to package documentation

## Getting Help

### Community Support

- **Discord**: [Join our community](https://discord.gg/ivy)
- **GitHub Discussions**: Ask questions and share ideas
- **Documentation**: [docs.ivy.app](https://docs.ivy.app)

### Mentorship

New contributors are welcome! If you're new to:

- **Ivy Framework**: Check out the `helloworld` example first
- **Open Source**: We're happy to guide you through your first contribution
- **.NET Development**: Our community can help you learn

## Recognition

### Contributors

All contributors are recognized in our:

- GitHub contributors list
- Release notes for significant contributions
- Community highlights

### Maintainers

Active contributors may be invited to become maintainers with:

- Code review privileges
- Direct commit access
- Involvement in project decisions

## License

By contributing to Ivy Examples, you agree that your contributions will be licensed under the MIT License.

## Thank You

Your contributions make Ivy Examples better for everyone. Whether you're fixing a typo, adding a new example, or improving documentation, every contribution matters!

---
