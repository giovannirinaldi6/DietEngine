# DietEngine – Daily AI Meal Recommendation

## Overview

**DietEngine** is a personal diet assistant that helps you choose a healthy and varied meal every day based on the menu sent by your bistro via email.

It uses:

- **MailKit** to read daily menus from email (IMAP)  
- **OpenAI** to generate AI-based meal recommendations  
- **SQLite** for storing meal history and forbidden ingredients  
- **Quartz.NET** for daily scheduling of recommendations  
- **SMTP** for sending the selected meal via email  

The system ensures:

- Variety in protein sources  
- Balanced meals  
- Avoiding ingredients you dislike  
- Tracking weekly intake (e.g., limiting red meat)  

---

## Features

- Fetches menu emails from a configured sender  
- Parses the menu (plain text, HTML; PDF/images support coming)  
- Reads your last 5 meals from the database  
- Takes into account forbidden ingredients  
- Calls OpenAI to choose the best meal according to rules:
  - Do not repeat yesterday’s protein  
  - Max 2 red meat dishes per week  
  - Prefer non-fried dishes  
  - Maximize variety  
- Saves the recommended meals in SQLite database  
- Sends the recommendation to your configured recipient email  
- Can handle multiple recommendations (e.g., main + dessert)  
- Fully configurable via `appsettings.json`  

---

## Setup

### 1. Clone the repository

git clone https://github.com/giovannirinaldi6/DietEngine.git
cd DietEngine/DietWorker

### 2\. Create the SQLite database

-   Open `ScriptDb.txt` and run the SQL commands in **DB Browser for SQLite** or any SQLite client.

-   Save the database as `meals.db` somewhere on your machine.

-   Make sure to mount it as a volume if running in Docker, so it persists outside the container.

### 3\. Configure `appsettings.json`

-   Copy the template:

cp appsettings.Development.json appsettings.json

-   Edit `appsettings.json`:

    -   `EmailService` → set your IMAP and SMTP credentials and host/port

    -   `Persone` → set `MenuFrom` (sender) and `MenuTo` (recipient) emails

    -   `OpenAI:ApiKey` → set your OpenAI API key

    -   `ConnectionStrings:DefaultConnection` → set the path to your `meals.db`

Example:

{\
 "EmailService": {\
 "Username": "your-email@gmail.com",\
 "Password": "your-app-password",\
 "Imap": {\
 "Host": "imap.gmail.com",\
 "Port": 993,\
 "UseSsl": true\
 },\
 "Smtp": {\
 "Host": "smtp.gmail.com",\
 "Port": 587,\
 "UseSsl": true\
 }\
 },\
 "Persone": {\
 "MenuFrom": "bistro@gmail.com",\
 "MenuTo": "you@domain.com"\
 },\
 "ConnectionStrings": {\
 "DefaultConnection": "Data Source=C:\\path\\to\\meals.db"\
 },\
 "OpenAI": {\
 "ApiKey": "sk-XXXXXX"\
 }\
}

> Make sure you use an **App Password** for Gmail if 2FA is enabled.

### 4\. Run the project

-   Locally with Visual Studio / `dotnet run`

-   Or in Docker (make sure to mount `meals.db` as a volume)

dotnet run --project DietWorker

-   The Quartz job will schedule the daily recommendation automatically.

* * * * *

Notes
-----

-   The system currently reads plain text or HTML emails; PDF and images are planned for future versions.

-   The AI generates **JSON output only** to ensure structured data for saving in the database.

-   Forbidden ingredients and meal history are configurable via the database tables.

* * * * *

Licenses / Third-party packages
-------------------------------

This project is licensed under **MIT License**. See LICENSE for more information.

The following open-source packages are used:

| Package | Version | License |
| --- | --- | --- |
| HtmlAgilityPack | 1.12.4 | MIT |
| MailKit | 4.15.0 | MIT |
| MimeKit | 4.15.0 | MIT |
| Microsoft.EntityFrameworkCore.Design | 9.0.13 | Apache 2.0 / MIT |
| Microsoft.EntityFrameworkCore.Sqlite | 9.0.13 | MIT |
| Microsoft.EntityFrameworkCore.Tools | 9.0.13 | MIT |
| Microsoft.Extensions.Hosting | 9.0.4 | MIT |
| OpenAI | 2.8.0 | MIT |
| Quartz | 3.15.1 | Apache 2.0 |
| Quartz.Extensions.Hosting | 3.15.1 | Apache 2.0 |

> For a complete list of all transitive dependencies and licenses, see `dotnet list package --include-transitive`.

* * * * *

Author
------

Giovanni Rinaldi -- 2026