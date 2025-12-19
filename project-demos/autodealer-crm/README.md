# Autodealer CRM

## Description

Autodealer CRM is a comprehensive customer relationship management system designed specifically for automotive dealerships. Built with Ivy Framework, it provides a complete solution for managing customers, leads, vehicles, tasks, communications, and sales analytics.

## Live Demo

[![Live Demo](https://img.shields.io/badge/Live%20Demo-Autodealer%20CRM-blue?style=for-the-badge)](https://ivy-projectdemos-autodealer_crm.sliplane.app)

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fautodealer_crm%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 10.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Automotive Dealership CRM

This example demonstrates a complete CRM system for automotive dealerships built with Ivy. It showcases how to build a full-featured business application with customer management, lead tracking, inventory management, and comprehensive analytics.

**What This Application Does:**

This implementation creates a **Complete CRM System** that allows dealerships to:

- **Dashboard & Analytics**: Comprehensive dashboard with key performance metrics including:
  - Total sales revenue
  - Number of leads and conversion rates
  - Average lead response time
  - Task completion rates
  - Customer retention rates
  - Vehicle inventory metrics
  - Interactive charts for daily trends and distributions

- **Customer Management**: Complete customer database with:
  - Customer profiles (name, email, contact information)
  - Multi-channel contact support (Viber, WhatsApp, Telegram)
  - Customer history and relationship tracking

- **Lead Management**: Advanced lead tracking system with:
  - Lead stages and intent tracking
  - Source channel attribution
  - Manager assignment
  - Priority levels
  - Notes and documentation
  - Conversion tracking

- **Vehicle Inventory**: Comprehensive vehicle management:
  - Vehicle details (make, model, year, VIN)
  - Pricing information
  - Status tracking (available, sold, reserved, etc.)
  - Manager assignment
  - ERP system integration support
  - Media attachments

- **Task Management**: Task tracking and workflow:
  - Task assignment and prioritization
  - Completion tracking
  - Task-to-lead relationships

- **Multi-Channel Communication**: Unified messaging system:
  - Messages across multiple channels (Viber, WhatsApp, Telegram)
  - Message direction tracking (inbound/outbound)
  - Message type classification
  - Conversation history

- **Call Records**: Phone call tracking:
  - Call direction (inbound/outbound)
  - Call duration tracking
  - Call-to-customer and call-to-lead relationships

- **User Management**: Team and role management:
  - User accounts and roles
  - Manager assignments
  - Access control

- **Media Management**: File and media handling:
  - Media attachments for customers, leads, and vehicles
  - Organized media library

**Technical Implementation:**

- **Entity Framework Core** with SQLite database
- **Ivy Framework** for unified C# frontend and backend
- **Blade-based UI** for modular, reusable components
- **Dashboard views** with real-time metrics and charts
- **Responsive layouts** with grid and horizontal/vertical arrangements
- **State management** with React-like hooks pattern
- **Database-first approach** with existing SQLite database

## Prerequisites

1. **.NET 10.0 SDK** or later
2. **SQLite database** (included: `db.sqlite`)

## How to Run

1. **Navigate to the example**:
   ```bash
   cd project-demos/autodealer_crm
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run the application**:
   ```bash
   dotnet watch
   ```

4. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd project-demos/autodealer_crm
   ```

2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```

3. **Configure database** in your deployment settings:
   - Ensure SQLite database is properly configured
   - Set up database connection string if using external database

This will deploy your CRM application with a single command.

## Learn More

- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
- Ivy Framework: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)

## Tags

CRM, Automotive, Dealership, Customer Management, Lead Tracking, Inventory Management, Sales Analytics, Business Application
