# ShopifySharp

<img width="1919" height="910" alt="image" src="https://github.com/user-attachments/assets/42412b44-e073-4d55-9674-88966e6a84d6" />

### Environment example:

```
SHOPIFY_SHOP_DOMAIN=test-1111111111111111111111111111111111711111111111123145.myshopify.com
SHOPIFY_ACCESS_TOKEN=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
```

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fshopifysharp%2Fdevcontainer.json&location=EuropeWest)

Launch the repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

This demo is built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework), an all-in-one C# framework for building internal tools, dashboards, and AI-assisted workflows.

## ShopifySharp Integration Overview

This example showcases how to integrate the [ShopifySharp](https://github.com/nozzlegear/ShopifySharp) library with Ivy to browse a Shopify store’s product catalog.

**What this app provides:**

- **Store Connection Form**: Enter your Shopify domain and admin API access token to authenticate.
- **Full Catalog Retrieval**: Fetches every product in the store (handles pagination automatically, 250 items per page behind the scenes).
- **Smart Filters**:
  - Sort by title, created/updated date, best-selling, or ID.
  - Switch between ascending/descending order.
  - Search using Shopify’s GraphQL query syntax (e.g. `tag:'summer'`).
- **Live Updates**: Filter controls trigger data refresh automatically—no manual reload required.
- **Product Cards**: Consistent 200×200 images, fallback placeholders, pricing display, and tidy layout.
- **UX Enhancements**:
  - Disabled filters until the first successful load.
  - Helpful loading and error states surfaced inside cards.
  - Informational copy guiding users through the workflow.

## How It Works

- Uses `ShopifySharp.GraphService` to call the Admin GraphQL API.
- Batches requests with a page size of 250 and follows `pageInfo` cursors until all products are retrieved.
- Maps Shopify nodes into lightweight Ivy view models with title, primary image, and first variant price.
- Renders the UI with Ivy widgets (`Layout`, `Card`, `Text`, `Image`, etc.) supplying a clean two-column layout.
- Reacts to state changes through Ivy hooks (`UseState`, `UseEffect`) to manage loading, filtering, and error handling.

## Run

1. **Prerequisites**
   - .NET 9.0 SDK
   - A Shopify store with an Admin API access token
2. **Navigate to the example**
   ```bash
   cd packages-demos/shopifysharp
   ```
3. **Restore dependencies**
   ```bash
   dotnet restore
   ```
4. **Run the app**
   ```bash
   dotnet watch
   ```
5. **Open your browser** at the URL displayed in the console (typically `http://localhost:5010`)

## Deploy

1. **Navigate to the example**
   ```bash
   cd packages-demos/shopifysharp
   ```
2. **Deploy**
   ```bash
   ivy deploy
   ```
This command ships the ShopifySharp Product Explorer to Ivy’s managed hosting.

## Learn More

- ShopifySharp documentation: [github.com/nozzlegear/ShopifySharp](https://github.com/nozzlegear/ShopifySharp)
- Shopify Admin API: [shopify.dev/docs/api/admin-graphql](https://shopify.dev/docs/api/admin-graphql)
- Ivy Framework docs: [docs.ivy.app](https://docs.ivy.app)