# Razorpay webhook setup (production)

1. In [Razorpay Dashboard](https://dashboard.razorpay.com) go to **Settings → Webhooks**.
2. Create a webhook with URL: `https://YOUR_DOMAIN/api/payment/razorpay/webhook`
3. Enable events: **payment.captured**, **order.paid**
4. Copy the **Webhook Secret** into configuration:
   - Environment variable: `RazorpaySettings__WebhookSecret`
   - Or `appsettings.Production.json` (do not commit secrets)
5. Set `RazorpaySettings:WebhookUrl` to the same public URL for your records.
6. Use **live** API keys (`rzp_live_*`) in production.

The API verifies `X-Razorpay-Signature` and activates membership if the user closes the browser before `/razorpay/verify` runs.
