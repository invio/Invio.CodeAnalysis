#!/usr/bin/env bash

#exit if any command fails
set -e

project="Invio.CodeAnalysis"

dotnet test ./test/${project}.Test/${project}.Test.csproj \
  --configuration Release \
  /p:CollectCoverage="true" \
  /p:CoverletOutputFormat="opencover" \
  /p:CoverletOutput="../../coverage/coverage.opencover.xml"
