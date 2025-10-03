# 📚 Guida Deploy GitHub Pages per TrGEN Unity

Questa guida spiega come configurare e utilizzare GitHub Pages per la documentazione TrGEN Unity.

## 🛠️ Setup Iniziale

### 1. Abilita GitHub Pages nel Repository

1. Vai su **Settings** del repository GitHub
2. Scorri fino alla sezione **Pages**
3. Sotto **Source**, seleziona **GitHub Actions**
4. Salva le impostazioni

### 2. Configurazione Repository

Il repository è già configurato con:
- ✅ **GitHub Actions workflow** (`.github/workflows/docs-deploy.yml`)
- ✅ **VuePress configuration** (`docs-site/.vuepress/config.js`)
- ✅ **Package dependencies** (`package-docs.json`)
- ✅ **Documentation content** (`docs-site/`)

## 🚀 Processo di Deploy

### Deploy Automatico

Il deploy avviene **automaticamente** quando:
- Fai push su branch `main`
- Modifichi file in `docs-site/` o `Runtime/`
- Modifichi il workflow stesso

### Deploy Manuale

Per triggerare un deploy manuale:
1. Vai su **Actions** nel repository GitHub
2. Seleziona il workflow "📚 Deploy VuePress Documentation to GitHub Pages"
3. Clicca su **Run workflow**
4. Clicca su **Run workflow** (conferma)

## 🔧 Sviluppo Locale

### Setup Ambiente di Sviluppo

```bash
# 1. Installa le dipendenze
cp package-docs.json package.json
npm install

# 2. Avvia il server di sviluppo
npm run dev

# 3. Apri http://localhost:8080/trgen-unity/
```

### Generazione Documentazione

```bash
# Genera documentazione da codice C#
python3 generate_legacy_docs.py --source Runtime --output docs-site

# Build per produzione
npm run build

# Preview build locale
npm run serve
```

## 📝 Struttura File

```
docs-site/
├── .vuepress/
│   └── config.js          # Configurazione VuePress
├── README.md              # Homepage
├── api/                   # Documentazione API generata
│   ├── README.md
│   ├── TrgenClient.md
│   ├── TrgenConfiguration.md
│   ├── TrgenImplementation.md
│   └── TrgenPort.md
├── guide/                 # Guide utente
│   ├── README.md
│   ├── installation.md
│   ├── quickstart.md
│   ├── configuration.md
│   └── troubleshooting.md
└── examples/              # Esempi di codice
    ├── README.md
    ├── basic-connection.md
    ├── configuration-management.md
    ├── trigger-sequences.md
    └── error-handling.md
```

## 🔍 Monitoraggio Deploy

### 1. Stato Build

- **GitHub Actions**: [Repository > Actions](https://github.com/stefanolatini/trgen-unity/actions)
- **Badge Status**: ![Build Status](https://github.com/stefanolatini/trgen-unity/workflows/📚%20Deploy%20VuePress%20Documentation%20to%20GitHub%20Pages/badge.svg)

### 2. URL Sito

- **Produzione**: https://stefanolatini.github.io/trgen-unity/
- **Verificare**: Dopo il deploy, controlla che il sito sia accessibile

### 3. Log di Deploy

```bash
# Controlla i log in GitHub Actions per:
✅ Build VuePress Documentation
✅ Deploy to GitHub Pages
✅ Validate Documentation (solo per PR)
```

## 🛠️ Troubleshooting

### Problemi Comuni

1. **404 su GitHub Pages**
   - Verifica che `base: '/trgen-unity/'` sia corretto in `config.js`
   - Controlla che GitHub Pages sia abilitato

2. **Build Fallisce**
   - Verifica sintassi Markdown nei file `docs-site/`
   - Controlla errori nel workflow Actions

3. **Link Rotti**
   - Usa percorsi relativi: `/api/TrgenClient.html`
   - Non percorsi assoluti

### Debug Locale

```bash
# Verifica configurazione VuePress
npm run dev

# Testa build produzione
npm run build
npm run serve

# Genera documentazione fresca
python3 generate_legacy_docs.py
```

## 📊 Metriche e Analytics

Il sito include:
- 🔍 **Ricerca integrata** VuePress
- 📱 **Responsive design** automatico
- ⚡ **Progressive Web App** features
- 🎯 **SEO ottimizzato** per GitHub Pages

## 🎉 Prossimi Passi

1. **Push del codice** per triggerare il primo deploy
2. **Verifica sito live** su GitHub Pages
3. **Personalizza** contenuti in `docs-site/`
4. **Aggiungi esempi** nella sezione `/examples/`

---

**🔗 Link Utili:**
- [VuePress Documentation](https://vuepress.vuejs.org/)
- [GitHub Pages Guide](https://docs.github.com/en/pages)
- [GitHub Actions](https://docs.github.com/en/actions)