# 🌐 GitHub Pages Setup Instructions

Follow these steps to enable GitHub Pages for your TrGEN Unity documentation.

## ⚙️ Repository Settings

1. **Navigate to Settings**
   - Go to your repository: https://github.com/stefanolatini/trgen-unity
   - Click **"Settings"** tab
   - Scroll down to **"Pages"** section

2. **Configure Source**
   - Under **"Source"**, select **"GitHub Actions"**
   - This enables the automated workflow deployment

3. **Verify Configuration**
   - The workflow file is already created: `.github/workflows/docs.yml`
   - Documentation will be available at: https://stefanolatini.github.io/trgen-unity/

## 🚀 Automatic Deployment

The documentation will automatically rebuild and deploy when you:

- Push changes to the `main` branch
- Modify any of these files:
  - `README.md`
  - `CHANGELOG.md` 
  - Files in `docs/` directory
  - Files in `Documentation/` directory

## 🧪 Testing the Setup

1. **Trigger First Build**
   ```bash
   # Make any small change to README.md and push
   git add .
   git commit -m "docs: trigger initial GitHub Pages build"
   git push origin main
   ```

2. **Monitor Build Process**
   - Go to **"Actions"** tab in your repository
   - Watch the "📚 Deploy Documentation" workflow
   - Build typically takes 2-3 minutes

3. **Verify Deployment**
   - Check https://stefanolatini.github.io/trgen-unity/
   - Should show professional documentation site

## 🔧 Troubleshooting

### If GitHub Pages is not available:

1. **Check Repository Visibility**
   - GitHub Pages requires public repository OR GitHub Pro account
   - For private repos, upgrade to GitHub Pro

2. **Enable GitHub Pages**
   - Go to Settings → Pages
   - If option is missing, check repository permissions

3. **Workflow Permissions**
   - Go to Settings → Actions → General
   - Ensure "Read and write permissions" is enabled
   - Check "Allow GitHub Actions to create and approve pull requests"

### If build fails:

1. **Check Workflow Status**
   ```bash
   # View workflow in Actions tab
   # Look for error messages in build logs
   ```

2. **Common Issues**
   - File path problems (use forward slashes `/`)
   - Missing required files
   - Invalid YAML syntax in workflow

3. **Manual Fix**
   ```bash
   # Re-trigger workflow
   git commit --allow-empty -m "docs: retrigger workflow"
   git push origin main
   ```

## ✅ Success Checklist

- [ ] Repository is public or GitHub Pro account
- [ ] GitHub Pages enabled in Settings
- [ ] Source set to "GitHub Actions"
- [ ] Workflow file exists: `.github/workflows/docs.yml`
- [ ] First build completed successfully
- [ ] Documentation site accessible
- [ ] Search functionality working
- [ ] All navigation links functional

## 🎉 Next Steps

Once GitHub Pages is working:

1. **Share Documentation URL**
   - https://stefanolatini.github.io/trgen-unity/
   - Add to README badges
   - Include in package.json

2. **Customize Documentation**
   - Edit files in `docs/` directory
   - Add more examples and guides
   - Update API documentation

3. **Promote Your Package**
   - Submit to OpenUPM registry
   - Share with Unity community
   - Add documentation link to Unity Asset Store

---

**Need help?** Open an issue in the repository with the "documentation" label.