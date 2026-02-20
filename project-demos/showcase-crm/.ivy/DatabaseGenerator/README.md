# ShowcaseCrm

## Run

```bash

dotnet run -- --data-provider Sqlite --connection-string "Data Source=C:\git\Ivy-Interactive\Ivy-Examples\project-demos\showcase-crm\db.sqlite" --seed-database --yes-to-all

```

## Schema

```dbml

Enum lead_status {
    new
    contacted
    qualified
    lost
}

Enum deal_stage {
    prospecting
    qualification
    proposal
    closed_won
    closed_lost
}

Table user {
    id int [pk, increment]
    name varchar [not null]
    email varchar [not null]
    created_at timestamp [not null]
    updated_at timestamp [not null]
}

Table company {
    id int [pk, increment]
    name varchar [not null]
    address varchar
    phone varchar
    website varchar
    created_at timestamp [not null]
    updated_at timestamp [not null]
}

Table contact {
    id int [pk, increment]
    company_id int [not null]
    first_name varchar [not null]
    last_name varchar [not null]
    email varchar
    phone varchar
    created_at timestamp [not null]
    updated_at timestamp [not null]
}

Table lead {
    id int [pk, increment]
    company_id int
    contact_id int
    status lead_status [not null]
    source varchar
    created_at timestamp [not null]
    updated_at timestamp [not null]
}

Table deal {
    id int [pk, increment]
    company_id int [not null]
    contact_id int [not null]
    lead_id int
    amount decimal
    stage deal_stage [not null]
    close_date date
    created_at timestamp [not null]
    updated_at timestamp [not null]
}

Ref: contact.company_id > company.id
Ref: lead.company_id > company.id
Ref: lead.contact_id > contact.id
Ref: deal.company_id > company.id
Ref: deal.contact_id > contact.id
Ref: deal.lead_id > lead.id

```

## Prompt

```
Generate a complete showcase CRM app for the Ivy framework that we can run with hot reload and demo to customers.

Requirements:
Full CRM app: dashboard, entities (e.g. Leads, Contacts, Deals, Companies), and typical CRUD flows.
Use the Ivy DB/agent flow: generate the app with “ivy db generate” (or equivalent) so that tables/schema and connections are created.
Include as many framework widgets and components as possible in one place:
Dashboard with charts, KPIs, and summary cards
At least one Kanban board (e.g. deals or tasks by status)
Data tables with sorting, filtering, pagination
Forms with common inputs (text, number, date, select, checkbox)
Layouts: sidebar navigation, blades, modals/dialogs
Buttons, badges, tooltips, tabs
Optional: file upload, rich text, or other advanced controls if available
Project name: showcase-crm, under project-demos/showcase-crm.
Structure: clear separation of Apps, Connections, and any shared components; ready for hot reload in the IDE.

Generate the full project: folder structure, .csproj, connection(s), app entry points, and all pages (dashboard, list/detail forms, kanban, tables) so the app runs and showcases the framework.



```
