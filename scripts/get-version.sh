#!/usr/bin/env bash

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 [1|2]"
    exit 1
fi

if [ "$1" -eq 1 ]; then
    CSPROJ_FILE="backend/src/Eduva.API/Eduva.API.csproj"
else
    echo "Invalid option"
    exit 1
fi

if [ ! -f "$CSPROJ_FILE" ]; then
    echo "File not found: $CSPROJ_FILE"
    exit 1
fi

VERSION=$(grep -oP '<Version>\K[^<]+' "$CSPROJ_FILE")
echo "$VERSION"
