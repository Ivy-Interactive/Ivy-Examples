# AutodealerCrm

## Run

```bash

dotnet run -- --data-provider Sqlite --connection-string "Data Source=db.sqlite" --seed-database --yes-to-all

```

## Schema

```dbml

Enum user_role {
  manager
  supervisor
  admin
  analyst
}

Enum lead_stage {
  new
  contacted
  qualified
  in_negotiation
  prepayment
  sold
  lost
}

Enum source_channel {
  viber
  whatsapp
  telegram
  call
  email
}

Enum lead_intent {
  buy
  sell
  service_inquiry
  other
}

Enum vehicle_status {
  available
  reserved
  sold
  archived
}

Enum message_channel {
  viber
  whatsapp
  telegram
  email
}

Enum message_direction {
  incoming
  outgoing
}

Enum message_type {
  text
  photo
  file
  voice_note
}

Enum call_direction {
  inbound
  outbound
}

Table user {
  id int [pk, increment]
  name varchar [not null]
  email varchar [not null]
  role user_role [not null]
  created_at timestamp [not null]
  updated_at timestamp [not null]
}

Table customer {
  id int [pk, increment]
  first_name varchar [not null]
  last_name varchar [not null]
  email varchar
  viber_id varchar
  whatsapp_id varchar
  telegram_id varchar
  created_at timestamp [not null]
  updated_at timestamp [not null]
}

Table vehicle {
  id int [pk, increment]
  make varchar [not null]
  model varchar [not null]
  year int [not null]
  vin varchar [not null, unique]
  price decimal [not null]
  status vehicle_status [not null]
  manager_id int
  erp_sync_id varchar
  created_at timestamp [not null]
  updated_at timestamp [not null]
}

Table lead {
  id int [pk, increment]
  customer_id int [not null]
  manager_id int
  source_channel source_channel [not null]
  intent lead_intent [not null]
  stage lead_stage [not null]
  priority int
  notes text
  created_at timestamp [not null]
  updated_at timestamp [not null]
}

Table lead_vehicle {
  lead_id int [not null]
  vehicle_id int [not null]
  indexes {
    (lead_id, vehicle_id) [pk]
  }
}

Table media {
  id int [pk, increment]
  file_path varchar [not null]
  file_type varchar [not null]
  uploaded_at timestamp [not null]
  vehicle_id int
  lead_id int
  customer_id int
  created_at timestamp [not null]
  updated_at timestamp [not null]
}

Table message {
  id int [pk, increment]
  lead_id int
  customer_id int [not null]
  manager_id int
  channel message_channel [not null]
  direction message_direction [not null]
  type message_type [not null]
  content text
  media_id int
  sent_at timestamp [not null]
  created_at timestamp [not null]
  updated_at timestamp [not null]
}

Table call_record {
  id int [pk, increment]
  lead_id int
  customer_id int [not null]
  manager_id int
  direction call_direction [not null]
  start_time timestamp [not null]
  end_time timestamp [not null]
  duration int
  recording_url varchar
  script_score decimal
  sentiment decimal
  created_at timestamp [not null]
  updated_at timestamp [not null]
}

Table task {
  id int [pk, increment]
  lead_id int [not null]
  manager_id int [not null]
  title varchar [not null]
  description text
  due_date date
  completed boolean [not null]
  created_at timestamp [not null]
  updated_at timestamp [not null]
}

Ref: vehicle.manager_id > user.id
Ref: lead.customer_id > customer.id
Ref: lead.manager_id > user.id
Ref: lead_vehicle.lead_id > lead.id
Ref: lead_vehicle.vehicle_id > vehicle.id
Ref: media.vehicle_id > vehicle.id
Ref: media.lead_id > lead.id
Ref: media.customer_id > customer.id
Ref: message.lead_id > lead.id
Ref: message.customer_id > customer.id
Ref: message.manager_id > user.id
Ref: message.media_id > media.id
Ref: call_record.lead_id > lead.id
Ref: call_record.customer_id > customer.id
Ref: call_record.manager_id > user.id
Ref: task.lead_id > lead.id
Ref: task.manager_id > user.id

```

## Prompt

```

You are an expert product architect, business analyst, and CRM solution designer.

Your task: using the Ivy .NET–based business framework as the core, design and describe a CRM platform for an auto dealership / car sourcing business, based on the requirements, business flows, and use cases below.

DO NOT ADD PHONE NUMBERS - IGNORE THEM COMPLETELY

====================================================
1. CONTEXT
====================================================

The client is an auto business that:
- Sources and sells cars (selection, import, resale).
- Processes around 1,000 cars per month, with ~700 sales per month.
- Receives 90–95% of initial customer contact via Viber (also uses WhatsApp and Telegram).
- Currently uses personal phones for most communication; there is no stable mapping “manager ↔ client”.
- Calls are not systematically recorded; script compliance and quality cannot be evaluated.
- The current CRM (PlanFix) is too complex and rarely used:
  - lead stages are difficult to manage,
  - initiating and tracking dialogues is cumbersome,
  - working with images from chats is inconvenient and low quality.

The goal is to build a CRM/communication platform **on top of Ivy (.NET-based)** that:
- Centralizes all messaging and calls.
- Provides clear mapping: CUSTOMER ↔ LEAD ↔ MANAGER ↔ VEHICLE.
- Gives management full visibility into sales processes and communication quality.
- Is future-proof and extendable (e.g., future marketplace or auction modules).

====================================================
2. BUSINESS OBJECTIVES
====================================================

The system MUST:

1. Provide a **single communication hub (Inbox)** aggregating:
   - Viber (primary),
   - WhatsApp,
   - Telegram,
   - and later email or other channels.

2. Ensure a clear link between:
   - Customer,
   - Lead,
   - Assigned Manager,
   - Related Vehicles.

3. Support **call logging and analysis**:
   - all calls recorded via telephony platform (e.g., Ringostat or equivalent),
   - calls linked to the correct customer and lead,
   - calls available for later review and quality assessment.

4. Offer a **simple, practical lead pipeline** suitable for auto sales:
   - e.g., New → Contacted → Qualified → In Negotiation → Prepayment → Sold / Lost.

5. Provide **robust image/media handling**:
   - photos and files stored as actual files, not just links,
   - easy viewing, sharing, and reusing car photos and documents.

6. Integrate with back-office / ERP (e.g., BAS or similar):
   - synchronize vehicles,
   - customer data,
   - basic financial or order information.

7. Deliver **analytics and reporting**:
   - per-manager performance and conversion,
   - channel performance (Viber vs others),
   - lead funnel statistics,
   - vehicle status overview.

8. Be **ready for AI-powered expansion**:
   - call transcription,
   - script compliance analysis,
   - sentiment and intent detection,
   - prioritization of “hot” leads.

====================================================
3. CORE DOMAIN MODEL (HIGH-LEVEL)
====================================================

Using Ivy’s domain modeling capabilities, define and refine entities and relationships for at least:

- **User (internal staff)**
  - Roles: Manager, Supervisor, Admin, Analyst.
  - Permissions and responsibilities.

- **Customer**
  - Contact information including phone, messenger IDs, and optional email.
  - Link to all leads, vehicles in interest, and full communication history.

- **Lead**
  - Link to one Customer.
  - Assigned Manager (optional at creation, required for processing).
  - Source channel (Viber, WhatsApp, Telegram, call).
  - Intent (buy, sell, service inquiry, other).
  - Stage in pipeline.
  - Priority, notes, and related tasks.
  - Linked Vehicles (one or many).

- **Vehicle**
  - Core vehicle data (make, model, year, VIN, etc.).
  - Price and availability.
  - Status (available, reserved, sold, archived).
  - Link to responsible Manager.
  - Link to Customer/Lead where relevant.
  - Link/ID for ERP/BAS synchronization.

- **Message (Omnichannel Communication)**
  - Channel (Viber, WhatsApp, Telegram, etc.).
  - Direction (incoming/outgoing).
  - Type (text, photo, file, voice note).
  - Content or reference to stored media.
  - Link to Lead, Customer, and (optionally) Manager.

- **CallRecord**
  - Direction (inbound/outbound).
  - Basic metadata (duration, time, status).
  - Reference to recording.
  - Link to Customer and Lead.
  - Optional fields for AI analysis (e.g., scriptScore, sentiment, key phrases).

- **Media**
  - Photos, documents, and other files.
  - Link to Vehicle, Lead, and/or Customer.
  - Metadata for searching and filtering.

- **Task / Activity**
  - Follow-ups, scheduled calls, reminders.
  - Link to Lead and Manager.
  - Due dates and completion status.

====================================================
4. KEY BUSINESS FLOWS
====================================================

Design Ivy-based business flows to support the following:

FLOW 1 — Inbound Viber message → New Lead creation and assignment
-----------------------------------------------------------------
1. A new customer sends a message or photo via Viber.
2. System recognizes the customer (by phone or ID) or creates a new Customer.
3. System creates a new Lead or attaches to an existing open Lead.
4. Lead is placed in the “New” stage.
5. Assignment logic:
   - automatically assigns the lead to a manager based on defined rules (e.g., round-robin, workload, specialization),
   - or routes to a queue for manual assignment.
6. The assigned manager sees the new lead in the Inbox with:
   - last messages,
   - any attached photos,
   - basic customer info.
7. Manager responds directly from the CRM (via integrated Viber connector).
8. As the conversation progresses, manager updates the lead stage.

FLOW 2 — Call handling (via telephony platform) and linkage to lead
--------------------------------------------------------------------
1. Customer calls a tracked phone number.
2. Telephony system sends events to Ivy-based CRM:
   - call started,
   - call ended,
   - recording location (if applicable).
3. CRM:
   - matches phone number to Customer,
   - finds the relevant open Lead or creates a new one,
   - stores CallRecord linked to Customer and Lead.
4. Manager and Supervisor can later:
   - see call in timeline,
   - play the recording,
   - read transcript (if AI integrated).

FLOW 3 — Manager daily workflow
-------------------------------
1. Manager opens the Omnichannel Inbox:
   - sees all new and ongoing leads grouped by status and priority.
2. Takes ownership of unassigned leads or works on assigned leads.
3. Communicates with customers via the CRM:
   - replies to Viber/WhatsApp/Telegram messages,
   - sends and receives photos of vehicles,
   - schedules calls and tasks.
4. Updates lead stage and related vehicle information.
5. Marks leads as Sold or Lost, including reasons.

FLOW 4 — Centralized vehicle and media handling
-----------------------------------------------
1. Vehicles are created or synchronized from ERP/BAS.
2. Media (photos, files) are stored centrally and linked to vehicles and/or leads.
3. Manager can:
   - quickly attach vehicles to a lead,
   - send selected vehicle photos to the customer via messenger,
   - see all images related to a specific vehicle.

FLOW 5 — Supervisor / Owner view and control
--------------------------------------------
1. Supervisor/Owner accesses dashboards:
   - number of new leads per day/week,
   - conversions per manager,
   - channel breakdown (Viber vs others),
   - number of missed calls,
   - leads stuck in specific stages.
2. Can drill down to:
   - lead details,
   - full communication history,
   - call records and outcomes.

====================================================
5. USE CASES TO DESIGN FOR
====================================================

Use Ivy’s process modeling to support at least:

USE CASE A — “Assign and handle a new Viber lead”
- From first contact to clearly assigned manager and initial response, within a defined SLA.

USE CASE B — “Full communication history per customer”
- One screen or view that combines:
  - all chats (across Viber/WA/Telegram),
  - calls,
  - linked vehicles,
  - tasks and notes.

USE CASE C — “Photo chaos to structured media”
- Previously: photos stored in personal phones and chat threads.
- Now: all photos are stored in a central media structure, searchable and reusable.

USE CASE D — “Quality control over calls”
- Supervisor:
  - sees all calls,
  - can listen to them,
  - checks if the correct script was followed,
  - gives feedback to managers.

USE CASE E — “Lead lifecycle monitoring”
- Ability to see pipeline health:
  - how many leads in each stage,
  - where they tend to get stuck,
  - which managers have highest conversion.

====================================================
6. REQUIREMENTS FOR YOUR OUTPUT
====================================================

Using the above information, and assuming Ivy (.NET-based) is the underlying business framework:

1. Propose a **refined domain model** (entities + relationships) appropriate for Ivy.
2. Describe **key processes and workflows** as they should be modeled in Ivy (e.g., states, transitions, events, business rules).
3. Outline **core modules** of the CRM solution:
   - Communication module (Inbox),
   - Lead and Customer Management,
   - Vehicle and Media Management,
   - Call Logging and Review,
   - Reporting and Analytics,
   - Administration and Roles.
4. Detail the **business rules and validations** for:
   - lead creation and stage transitions,
   - assignment logic,
   - matching calls/messages to leads and customers,
   - vehicle status updates.
5. Explain how you would leverage Ivy’s strengths:
   - process modeling,
   - workflow automation,
   - integration capabilities,
   - to build this CRM in a maintainable, scalable way.

Focus on clear business logic and architecture that fits the auto sales context and uses Ivy as the backbone for workflows, state management, and integration—not on low-level technical implementation details.

autodealer_crm_prompt_ivy.md
External
Displaying autodealer_crm_prompt_ivy.md.
```
