# Minimal makefile for Sphinx documentation
#

# You can set these variables from the command line, and also
# from the environment for the first two.
SPHINXOPTS    ?=
SPHINXBUILD   ?= sphinx-build
SOURCEDIR     = source
BUILDDIR      = build

# Put it first so that "make" without argument is like "make help".
help:
    @$(SPHINXBUILD) -M help "$(SOURCEDIR)" "$(BUILDDIR)" $(SPHINXOPTS) $(O)

.PHONY: help Makefile

# Catch-all target: route all unknown targets to Sphinx-Build
%: Makefile
    @$(SPHINXBUILD) -M $@ "$(SOURCEDIR)" "$(BUILDDIR)" $(SPHINXOPTS) $(O)

# Custom targets for MATLAB documentation
clean:
    rm -rf $(BUILDDIR)/*
    rm -rf $(SOURCEDIR)/_autosummary
    rm -rf $(SOURCEDIR)/api/_autosummary

livehtml:
    sphinx-autobuild $(SOURCEDIR) $(BUILDDIR)/html $(SPHINXOPTS) $(O)