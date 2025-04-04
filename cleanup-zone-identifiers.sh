#!/bin/bash

# Find all Zone.Identifier files
echo "Finding Zone.Identifier files..."
ZONE_FILES=$(find . -name "*:Zone.Identifier")

# Count the files
COUNT=$(echo "$ZONE_FILES" | grep -v "^$" | wc -l)
echo "Found $COUNT Zone.Identifier files"

# Remove the files
if [ $COUNT -gt 0 ]; then
  echo "Removing Zone.Identifier files..."
  echo "$ZONE_FILES" | xargs rm -f
  echo "All Zone.Identifier files have been removed"
else
  echo "No Zone.Identifier files to remove"
fi
