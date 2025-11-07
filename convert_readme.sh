#!/bin/bash
# build-docs.sh

echo "🔧 Preparando le directory..."
mkdir -p docs/source/_static
mkdir -p docs/source/_templates
mkdir -p public

echo "📚 Preprocessing e conversione README..."

# Rimuovi file precedenti
rm -f docs/source/api/index.rst
rm -rf docs/source/api/
rm -f docs/source/installation.rst
rm -f docs/source/quickstart.rst
rm -f docs/source/examples.rst
rm -f docs/source/changelog.rst

if [ -f README.md ]; then
    echo "Convertendo README.md a RST con preprocessing Mermaid..."
    
    # Usa il preprocessing script esistente
    python3 preprocess-md.py README.md README_processed.md
    
    # Converti da Markdown a RST
    m2r2 README_processed.md --overwrite
    
    if [ -f README_processed.rst ]; then
        echo "Copiando README_processed.rst come index.rst..."
        cp README_processed.rst docs/source/index.rst
        echo "✅ Conversione completata!"
    else
        echo "❌ Errore: README_processed.rst non trovato!"
        exit 1
    fi
else
    echo "❌ README.md non trovato!"
    exit 1
fi

echo "📝 Contenuto generato in index.rst (prime 20 righe):"
head -20 docs/source/index.rst

echo "🏗️ Building Sphinx documentation..."
cd docs
sphinx-build -b html source ../public --keep-going -v

if [ $? -eq 0 ]; then
    echo "✅ Documentazione generata con successo in ./public/"
    echo "🌐 Apri public/index.html nel browser per visualizzarla"
else
    echo "❌ Errore durante la generazione della documentazione"
    exit 1
fi