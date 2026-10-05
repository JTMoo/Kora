# Quick start for Windows: checks prerequisites, restores/builds the API,
# installs the web app's dependencies, and prints the commands to run it.
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

$missing = $false

function Check-Command($name, $hint) {
	if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
		Write-Host "Missing: $name ($hint)"
		$script:missing = $true
	}
}

Check-Command dotnet "https://dotnet.microsoft.com/download/dotnet/10.0"
Check-Command node "https://nodejs.org"
Check-Command npm "https://nodejs.org"
Check-Command psql "PostgreSQL client, e.g. https://www.postgresql.org/download/windows/"

if ($missing) {
	Write-Host "Install the missing tools above, then re-run this script."
	exit 1
}

$env:PGPASSWORD = "postgres"
psql "postgresql://postgres@127.0.0.1:5432/postgres" -c "\q" *> $null
if ($LASTEXITCODE -ne 0) {
	Write-Host "Can't reach PostgreSQL at 127.0.0.1:5432 as postgres/postgres."
	Write-Host "Start PostgreSQL and make sure that user/password exist, or override"
	Write-Host "ConnectionStrings:Postgres in StockManagement.Api/appsettings.local.json."
	exit 1
}

Write-Host "Restoring .NET solution..."
dotnet restore StockManagement.sln
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "Installing web app dependencies..."
Push-Location StockManagement.Web
npm ci
if ($LASTEXITCODE -ne 0) { Pop-Location; exit 1 }
Pop-Location

Write-Host ""
Write-Host "Setup done. Start the app with two terminals:"
Write-Host "  1) dotnet run --project StockManagement.Api"
Write-Host "  2) cd StockManagement.Web; npm run dev"
Write-Host ""
Write-Host "Then open the URL npm run dev prints and log in with admin / ChangeMe123!"
