#!/bin/bash

# 🧪 Script di test locale per la documentazione VuePress
# Mantiene intatto il package.json Unity/OpenUPM e usa package-docs.json per VuePress

echo "🧪 Testing VuePress Documentation Build"
echo "======================================"

# Verifica che esista package-docs.json
if [ ! -f "package-docs.json" ]; then
    echo "❌ Error: package-docs.json not found!"
    echo "This file contains VuePress dependencies for documentation build."
    exit 1
fi

# Verifica che esista il package.json Unity/OpenUPM
if [ -f "package.json" ]; then
    echo "� Found Unity/OpenUPM package.json - preserving it"
    # Backup del package.json Unity/OpenUPM
    cp package.json package-unity-openupm.json.backup
else
    echo "ℹ️ No existing package.json found"
fi

# Setup temporaneo per VuePress (sostituisce temporaneamente package.json)
echo "� Setting up VuePress environment with package-docs.json..."
cp package-docs.json package.json

# Installa dipendenze VuePress
echo "� Installing VuePress dependencies..."
npm install --production=false

# Test build documentazione
echo "🏗️ Building VuePress documentation..."
npm run build

# Verifica risultato build
if [ -d "public" ]; then
    echo "✅ VuePress build successful!"
    echo "📂 Generated files in public/:"
    ls -la public/ | head -10
    
    echo ""
    echo "🚀 To preview documentation locally:"
    echo "   npm run serve"
    echo "   # Then open http://localhost:3000"
    echo ""
    echo "📁 Documentation structure:"
    find public -name "*.html" | head -5
else
    echo "❌ Build failed - no public/ directory found"
    FAILED=1
fi

# IMPORTANTE: Ripristina sempre il package.json Unity/OpenUPM originale
echo "🔄 Restoring original Unity/OpenUPM package.json..."
if [ -f "package-unity-openupm.json.backup" ]; then
    mv package-unity-openupm.json.backup package.json
    echo "✅ Unity/OpenUPM package.json restored"
else
    # Se non c'era un package.json originale, rimuovi quello temporaneo
    rm -f package.json
    echo "🗑️ Temporary VuePress package.json removed"
fi

# Pulizia file temporanei VuePress
echo "🧹 Cleaning up VuePress temporary files..."
rm -f package-lock.json

# Exit con codice appropriato
if [ "$FAILED" = "1" ]; then
    echo "❌ Documentation build test failed!"
    exit 1
else
    echo "✅ Documentation build test completed successfully!"
    echo "💡 The Unity/OpenUPM package.json remains unchanged for OpenUPM publishing"
fi