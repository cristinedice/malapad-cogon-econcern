# ==============================================================================
# Barangay Malapad Cogon e-CONCERN System - Automated Database & Upload Backup
# ==============================================================================

$backupRoot = Join-Path $PSScriptRoot "backups"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$targetFolder = Join-Path $backupRoot $timestamp

if (-not (Test-Path $targetFolder)) {
    New-Item -ItemType Directory -Path $targetFolder -Force | Out-Null
}

Write-Host "Starting Malapad Cogon e-Concern Backup ($timestamp)..." -ForegroundColor Cyan

# 1. Backup SQLite Database (Safe Copy)
$dbSource = Join-Path $PSScriptRoot "econcern.db"
if (Test-Path $dbSource) {
    $dbTarget = Join-Path $targetFolder "econcern_$timestamp.db"
    Copy-Item -Path $dbSource -Destination $dbTarget -Force
    Write-Host "[OK] Database successfully copied to: $dbTarget" -ForegroundColor Green
} else {
    Write-Host "[WARN] econcern.db was not found in project root." -ForegroundColor Yellow
}

# 2. Archive Uploaded Photos Directory
$uploadsSource = Join-Path $PSScriptRoot "wwwroot\uploads"
if (Test-Path $uploadsSource) {
    $zipTarget = Join-Path $targetFolder "uploads_$timestamp.zip"
    Compress-Archive -Path "$uploadsSource\*" -DestinationPath $zipTarget -Force
    Write-Host "[OK] Uploaded evidence photos archived to: $zipTarget" -ForegroundColor Green
} else {
    Write-Host "[INFO] No wwwroot/uploads directory found yet." -ForegroundColor Gray
}

Write-Host "Backup completed successfully! Location: $targetFolder" -ForegroundColor Green
