# Install Cluster Visualization Dependencies

Write-Host "Installing Cluster Visualization Dependencies..." -ForegroundColor Green

# Frontend Dependencies
Write-Host "`nInstalling Frontend Dependencies..." -ForegroundColor Cyan
Push-Location "dhbw-automation\src\Frontend"
npm install plotly.js-dist-min
Pop-Location

# Python Dependencies
Write-Host "`nInstalling Python Dependencies..." -ForegroundColor Cyan
Push-Location "dhbw-automation\src\Backend\Python"

# Check if pip is available
if (Get-Command pip -ErrorAction SilentlyContinue) {
    pip install -r requirements.txt
} else {
    Write-Host "Warning: pip not found. Please install Python dependencies manually:" -ForegroundColor Yellow
    Write-Host "  pip install numpy scikit-learn umap-learn" -ForegroundColor Yellow
}

Pop-Location

Write-Host "`nDependencies installation complete!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Rebuild backend: cd dhbw-automation && docker-compose build"
Write-Host "2. Start services: docker-compose up -d"
Write-Host "3. Open Knowledge Network in browser: http://localhost:5173/knowledge-network"
