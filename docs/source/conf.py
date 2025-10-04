# Configuration file for the Sphinx documentation builder.
#
# For the full list of built-in configuration values, see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# -- Project information -----------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#project-information

# docs/source/conf.py
import os
import sys
sys.path.insert(0, os.path.abspath('../../')) 

project = 'TrGEN Unity'
copyright = '2025, CoSANLab / Stefano Latini'
author = 'CoSANLab / Stefano Latini'
release = '1.0.1'

# -- General configuration ---------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#general-configuration

extensions = [
    'sphinxsharp.sphinxsharp',  # Rimuovo la versione pro e correggo la sintassi
    'sphinxcontrib.mermaid',
    'sphinx.ext.autosummary',  # per generare automaticamente sommari
    'sphinx.ext.viewcode',  # per link al codice sorgente
]

# Support Markdown files (MyST)
# Install with: pip install myst-parser
extensions.insert(0, 'myst_parser')

templates_path = ['_templates']
exclude_patterns = []

# -- C# Domain Configuration ------------------------------------------------
primary_domain = "csharp"

# C# source paths - percorso ai file C# del progetto Unity
csharp_src_dir = os.path.abspath('../../Runtime')
csharp_short_links = True
csharp_auto_link = "basic"

# SphinxSharp Configuration
sphinxsharp_project_dir = os.path.abspath('../../')
sphinxsharp_source_dirs = ['Runtime']
sphinxsharp_output_dir = os.path.abspath('api')
sphinxsharp_exclude_patterns = ['obj/**', 'bin/**', 'Temp/**']

# Auto-generate summaries per C#
# NOTE: autosummary can try to import Python modules (fails for C# projects).
# Disable automatic generation to avoid import errors like "no module named src".
autosummary_generate = False
autosummary_imported_members = True

# -- Options for HTML output -------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#options-for-html-output

# Configurazione Mermaid con i tuoi colori
mermaid_output_format = 'raw'
mermaid_version = '10.6.1'
mermaid_init_js = """
mermaid.initialize({
    "theme": "base",
    "themeVariables": {
        "primaryColor": "#FF6600",
        "primaryTextColor": "#333333",
        "primaryBorderColor": "#FF6600",
        "lineColor": "#FF6600",
        "secondaryColor": "#FFF8F0",
        "tertiaryColor": "#FFE5CC"
    }
});
"""

html_theme = "furo"
html_static_path = ['_static']

html_theme_options = {
    "description": "Documentazione ufficiale TrGEN Unity 🧠",
    "light_css_variables": {
        "color-brand-primary": "#FF6600",
        "color-brand-content": "#333333",
        "color-admonition-background": "rgba(255, 102, 0, 0.1)",
    },
    "dark_css_variables": {
        "color-brand-primary": "#FF6600",
        "color-brand-content": "#DDDDDD",
        "color-admonition-background": "rgba(255, 102, 0, 0.1)",
    },
    "sidebar_hide_name": False,
    "navigation_with_keys": True,
    "top_of_page_button": "edit",
    "source_repository": "https://github.com/stefanolatini/trgen-unity/",
    "source_branch": "main",
    "source_directory": "docs/source/",
}

html_title = f"{project} v{release}"
html_short_title = "TrGEN Unity"

# Logo e favicon
html_logo = "_static/logo.png"
html_favicon = "_static/favicon.ico"

# Context per template personalizzati
html_context = {
    'display_github': True,
    'github_user': 'stefanolatini',
    'github_repo': 'trgen-unity',
    'github_version': 'main',
    'conf_py_path': '/docs/source/',
}