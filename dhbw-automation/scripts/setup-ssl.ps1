# Setup SSL Certificates for my-dhbw.com
# This script helps you set up SSL certificates using Let's Encrypt

param(
    [string]$Server = "192.168.178.198",
    [string]$Domain = "my-dhbw.com"
)

Write-Host "=== SSL Certificate Setup ===" -ForegroundColor Green
Write-Host ""

Write-Host "Option 1: Self-Signed Certificate (for testing)" -ForegroundColor Yellow
Write-Host "Option 2: Let's Encrypt Certificate (for production)" -ForegroundColor Yellow
Write-Host ""

$choice = Read-Host "Choose option (1 or 2)"

if ($choice -eq "1") {
    Write-Host "`nGenerating self-signed certificate..." -ForegroundColor Cyan
    
    ssh root@$Server @"
mkdir -p /root/dhbw-automation-deploy/dhbw-automation/docker/ssl
cd /root/dhbw-automation-deploy/dhbw-automation/docker/ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout key.pem \
    -out cert.pem \
    -subj "/C=DE/ST=Baden-Wuerttemberg/L=Stuttgart/O=DHBW/CN=$Domain"
chmod 600 key.pem cert.pem
"@
    
    Write-Host "`n✓ Self-signed certificate created!" -ForegroundColor Green
    Write-Host "Note: Browsers will show a security warning. This is normal for self-signed certificates." -ForegroundColor Yellow
}
elseif ($choice -eq "2") {
    Write-Host "`n=== Let's Encrypt Setup ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Prerequisites:" -ForegroundColor Yellow
    Write-Host "1. Domain $Domain must point to $Server" -ForegroundColor White
    Write-Host "2. Port 80 must be accessible from the internet" -ForegroundColor White
    Write-Host "3. Port 440 must be accessible from the internet" -ForegroundColor White
    Write-Host ""
    
    $email = Read-Host "Enter your email for Let's Encrypt notifications"
    
    Write-Host "`nInstalling certbot..." -ForegroundColor Cyan
    
    ssh root@$Server @"
apt-get update
apt-get install -y certbot
mkdir -p /root/dhbw-automation-deploy/dhbw-automation/docker/ssl
certbot certonly --standalone \
    --preferred-challenges http \
    --email $email \
    --agree-tos \
    --no-eff-email \
    -d $Domain \
    -d www.$Domain
    
# Copy certificates to docker folder
cp /etc/letsencrypt/live/$Domain/fullchain.pem /root/dhbw-automation-deploy/dhbw-automation/docker/ssl/cert.pem
cp /etc/letsencrypt/live/$Domain/privkey.pem /root/dhbw-automation-deploy/dhbw-automation/docker/ssl/key.pem
chmod 600 /root/dhbw-automation-deploy/dhbw-automation/docker/ssl/*.pem
"@
    
    Write-Host "`n✓ Let's Encrypt certificate installed!" -ForegroundColor Green
    Write-Host "`nTo auto-renew, add this cron job on the server:" -ForegroundColor Yellow
    Write-Host "0 3 * * * certbot renew --quiet && cp /etc/letsencrypt/live/$Domain/fullchain.pem /root/dhbw-automation-deploy/dhbw-automation/docker/ssl/cert.pem && cp /etc/letsencrypt/live/$Domain/privkey.pem /root/dhbw-automation-deploy/dhbw-automation/docker/ssl/key.pem && docker restart dhbw-nginx-proxy" -ForegroundColor White
}
else {
    Write-Host "Invalid choice. Exiting." -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Next Steps ===" -ForegroundColor Green
Write-Host "1. Deploy the updated configuration:"
Write-Host "   .\scripts\rebuild-and-deploy.ps1" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Access your site at:" -ForegroundColor White
Write-Host "   https://$Domain`:440" -ForegroundColor Cyan
Write-Host ""
