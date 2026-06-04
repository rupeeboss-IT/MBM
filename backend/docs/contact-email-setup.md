# Contact form email — how it is sent

Contact emails are sent through **SMTP** (same mechanism as OTP and report emails), using `SmtpEmailSender` in `Auth/Senders.cs`.

## Two configuration blocks

### 1. `EmailSettings` (shared SMTP — required unless contact overrides everything)

Used by OTP, password reset, invoices/reports, and contact form when contact SMTP fields are empty.

| Key | Example | Purpose |
|-----|---------|---------|
| `Host` | `smtpout.secureserver.net` | SMTP server |
| `Port` | `587` | Usually 587 (TLS) |
| `Username` | `info@msmebharatmanch.com` | Mailbox login |
| `Password` | *(mailbox password)* | SMTP password |
| `From` | `info@msmebharatmanch.com` | Default sender |

### 2. `ContactSettings` (addresses + optional contact-only SMTP)

| Key | Purpose |
|-----|---------|
| `FromEmail` | Visible **From** on contact mails (`support@msmebharatmanch.com`) |
| `SupportNotifyEmail` | Inbox that receives the internal enquiry alert |
| `SmtpUsername` / `SmtpPassword` | Optional: log in as **support@** instead of `EmailSettings:Username` |
| `SmtpHost` / `SmtpPort` | Optional: override host/port (empty = use `EmailSettings`) |

## Recommended for support@ sender

Set the **support mailbox password** on contact SMTP (GoDaddy often requires login as the same address you send as):

```json
"ContactSettings": {
  "FromEmail": "support@msmebharatmanch.com",
  "SupportNotifyEmail": "support@msmebharatmanch.com",
  "SmtpUsername": "support@msmebharatmanch.com",
  "SmtpPassword": "YOUR_SUPPORT_MAILBOX_PASSWORD"
}
```

Keep `EmailSettings` filled in for other features (OTP, etc.).

## Flow on submit

1. Save row to `MBM.dbo.ContactSubmissions` (email is **required**)
2. SMTP send to **customer** at the form email (card HTML; uses `EmailSettings` first for deliverability to Gmail/Yahoo)
3. SMTP send to **support@** (enquiry details, Reply-To = customer email; uses `ContactSettings` SMTP when configured)

If SMTP is not configured, the form still saves but emails are skipped (logged server-side only).
