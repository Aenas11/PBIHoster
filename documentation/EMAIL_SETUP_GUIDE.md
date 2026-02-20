# Email Notification Setup Guide

## Overview

PBIHoster uses SMTP to send dataset refresh notifications. This guide covers setup for common email providers and troubleshooting.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Step-by-Step Setup](#step-by-step-setup)
- [Provider Configuration](#provider-configuration)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Security Best Practices](#security-best-practices)

---

## Prerequisites

- SMTP server access with valid credentials
- Network access to SMTP port (usually 587 or 25)
- Admin access to PBIHoster configuration
- For corporate environments: approval from IT/Security team

---

## Step-by-Step Setup

### 1. Obtain SMTP Credentials

Your email provider will give you:
- **Host**: SMTP server hostname
- **Port**: Usually 587 (TLS) or 25 (unencrypted) - use 587
- **From Address**: Email to send from (must be verified)
- **Username**: SMTP authentication username
- **Password**: SMTP authentication password or app-specific password

### 2. Configure PBIHoster

Edit your configuration file or set environment variables:

**Option A: Environment Variables** (Recommended for Docker)
```bash
export EMAIL_ENABLED=true
export EMAIL_HOST=smtp.gmail.com
export EMAIL_PORT=587
export EMAIL_FROM_ADDRESS=your-email@gmail.com
export EMAIL_FROM_NAME="PBI Hoster"
export EMAIL_USE_SSL=true
export EMAIL_USERNAME=your-email@gmail.com
export EMAIL_PASSWORD=your-app-password
```

**Option B: appsettings.json**
```json
{
  "Email": {
    "Enabled": true,
    "Host": "smtp.gmail.com",
    "Port": 587,
    "FromAddress": "your-email@gmail.com",
    "FromName": "PBI Hoster",
    "UseSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

### 3. Restart Application

```bash
# Docker
docker restart pbihoster

# Or .NET CLI
dotnet run
```

### 4. Create a Refresh Schedule with Email

1. Log in as Admin
2. Go to **Admin > Settings > Data Refresh**
3. Click **Create schedule**
4. Fill in:
   - Name: "Test Email Notification"
   - Dataset ID: (select a dataset)
   - Cron: `0 */4 * * *` (every 4 hours - for testing, use `*/5 * * * *` for every 5 minutes)
5. Scroll to "Notification targets"
6. Enter: `email:your-inbox@example.com`
7. Check "Notify on failure"
8. Click **Create schedule**

### 5. Trigger a Test Refresh

1. In the Data Refresh table, find your test schedule
2. Click **Run now**
3. Wait a few seconds, then check your inbox

✅ If you receive an email, email is working!

---

## Provider Configuration

### Gmail (Recommended for Testing)

**Advantages:**
- Free tier available
- Easy setup with App Passwords
- Reliable delivery

**Steps:**

1. **Enable 2-Factor Authentication**
   - Sign in to myaccount.google.com
   - Click "Security" (left menu)
   - Enable 2-Step Verification (follow prompts)

2. **Create App Password**
   - In Security settings, find "App passwords"
   - Device: Select "Mail"
   - OS: Select "Windows PC" (or your OS)
   - Google generates a 16-character password
   - **Copy this password** (you'll use it once)

3. **Configure PBIHoster**
   ```
   EMAIL_HOST=smtp.gmail.com
   EMAIL_PORT=587
   EMAIL_FROM_ADDRESS=your-email@gmail.com
   EMAIL_USERNAME=your-email@gmail.com
   EMAIL_PASSWORD=xxxx-xxxx-xxxx-xxxx  (the 16-char app password)
   EMAIL_USE_SSL=true
   ```

4. **Test**
   - Create a refresh schedule
   - Add email target: `email:your-inbox@gmail.com`
   - Click "Run now"
   - Check inbox (may take 10-30 seconds)

### Office 365

**Advantages:**
- Works with corporate email
- Integrated with Azure AD (future enhancement)
- Professional appearance

**Steps:**

1. **Enable Modern Authentication** (usually default)
   - Ask your IT admin if SMTP Auth is enabled in Exchange Online

2. **Configure PBIHoster**
   ```
   EMAIL_HOST=smtp.office365.com
   EMAIL_PORT=587
   EMAIL_FROM_ADDRESS=your-email@company.com
   EMAIL_USERNAME=your-email@company.com
   EMAIL_PASSWORD=your-company-password
   EMAIL_USE_SSL=true
   ```

3. **Troubleshooting If It Fails**
   - Check with IT: "Is SMTP Authentication enabled?"
   - Try port 25 instead of 587 (less secure)
   - Add PBIHoster server IP to Exchange whitelist

### SendGrid (Cloud SMTP Service)

**Advantages:**
- Scalable for production
- High delivery rate
- Free tier: 100 emails/day

**Steps:**

1. **Create Account**
   - Sign up at sendgrid.com
   - Complete email verification

2. **Create API Key**
   - In Settings, click "API Keys"
   - Create a "Full Access" key
   - Copy the key (save it somewhere safe)

3. **Configure PBIHoster**
   ```
   EMAIL_HOST=smtp.sendgrid.net
   EMAIL_PORT=587
   EMAIL_FROM_ADDRESS=noreply@company.com (must be verified in SendGrid)
   EMAIL_USERNAME=apikey
   EMAIL_PASSWORD=SG.your-api-key-here
   EMAIL_USE_SSL=true
   ```

4. **Verify From Address**
   - In SendGrid > Sender Authentication, add your from address
   - Click verification link in email

### AWS SES (Amazon Simple Email Service)

**Advantages:**
- Pay-as-you-go pricing
- Integrates with AWS infrastructure
- Reliable

**Steps:**

1. **Create SMTP Credentials**
   - In AWS SES console, go to Account Dashboard
   - Click "Create My SMTP Credentials"
   - Select "us-east-1" region
   - Download credentials

2. **Configure PBIHoster**
   ```
   EMAIL_HOST=email-smtp.us-east-1.amazonaws.com
   EMAIL_PORT=587
   EMAIL_FROM_ADDRESS=verified-email@company.com
   EMAIL_USERNAME=AKIAIOSFODNN7EXAMPLE (from credentials)
   EMAIL_PASSWORD=your-smtp-password (from credentials)
   EMAIL_USE_SSL=true
   ```

3. **Verify Sender Email**
   - In AWS SES, verify your from address
   - Check verification email

### Custom SMTP Server (On-Premises)

**For corporate SMTP servers or relay hosts:**

```
EMAIL_HOST=mail.company.local (or IP)
EMAIL_PORT=25 (or 587 for TLS)
EMAIL_FROM_ADDRESS=pbihoster@company.local
EMAIL_USERNAME=service-account@company.local
EMAIL_PASSWORD=secure-password
EMAIL_USE_SSL=false (if internal/no TLS)
```

---

## Testing

### Manual Test from CLI

```bash
# In PBIHoster directory
dotnet test

# Or create a test endpoint:
curl -X POST http://localhost:5001/api/test-email \
  -H "Content-Type: application/json" \
  -d '{"to":"your@email.com"}'
```

### Verify Configuration

1. Check application logs:
   ```bash
   # Docker
   docker logs pbihoster | grep -i email
   
   # Kubernetes
   kubectl logs -f pod/pbihoster | grep -i email
   ```

2. Expected log output if configured correctly:
   ```
   Email service initialized: Host=smtp.gmail.com, Port=587, SSL=true
   ```

3. Create a test refresh schedule and check logs:
   ```
   Starting dataset refresh...
   Sending success notification email to user@example.com
   Email sent successfully
   ```

### Test Email Checklist

- [ ] Email received within 30 seconds
- [ ] From address is correct
- [ ] Subject line includes schedule name
- [ ] Body contains refresh status, dataset ID, duration
- [ ] Email formatted correctly (not HTML errors)

---

## Troubleshooting

### Issue: "Email is disabled"

**Cause**: `EMAIL_ENABLED` is not `true`

**Solution**:
```bash
export EMAIL_ENABLED=true
# Then restart
docker restart pbihoster
```

### Issue: "Email configuration is incomplete"

**Cause**: Missing `EMAIL_HOST` or `EMAIL_FROM_ADDRESS`

**Solution**:
1. Check all EMAIL_* environment variables are set
2. Verify no typos in variable names
3. Restart application

### Issue: "SMTP Connection Timeout"

**Cause**: Network cannot reach SMTP server

**Solutions**:
1. **Test connectivity from server:**
   ```bash
   # From PBIHoster server
   telnet smtp.gmail.com 587
   # Should connect (not instant timeout)
   ```

2. **Check firewall rules:**
   - Corporate firewall may block port 587
   - Ask IT to whitelist SMTP server
   - Try port 25 (less secure, often less restricted)

3. **DNS issues:**
   ```bash
   nslookup smtp.gmail.com
   # Should resolve to IP address
   ```

4. **Proxy issues** (corporate environments):
   - If corporate proxy required, may not work with SMTP
   - Ask IT for SMTP relay server on internal network

### Issue: "Authentication failed" / SMTP 535 Error

**Cause**: Wrong username or password

**Solutions**:

**For Gmail:**
- Did you create an App Password? (not regular password)
- Is 2FA enabled? (required for App Passwords)
- Did you copy the 16-char password exactly?
  ```bash
  # Correct format with dashes (no extra spaces):
  EMAIL_PASSWORD=xxxx-xxxx-xxxx-xxxx
  ```

**For Office 365:**
- Use full email: `user@company.com`
- If password has special chars, quote it:
  ```bash
  EMAIL_PASSWORD="Pa$$w0rd!@#"
  ```

**For SendGrid:**
- Username MUST be exactly `apikey`
- Password MUST start with `SG.`
  ```bash
  EMAIL_USERNAME=apikey
  EMAIL_PASSWORD=SG.1234567890abcdefghij...
  ```

### Issue: Email Not Received (But No Error)

**Causes:**

1. **Email in spam/junk folder**
   - Check spam folder
   - Add sender to contacts
   - Ask email provider to whitelist

2. **Email filter blocks it**
   - Corporate email filters may block notifications
   - Ask IT to allow notifications from PBIHoster domain
   - Whitelist sender address

3. **Refresh schedule never triggerd**
   - Check cron expression: `0 3 * * *` = 3AM daily
   - Verify schedule is "Enabled" (green tag)
   - Check server time is correct

4. **Notification disabled on schedule**
   - In Data Refresh, click schedule
   - Check "Notify on success" or "Notify on failure"
   - Re-save schedule

### Issue: "Emails sending but going to spam"

**Solutions:**

1. **Add SPF record** (if using custom domain)
   ```
   v=spf1 include:sendgrid.net ~all
   ```

2. **Configure DKIM**
   - Vary by provider (Gmail, SendGrid, SES have built-in)
   - Ask provider for DKIM record

3. **Use professional from address**
   - Instead of: `noreply@gmail.com`
   - Use: `reports@company.com` (verified domain)

### Issue: Gmail "Less secure app access" errors

**Solution:**

If using regular Gmail password (not App Password):
1. Go to myaccount.google.com
2. Click "Less secure app access"
3. Enable "Allow less secure apps"
4. Wait 10 minutes for change to apply

**Recommended**: Use App Passwords instead (more secure)

---

## Security Best Practices

### 1. Never Commit Credentials
```bash
# ❌ Don't do this:
git add appsettings.json  # Contains passwords!

# ✅ Do this instead:
export EMAIL_PASSWORD=your-password
# Or use secrets manager
```

### 2. Use Environment Variables

```bash
# ✅ Good - variables are not committed
export EMAIL_PASSWORD=your-password

# ❌ Bad - hardcoded in file
"Password": "your-password"
```

### 3. Use App-Specific Passwords

```bash
# ✅ Best - rotates regularly
Gmail App Password: xxxx-xxxx-xxxx-xxxx

# ⚠️ Less secure - account password uses everywhere
Regular password
```

### 4. Rotate Credentials Regularly

- Change SMTP passwords every 90 days
- For service accounts, rotate app passwords quarterly
- Use strong passwords (20+ chars, mixed case, numbers, symbols)

### 5. Use TLS/SSL Encryption

```bash
# ✅ Always use TLS
EMAIL_PORT=587          # TLS required
EMAIL_USE_SSL=true

# ❌ Avoid unencrypted
EMAIL_PORT=25           # Unencrypted (unless internal)
EMAIL_USE_SSL=false
```

### 6. Limit Email Targets

Only add notification emails for admins or authorized users:
```
email:admin@company.com
email:ops-team@company.com
```

NOT:
```
# ❌ Don't include all users
email:all-users@company.com
```

### 7. Monitor for Abuse

Check audit logs for failed email sends:
- Admin > Audit Logs > Filter by action: "DATASET_REFRESH_RUN"
- Look for repeated failures (potential attack)
- Disable if abuse suspected

### 8. Use Webhook Alerts for Production

For critical systems, prefer webhooks:
```
webhook:https://your-alerting-system.com/refresh-hook
```

Webhooks are:
- Only sent within your network (more secure)
- Easier to rate-limit
- Can authenticate with custom headers

---

## Advanced: Docker Compose Configuration

**File**: `docker-compose.yml`

```yaml
services:
  pbihoster:
    image: pbihoster:latest
    environment:
      - EMAIL_ENABLED=true
      - EMAIL_HOST=smtp.gmail.com
      - EMAIL_PORT=587
      - EMAIL_FROM_ADDRESS=noreply@pbihoster.com
      - EMAIL_FROM_NAME=PBI Hoster
      - EMAIL_USE_SSL=true
      - EMAIL_USERNAME=${SMTP_USERNAME}
      - EMAIL_PASSWORD=${SMTP_PASSWORD}
    # ... rest of config
```

**File**: `.env`

```
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=xxxx-xxxx-xxxx-xxxx
```

**Deploy**:
```bash
docker-compose up -d pbihoster
```

---

## What If I Don't Want Email?

Email is **completely optional**. If you don't configure it:

1. ✅ Email section in notification UI simply won't work
2. ✅ Webhooks will still function
3. ✅ No error messages or warnings
4. ✅ Application runs normally

To disable:
```bash
EMAIL_ENABLED=false
# or don't set EMAIL_HOST
```

---

## Next Steps

1. ✅ Configure SMTP credentials (pick a provider above)
2. ✅ Set environment variables
3. ✅ Restart PBIHoster
4. ✅ Create a test refresh schedule
5. ✅ Trigger "Run now" to receive test email
6. ✅ Check audit logs for success

**Questions?** Check the [Troubleshooting](#troubleshooting) section or review [DEPLOYMENT.md](./DEPLOYMENT.md) for more info.
