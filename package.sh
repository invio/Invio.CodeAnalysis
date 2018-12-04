#!/usr/bin/env bash

#exit if any command fails
set -e

artifactsFolder="./artifacts"

if [ -d $artifactsFolder ]; then
  rm -R $artifactsFolder
fi

dotnet pack src/Invio.CodeAnalysis/Invio.CodeAnalysis.csproj -c Release -o ../../artifacts
