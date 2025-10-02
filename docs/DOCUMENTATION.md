# 📖 Documentation Guide

This directory contains the source files for the TrGEN Unity documentation website.

## 🌐 Live Documentation

The documentation is automatically published to GitHub Pages at:
**https://stefanolatini.github.io/trgen-unity/**

## 📁 Structure

```
docs/
├── README.md              # Homepage content
├── _sidebar.md           # Navigation sidebar
├── _navbar.md            # Top navigation
├── _coverpage.md         # Cover page design
├── .nojekyll             # GitHub Pages configuration
├── api/                  # API reference documentation
│   ├── README.md
│   ├── TrgenClient.md
│   ├── TrgenPort.md
│   └── Configuration.md
└── examples/             # Usage examples
    ├── README.md
    ├── basic-setup.md
    ├── simple-triggers.md
    └── configuration.md
```

## 🔄 Automatic Updates

Documentation is automatically updated when you push changes to:
- `README.md` (main project README)
- `CHANGELOG.md`
- `docs/**` (any file in this directory)
- `Documentation/**` (legacy documentation folder)

## 🛠️ Local Development

To preview documentation locally:

```bash
# Install docsify CLI globally
npm install -g docsify-cli

# Navigate to docs directory  
cd docs

# Start local server
docsify serve . --port 3000

# Open http://localhost:3000 in browser
```

## ✨ Features

The documentation site includes:

- **🔍 Search** - Full-text search across all pages
- **📱 Responsive** - Mobile-friendly design
- **🎨 Syntax Highlighting** - C# code examples with proper highlighting
- **📋 Copy Code** - One-click code copying
- **📖 Pagination** - Previous/Next navigation
- **🔗 Edit Links** - Direct links to edit pages on GitHub
- **⭐ GitHub Integration** - Repository stars and links

## 📝 Writing Guidelines

### Markdown Style

- Use clear, descriptive headings
- Include code examples for all features
- Add emoji icons for visual appeal (🎯 📚 ⚡ etc.)
- Use tables for reference information
- Include troubleshooting sections

### Code Examples

```csharp
// Always include complete, runnable examples
public class ExampleClass : MonoBehaviour
{
    private TrgenClient client;
    
    void Start()
    {
        client = new TrgenClient();
        client.Connect();
    }
}
```

### Documentation Standards

1. **API Documentation** - Include all public methods, properties, and parameters
2. **Examples** - Provide practical, real-world usage scenarios  
3. **Error Handling** - Show proper error handling patterns
4. **Performance Notes** - Include timing and optimization tips

## 🚀 Deployment

Documentation is automatically deployed via GitHub Actions:

1. **Trigger**: Push to `main` branch with documentation changes
2. **Build**: GitHub Actions workflow builds static site using Docsify
3. **Deploy**: Automatically deploys to GitHub Pages
4. **Live**: Available at https://stefanolatini.github.io/trgen-unity/

## 🐛 Troubleshooting

### Local Preview Issues

```bash
# Clear npm cache
npm cache clean --force

# Reinstall docsify
npm uninstall -g docsify-cli
npm install -g docsify-cli

# Check Node.js version (requires Node 12+)
node --version
```

### Deployment Issues

1. Check GitHub Actions tab for build errors
2. Ensure GitHub Pages is enabled in repository settings
3. Verify source is set to "GitHub Actions"
4. Check file permissions and paths

### Content Issues

- Verify Markdown syntax with a linter
- Check internal links use correct relative paths
- Ensure code blocks specify language for highlighting
- Test search functionality with common terms

---

For questions about the documentation system, please open an issue in the main repository.