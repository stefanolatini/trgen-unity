module.exports = {
  title: 'TrGEN Unity Documentation',
  description: 'Documentazione completa per il package Unity TrGEN di CoSANLab',
  base: '/trgen-unity/',
  head: [
    ['link', { rel: 'icon', href: '/favicon.ico' }],
    ['meta', { name: 'theme-color', content: '#3eaf7c' }],
    ['meta', { name: 'apple-mobile-web-app-capable', content: 'yes' }],
    ['meta', { name: 'apple-mobile-web-app-status-bar-style', content: 'black' }]
  ],
  
  themeConfig: {
    repo: 'stefanolatini/trgen-unity',
    editLinks: true,
    docsDir: 'docs',
    editLinkText: 'Modifica questa pagina su GitLab',
    lastUpdated: 'Ultimo aggiornamento',
    
    nav: [
      {
        text: 'Home',
        link: '/'
      },
      {
        text: 'Guida',
        link: '/guide/'
      },
      {
        text: 'API Reference',
        link: '/api/'
      },
      {
        text: 'Esempi',
        link: '/examples/'
      },
      {
        text: 'Changelog',
        link: '/changelog/'
      }
    ],
    
    sidebar: {
      '/guide/': [
        {
          title: 'Guida',
          collapsable: false,
          children: [
            '',
            'installation',
            'quickstart',
            'configuration',
            'troubleshooting'
          ]
        }
      ],
      
      '/api/': [
        {
          title: 'API Reference',
          collapsable: false,
          children: [
            '',
            'TrgenClient',
            'TrgenConfiguration',
            'TrgenImplementation',
            'TrgenPort'
          ]
        }
      ],
      
      '/examples/': [
        {
          title: 'Esempi',
          collapsable: false,
          children: [
            '',
            'basic-connection',
            'configuration-management',
            'trigger-sequences',
            'error-handling'
          ]
        }
      ]
    }
  },
  
  plugins: [
    '@vuepress/plugin-back-to-top',
    '@vuepress/plugin-medium-zoom',
    [
      '@vuepress/plugin-search',
      {
        searchMaxSuggestions: 10
      }
    ]
  ],
  
  markdown: {
    lineNumbers: true,
    anchor: {
      permalink: true,
      permalinkBefore: true,
      permalinkSymbol: '#'
    }
  }
}