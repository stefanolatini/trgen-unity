## 1.0.1 (upcoming)

Fixes:

  - **BREAKING CHANGE**: `StartTrigger()`, `StartTriggerAsync()`, `StartTriggerList()` e `StartTriggerListAsync()` ora includono automaticamente una chiamata `Stop()` al termine dell'operazione per evitare conflitti con altre librerie (es. Python) che accedono al dispositivo
  - Risolto problema di timeout con la libreria Python dopo l'uso di `StartTrigger()` da Unity

## 1.0.0 (unreleased)

Features:

  - added support for basic TrGEN connectivity
  - added info for package distribution and examples
