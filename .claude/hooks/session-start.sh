#!/bin/bash
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
	exit 0
fi

# .NET 10 SDK (dot.net's own installer is network-blocked here; apt works)
if ! command -v dotnet >/dev/null 2>&1; then
	apt-get update -qq
	apt-get install -y -qq dotnet-sdk-10.0
fi

dotnet restore StockManagement.sln

# PostgreSQL for StockManagement.Api / StockManagement.Api.Tests / web e2e
if ! command -v psql >/dev/null 2>&1; then
	apt-get update -qq
	apt-get install -y -qq postgresql
fi
service postgresql start >/dev/null 2>&1 || true
for i in $(seq 1 30); do
	if pg_isready -h 127.0.0.1 -p 5432 >/dev/null 2>&1; then
		break
	fi
	sleep 1
done
sudo -u postgres psql -tc "ALTER USER postgres WITH PASSWORD 'postgres';" >/dev/null 2>&1 || true

# Docker, for StockManagement.Api.Tests' Testcontainers-based Postgres
if command -v dockerd >/dev/null 2>&1 && ! docker info >/dev/null 2>&1; then
	dockerd >/var/log/dockerd.log 2>&1 &
	for i in $(seq 1 30); do
		docker info >/dev/null 2>&1 && break
		sleep 1
	done
fi

# Web app: npm deps + Playwright (browsers are preinstalled, skip the download)
echo 'export PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers' >> "$CLAUDE_ENV_FILE"
echo 'export PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1' >> "$CLAUDE_ENV_FILE"
(cd StockManagement.Web && npm install)
