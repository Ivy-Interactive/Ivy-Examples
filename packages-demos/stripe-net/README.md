# Stripe.net

<img width="1910" height="911" alt="image" src="https://github.com/user-attachments/assets/43c33420-216d-4920-a3f2-a33ab1ab24a6" />

<img width="1917" height="918" alt="image" src="https://github.com/user-attachments/assets/3495ecaa-a794-411f-8f7a-3894bedb5209" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fstripe-net%2Fdevcontainer.json&location=EuropeWest)

Launch a ready-to-code workspace with:
- **.NET 9.0** SDK pre-installed
- **Stripe CLI** and Ivy tooling available out of the box
- **Zero local setup** required

## Built With Ivy

This web application is powered by [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## Interactive Stripe Checkout Example

This demo showcases how to integrate [Stripe Checkout](https://stripe.com/docs/payments/checkout) using the official [Stripe.net SDK](https://github.com/stripe/stripe-dotnet) within an Ivy application.

### Features

- **Session Builder UI** – Configure product name, currency, amount, and quantity in real time
- **Automatic Currency Handling** – Detects zero-decimal currencies to format amounts correctly
- **Instant Session Creation** – Creates a Stripe checkout session with a single click and opens the redirect URL
- **Toast Notifications** – Guides the user through validation errors and status updates
- **Configurable Success/Cancel URLs** – Reads the base URL from configuration to keep redirects consistent

### Configuration

The app reads settings from `appsettings.json` (overridable via environment variables):
- `Stripe:SecretKey` – Your Stripe secret API key (use the test key in development)
- `BaseURL` – Public URL that Stripe redirects back to after checkout

## Setting Up the Secret Key

Before running the application, you need to configure your Stripe secret API key. There are two ways to set it up:

### Step 1: Get Your Stripe Secret Key

1. **Sign up or log in** to [Stripe Dashboard](https://dashboard.stripe.com/)
2. Navigate to **Developers** → **API keys**
3. For development, make sure **Test mode** is enabled (toggle in the top right)
4. Copy your **Secret key** (starts with `sk_test_...` for test mode)

> **Important:** Never publish secret keys in public repositories or share them with unauthorized parties.

### Step 2: Configure the Secret Key

For better security, especially in production, use environment variables instead of storing the key in a file.

**Windows PowerShell:**
```powershell
$env:Stripe__SecretKey = "sk_test_your_secret_key_here"
```

**Windows Command Prompt:**
```cmd
set Stripe__SecretKey=sk_test_your_secret_key_here
```

**Linux/macOS:**
```bash
export Stripe__SecretKey="sk_test_your_secret_key_here"
```

> **Note:** The double underscore `__` in the environment variable corresponds to the colon `:` in configuration (i.e., `Stripe__SecretKey` → `Stripe:SecretKey`)

After setting the environment variable, run the app in the same console:
```bash
dotnet watch
```
## How to Run Locally

1. **Prerequisites:** .NET 9.0 SDK and a Stripe test secret key
2. **Navigate to the project:**
   ```bash
   cd packages-demos/stripe-net
   ```
3. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
4. **Start the app:**
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## Deploy to Ivy Hosting

1. **Install Ivy CLI** if not already installed:
   ```bash
   dotnet tool install --global Ivy.Cli
   ```
2. **Deploy:**
   ```bash
   ivy deploy
   ```

## Learn More

- Stripe Checkout documentation: [stripe.com/docs/payments/checkout](https://stripe.com/docs/payments/checkout)
- Stripe.net SDK: [github.com/stripe/stripe-dotnet](https://github.com/stripe/stripe-dotnet)
- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)