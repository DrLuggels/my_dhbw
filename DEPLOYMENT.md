# Deployment Guide

## Server Details
- **Host**: root@192.168.178.198
- **Project Path**: /root/dhbw-automation-deploy/dhbw-automation
- **Compose File**: docker-compose.prod.yml

## Standard Deployment Workflow

### 1. Commit Changes
```bash
cd "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Ubungen\Projekte\my_dhbw"
git add <files>
git commit -m "Your commit message"
```

### 2. Build and Deploy Backend
```bash
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml build backend && docker compose -f docker-compose.prod.yml up -d backend"
```

### 3. Check Logs
```bash
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs --tail=100 backend"
```

## Quick Commands

### View Backend Logs
```bash
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs --tail=100 backend"
```

### Restart Backend
```bash
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml restart backend"
```

### Full Rebuild
```bash
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml down && docker compose -f docker-compose.prod.yml build && docker compose -f docker-compose.prod.yml up -d"
```

### Follow Logs (Real-time)
```bash
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs -f backend"
```

## Important Notes
- Always check logs after deployment to verify everything is running correctly
- The backend service needs to be rebuilt after C# code changes
- Use `.AsNoTracking()` in EF queries to prevent circular reference issues in JSON serialization
