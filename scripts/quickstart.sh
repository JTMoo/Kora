#!/usr/bin/env bash
# Quick start for macOS / Linux: checks prerequisites, restores/builds the API,
# installs the web app's dependencies, and prints the commands to run it.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

fail=0

check() {
	if ! command -v "$1" >/dev/null 2>&1; then
		echo "Missing: $1 ($2)"
		fail=1
	fi
}

check dotnet "https://dotnet.microsoft.com/download/dotnet/10.0"
check node "https://nodejs.org"
check npm "https://nodejs.org"
check psql "PostgreSQL client, e.g. 'brew install postgresql@16' or 'apt install postgresql-client'"

if [ "$fail" -ne 0 ]; then
	echo "Install the missing tools above, then re-run this script."
	exit 1
fi

if ! psql "postgresql://postgres:postgres@127.0.0.1:5432/postgres" -c '\q' >/dev/null 2>&1; then
	echo "Can't reach PostgreSQL at 127.0.0.1:5432 as postgres/postgres."
	echo "Start PostgreSQL and make sure that user/password exist, or override"
	echo "ConnectionStrings:Postgres in StockManagement.Api/appsettings.local.json."
	exit 1
fi

echo "Restoring .NET solution..."
dotnet restore StockManagement.sln

echo "Installing web app dependencies..."
(cd StockManagement.Web && npm ci)

cat <<'EOF'

Setup done. Start the app with two terminals:
  1) dotnet run --project StockManagement.Api
  2) cd StockManagement.Web && npm run dev

Then open the URL npm run dev prints and log in with admin / ChangeMe123!
EOF
