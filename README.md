# Capture Studio

Outil Windows de capture et d'annotation pour préparer des tutoriels : capture de zone, de fenêtre ou de l'écran, retardateur, flèches, numéros, texte, loupes circulaires, surlignage, masques et flou local.

## Installer

Télécharger **CaptureStudio-Setup-0.15.1.exe** depuis [la dernière version](https://github.com/nferon/capture-studio/releases/latest). L’installateur demande à l’application de sauvegarder ses projets puis de quitter proprement. Annuler une sauvegarde annule l’installation. Les versions antérieures à 0.15.1 doivent être quittées manuellement via l’icône près de l’horloge une dernière fois.

L'installation se fait pour l'utilisateur courant dans `%LOCALAPPDATA%\Programs\CaptureStudio`, sans droits administrateur. Un raccourci est créé dans le menu Démarrer. Les réglages et brouillons sont conservés séparément dans `%LOCALAPPDATA%\CaptureStudio`. Les fichiers `.capture` enregistrés par l'utilisateur ne sont pas déplacés.

Windows 10/11 et .NET Framework 4.8 sont requis. L'OCR nécessite les composants OCR Windows et une langue prise en charge installée. Le paquet et l'installateur ne sont pas signés Authenticode à ce stade.

## Mises à jour

- Fichier > Rechercher les mises à jour.
- Vérification discrète au démarrage, au maximum une fois par jour ; désactivable dans Paramètres.
- L'application vérifie les versions stables du dépôt `nferon/capture-studio`.
- Après accord, l'installateur est téléchargé en HTTPS et son SHA-256 est comparé à l'empreinte fournie par l'API GitHub.
- Les captures ouvertes non enregistrées doivent être sauvegardées en projets avant le redémarrage. Annuler une sauvegarde annule la procédure de mise à jour.
- L'installateur attend la sortie de l'application, refuse d'écraser une instance encore active et redémarre Capture Studio après la mise à jour.

La vérification de téléchargement ne remplace pas une signature Authenticode. Aucun identifiant GitHub n'est nécessaire pour rechercher ou télécharger les versions publiques. La vérification contacte GitHub et lui transmet les informations réseau habituelles d'une requête HTTPS.

## Utiliser

- **Ouvrir / Ctrl+O** : PNG, JPEG, BMP et projets `.capture`.
- **Ctrl+S** : enregistrer le projet modifiable.
- **Alt+Maj+S** ou **Ctrl+Maj+S** : exporter un PNG. Un autre logiciel peut réserver l'une de ces combinaisons.
- **Ctrl+A / Ctrl+C / Ctrl+V** : sélectionner l'image, copier le rendu, importer depuis le presse-papiers.
- **+ / Ctrl+N** : nouvelle fenêtre indépendante. Un nouveau lancement ouvre aussi une nouvelle fenêtre.
- **Molette** : zoom ; **Espace + glisser** ou **clic molette maintenu** : déplacer la vue.
- **Vider l'image** : remettre uniquement cet éditeur à vide.

En mode Fenêtre, sélectionner d'abord la fenêtre : le retardateur démarre ensuite. Il est possible de capturer Capture Studio lui-même et d'ouvrir son menu pendant le décompte.

L'OCR propose une sélection dans l'image ouverte ou sur l'écran, ainsi que l'extraction de toute l'image. Les traitements OCR utilisent le moteur Windows local. Le partage propose des tuiles Discord/WhatsApp qui copient le rendu puis ouvrent l'application ; sélectionner ensuite une conversation et coller avec Ctrl+V. Aucun accès aux contacts n'est intégré.

Impr. écran nécessite que Capture Studio reste actif près de l'horloge et que la touche ne soit pas déjà réservée à un autre outil. Son activation dans Windows reste un choix de l'utilisateur.

## Compiler et tester

Sous Windows, avec PowerShell, .NET Framework 4.8 et les métadonnées WinRT du système :

```powershell
.\build.ps1 -OutputDirectory .\dist
.\scripts\test.ps1
# Facultatif : test OCR réel si les composants et langues OCR sont installés
.\scripts\test.ps1 -IncludeOcr
# Installateur avec Inno Setup 6
.\scripts\package.ps1
```

Le workflow manuel **Build and release** compile, teste, construit et vérifie l'installateur, puis publie les fichiers de la version déclarée dans `VersionInfo.cs`. Augmenter cette version avant une nouvelle publication. Les archives ne contiennent pas les données personnelles, captures ou brouillons de développement.

## Logos et droits

Les logos Discord et WhatsApp proviennent de [Simple Icons](https://github.com/simple-icons/simple-icons). Leurs sources et la licence associée sont conservées dans `assets/`. Ils identifient les applications de destination ; Capture Studio n'est pas affilié à ces services.

La licence des sources de Capture Studio reste à définir par l'auteur. Leur publication ne constitue pas, à elle seule, l'octroi d'une licence de réutilisation.

Les projets enregistrés lors d'une mise à jour sont rouverts après le redémarrage automatique. Les tests couvrent cette reprise ainsi que l'installation, la réinstallation et la désinstallation en préservant les données utilisateur.

Le test facultatif -IncludeNetwork télécharge l'installateur de la dernière version publique et vérifie son SHA-256 sans l'exécuter. Il nécessite un accès réseau et une version déjà publiée.

À partir de la version 0.15.1, l’installateur inclut un processus de demande de fermeture. Il ne termine jamais une application de force, respecte l’annulation, attend la fin réelle du processus et vérifie à nouveau qu’aucune instance ne reste active avant de modifier les fichiers. Les échanges temporaires ne contiennent qu’un identifiant aléatoire et un statut de fermeture, jamais une image ou du texte OCR.
