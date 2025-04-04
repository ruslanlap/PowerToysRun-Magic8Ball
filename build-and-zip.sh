#!/bin/bash

set -e  # Зупинити скрипт при першій помилці

PROJECT_PATH="Magic8Ball/Community.PowerToys.Run.Plugin.Magic8Ball/Community.PowerToys.Run.Plugin.Magic8Ball.csproj"
OUT_ROOT="./Magic8Ball/Community.PowerToys.Run.Plugin.Magic8Ball/bin"
DEST_DIR="./Magic8Ball/Publish"  # Нова папка для зручного розміщення файлів

# 1. Побудова для x64
echo "🛠️  Building for x64..."
dotnet publish "$PROJECT_PATH" -c Release -r win-x64 -p:Platform=x64 -p:PlatformTarget=x64

# 2. Побудова для ARM64
echo "🛠️  Building for ARM64..."
dotnet publish "$PROJECT_PATH" -c Release -r win-arm64 -p:Platform=ARM64 -p:PlatformTarget=ARM64

# 3. Копіювання файлів з папки publish до зручного місця
echo "📂 Copying published files to $DEST_DIR..."
rm -rf "$DEST_DIR"
mkdir -p "$DEST_DIR"
# Копіюємо файли для x64 (за потреби можна зробити і для ARM64)
PUBLISH_X64="$OUT_ROOT/x64/Release/net9.0-windows10.0.22621.0/win-x64/publish"
cp -r "$PUBLISH_X64"/* "$DEST_DIR"

# 4. Архівування файлів із папки DEST_DIR
echo "📦 Zipping results..."
ZIP_X64="./Magic8Ball-x64.zip"
zip -r "$ZIP_X64" "$DEST_DIR"/*

# Якщо потрібно створити окремий zip для ARM64, можна розкоментувати нижче:
# DEST_DIR_ARM64="./Magic8Ball/Publish_ARM64"
# rm -rf "$DEST_DIR_ARM64"
# mkdir -p "$DEST_DIR_ARM64"
# PUBLISH_ARM64="$OUT_ROOT/ARM64/Release/net9.0-windows10.0.22621.0/win-arm64/publish"
# cp -r "$PUBLISH_ARM64"/* "$DEST_DIR_ARM64"
# ZIP_ARM64="./Magic8Ball-ARM64.zip"
# zip -r "$ZIP_ARM64" "$DEST_DIR_ARM64"/*

echo "✅ Done! Created:"
echo " - $ZIP_X64"
# echo " - $ZIP_ARM64"  # Розкоментуйте, якщо використовуєте ARM64
