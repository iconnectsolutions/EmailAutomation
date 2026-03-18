# Email Automation

A cross-platform web app (Mac + Windows) that imports CSV contacts, uses Outlook Web draft templates, and sends personalized emails with `{FirstName}` replacement. Tracks sent dates in mail1, mail2, etc. columns.

## Prerequisites

- .NET 9 SDK
- Azure AD app registration (for Outlook/Graph API)

## Azure AD Setup

1. Go to [Azure Portal](https://portal.azure.com) → **App registrations** → **New registration**
2. Name: `Email Automation`
3. Supported account types: **Accounts in any organizational directory and personal Microsoft accounts**
4. Redirect URI: **Web** → `http://localhost:5052/api/auth/callback`
5. After creation, go to **Certificates & secrets** → **New client secret** → copy the value
6. Go to **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated**:
   - `Mail.ReadWrite`
   - `User.Read`
   - `offline_access`

## Configuration

Edit `src/EmailAutomation.Web/appsettings.json` (or `appsettings.Development.json`):

```json
{
  "AzureAd": {
    "ClientId": "YOUR_APP_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "TenantId": "common"
  }
}
```

## Run

```bash
cd src/EmailAutomation.Web
dotnet run
```

Open http://localhost:5052

## CSV Format

| Column | Required | Description |
|--------|----------|-------------|
| email | Yes | Recipient email |
| name | Yes | Used for `{FirstName}` replacement |
| mail1, mail2, ... | No | Pre-filled send dates; first empty one is used |
| ignore | No | "yes" / "true" / "1" = skip this row |

## Workflow

1. **Connect Outlook** – Sign in with your Microsoft account
2. **Import CSV** – Upload a CSV file
3. **Select batch** – Choose the imported batch
4. **Draft subject** – Enter the exact subject of your Outlook draft (must match)
5. **Send** – Emails are sent; dates are written to the first empty mail column

## Outlook Draft

Create a draft in Outlook (outlook.com or Office 365) with:
- The exact subject you will enter in the app
- Body containing `{FirstName}` as placeholder
- Save as draft (do not send)

The app will use this draft as the template, replace `{FirstName}` with each recipient's name, and send.
