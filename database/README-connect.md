# MBM Connect Module — Database Setup

**You must run the SQL script manually.** The application does not create or alter tables at startup.

## Steps

1. Open SQL Server Management Studio (or Azure Data Studio) against your **MBM** database.
2. Run the full script: [`connect-module-schema.sql`](./connect-module-schema.sql)
3. Confirm four new tables exist:
   - `dbo.ConnectMemberProfiles`
   - `dbo.ConnectAdminListings`
   - `dbo.ConnectRequests`
   - `dbo.ConnectContactUnlocks`
4. Deploy / restart the API and frontend.
5. In **Admin → Manage Connect**, add listings for existing members/partners.

### Quick import from existing Users

If you already have members/partners in `dbo.Users` but Connect shows empty, run:

[`connect-seed-from-users.sql`](./connect-seed-from-users.sql)

This creates `ConnectAdminListings` rows for all active members/partners who are not listed yet.

## Notes

- **No existing tables are modified** — only new tables are created.
- `UserId` references `dbo.Users(UserId)` — members/partners must already exist.
- Set `ConnectSettings:Enabled` to `false` in `appsettings.json` to disable Connect APIs until you are ready.

## Configuration

```json
"ConnectSettings": {
  "Enabled": true,
  "BasicStandardContactLimit": 5
}
```

- **Basic / Standard**: lifetime limit of 5 accepted-connection contact unlocks.
- **Premium / Pro**: unlimited contact access after connection is accepted.
- **No active plan**: API returns fully locked cards (no identifiable data).
