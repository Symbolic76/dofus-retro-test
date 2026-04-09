# Dofus Retro — Window Manager

Un outil Windows (.exe) pour gérer plusieurs fenêtres/personnages Dofus Retro simultanément.

## Fonctionnalités

| Fonctionnalité | Description |
|---|---|
| 👤 **Gestion des personnages** | Organisez l'ordre de jeu de vos personnages. L'ordre contrôle dans quelle séquence le raccourci clavier bascule entre les fenêtres. |
| ⌨ **Raccourci clavier global** | Configurez n'importe quelle touche (avec modificateurs Ctrl/Alt/Shift) pour passer à la fenêtre suivante — fonctionne même quand l'appli n'est pas au premier plan. |
| 🔔 **Auto-swap sur notification** | Surveille les titres des fenêtres Dofus. Dès qu'un mot-clé (tour, invitation, échange…) est détecté, la fenêtre concernée passe automatiquement au premier plan. |
| 💾 **Configuration JSON** | Tout est persisté dans `config.json` à côté de l'exe — aucune base de données. |
| 📋 **Journal d'activité** | Chaque swap, détection de notification et action est loggé avec horodatage. |

## Compilation

### Pré-requis
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (Windows)
- Visual Studio 2022+ **ou** `dotnet` CLI

### Depuis la ligne de commande

```powershell
# Cloner le repo
git clone https://github.com/Symbolic76/dofus-retro-test.git
cd dofus-retro-test

# Générer l'exe autonome (tout-en-un, ~60 Mo)
dotnet publish DofusRetroManager/DofusRetroManager.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  --output ./publish

# L'exe se trouve dans ./publish/DofusRetroManager.exe
```

### Depuis Visual Studio
1. Ouvrir `DofusRetroManager/DofusRetroManager.csproj`
2. `Build > Publish…` → choisir le profil *win-x64 self-contained*

### Télécharger l'exe pré-compilé
Chaque push sur `main` déclenche une CI GitHub Actions qui publie l'exe en tant qu'artefact :
**Actions → dernier run → Artifacts → `DofusRetroManager-win-x64`**

## Utilisation

1. **Lancer** `DofusRetroManager.exe`
2. Onglet **Personnages** : ajouter vos persos avec le texte unique de leur fenêtre Dofus (ex. le nom du perso qui apparaît dans le titre `Dofus Retro - MonPerso - …`)
3. Onglet **Raccourcis** : choisir la touche et cliquer *Appliquer*
4. Cliquer **▶ Démarrer** pour activer la surveillance des notifications
5. Jouer — le raccourci et l'auto-swap font le reste !

## Structure du projet

```
DofusRetroManager/
├── Models/
│   ├── Character.cs           # Modèle personnage
│   └── AppConfig.cs           # Modèle configuration
├── Services/
│   ├── ConfigService.cs       # Lecture/écriture config.json
│   ├── NativeWindowManager.cs # Win32 : énumération & switch de fenêtres
│   ├── HotkeyManager.cs       # Win32 : RegisterHotKey global
│   └── NotificationMonitor.cs # WinEvent hook : détection titres fenêtres
├── ViewModels/
│   └── MainViewModel.cs       # Logique UI (MVVM)
├── MainWindow.xaml(.cs)       # Interface WPF
├── App.xaml(.cs)
└── DofusRetroManager.csproj
```

## Configuration (`config.json`)

```json
{
  "Characters": [
    { "Name": "Iop1",  "WindowTitlePattern": "Iop1",  "Order": 0, "IsActive": true },
    { "Name": "Sacri", "WindowTitlePattern": "Sacri", "Order": 1, "IsActive": true }
  ],
  "HotkeyKey":   "Tab",
  "HotkeyCtrl":  false,
  "HotkeyAlt":   true,
  "HotkeyShift": false,
  "AutoSwapEnabled": true,
  "GameWindowTitlePattern": "Dofus",
  "NotificationPatterns": [
    { "Pattern": "tour",       "Type": 0 },
    { "Pattern": "invitation", "Type": 1 },
    { "Pattern": "échange",    "Type": 2 }
  ]
}
```

## Licence
MIT
