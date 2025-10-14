# DNS Client

<img width="1911" height="909" alt="image" src="https://github.com/user-attachments/assets/ee9cb562-6c65-4686-8299-eacc94b79689" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fdnsclient%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For DNS Lookups

This example demonstrates DNS query operations using the [DnsClient.NET library](https://github.com/MichaCo/DnsClient.NET) integrated with Ivy. DnsClient.NET is a simple yet very powerful and high performant open source library for the .NET Framework to do DNS lookups.

**What This Application Does:**

This specific implementation creates a **DNS Lookup Tool** that allows users to:

- **Query DNS Records**: Perform DNS lookups for multiple record types (A, AAAA, MX, TXT, CNAME, NS, PTR, SRV, SOA)
- **Detailed Information Display**: Shows IP addresses, mail servers with priorities, text records, name servers, and more
- **Query Status Tracking**: View query success status, name server, record count, and message size
- **Record Sections**: Organized display of Answers and Authority sections
- **Domain Validation**: RFC 1035/1123 compliant domain name validation
- **Interactive UI**: Clean card-based layout with structured data presentation

**Technical Implementation:**

- Uses DnsClient.NET's `LookupClient` with singleton pattern for optimal performance
- Type-specific rendering for each DNS record type (A, AAAA, MX, TXT, CNAME, NS, PTR, SRV, SOA)
- Displays detailed record data: IPv4/IPv6 addresses, mail server priorities, TTL values
- Implements async DNS queries with error handling and toast notifications
- Domain validation using regex patterns (RFC 1035/1123 compliant)
- Signal-based component communication for reactive UI updates
- Split display with query form and detailed results sections

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd dnsclient
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Run the application**:
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd dnsclient
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your DNS lookup application with a single command.

## Learn More

- DnsClient.NET GitHub repository: [github.com/MichaCo/DnsClient.NET](https://github.com/MichaCo/DnsClient.NET)
- DnsClient.NET Documentation: [dnsclient.michaco.net](https://dnsclient.michaco.net)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
