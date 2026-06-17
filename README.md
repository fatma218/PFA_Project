# PFA_Project

Simulation VR de formation médicale

## Description

Ce projet Unity présente une simulation de formation médicale en réalité virtuelle. Il inclut une scène dédiée à la salle de lavage modélisée en 3D, ainsi qu'une séquence d'étapes de lavage des mains.

## Technologies utilisées

- Unity 2022
- Hand tracking pour le déplacement et l’interaction en VR
- Guidage audio à chaque étape du lavage
- Modélisation 3D de la salle de lavage réalisée avec Blender
- Téléportation pour commencer le lavage

### Etape de LavageMain

<p align="center">
  <img src="capture_etape_lavage_scene/SalledeLavagedansUnity.png" alt="Salle de lavage 3D modélisée en Blender" width="640" />
  <br/>
  <strong>Salle de lavage 3D modélisée en Blender</strong>
</p>

<table cellpadding="10" cellspacing="10" style="margin: 0 auto;">
  <tr>
    <td align="center" style="padding: 10px;"><img src="capture_etape_lavage_scene/premieretape telepoter pour commencer le lavage.png" alt="Téléportation pour commencer" width="280" style="display:block; margin:0 auto;" /><br/><strong>Téléportation</strong><br/>Déplacement initial en VR via hand tracking.</td>
    <td align="center" style="padding: 10px;"><img src="capture_etape_lavage_scene/scene_01_robinet.png" alt="Ouverture du robinet" width="280" style="display:block; margin:0 auto;" /><br/><strong>Ouverture du robinet</strong></td>
    <td align="center" style="padding: 10px;"><img src="capture_etape_lavage_scene/scene_03_halo_savon.png" alt="Halo de savon" width="280" style="display:block; margin:0 auto;" /><br/><strong>Halo de savon</strong></td>
    <td align="center" style="padding: 10px;"><img src="capture_etape_lavage_scene/scene_02_soapdispenser_detection.png" alt="Distributeur de savon" width="280" style="display:block; margin:0 auto;" /><br/><strong>Distributeur de savon</strong></td>
  </tr>
  <tr>
    <td align="center" colspan="2" style="padding: 10px;"><img src="capture_etape_lavage_scene/scene_04_frottage_minuteur.png" alt="Frottage des mains" width="280" style="display:block; margin:0 auto;" /><br/><strong>Frottage des mains</strong><br/>Frottage pendant 90 secondes en suivant le protocole médical.</td>
    <td align="center" colspan="2" style="padding: 10px;"><img src="capture_etape_lavage_scene/scene_05_rincage.png" alt="Rinçage" width="280" style="display:block; margin:0 auto;" /><br/><strong>Rinçage</strong><br/>Diminution progressive du savon, mousse rose pendant le rinçage.</td>
  </tr>
  <tr>
    <td align="center" style="padding: 10px;"><img src="capture_etape_lavage_scene/scene_06_sechage_tissu.png" alt="Séchage des mains" width="280" style="display:block; margin:0 auto;" /><br/><strong>Séchage</strong></td>
    <td align="center" style="padding: 10px;"><img src="capture_etape_lavage_scene/Trushzone.png" alt="Zone de poubelle" width="280" style="display:block; margin:0 auto;" /><br/><strong>Zone de poubelle</strong></td>
    <td align="center" colspan="2" style="padding: 10px;"><img src="capture_etape_lavage_scene/succeedmenu.png" alt="Réussite" width="280" style="display:block; margin:0 auto;" /><br/><strong>Réussite</strong></td>
  </tr>
</table>

### En cas de contamination

<p align="center">
  <img src="capture_etape_lavage_scene/scene_failmenu_contamination.png" alt="Échec contamination" width="640" />
  <br/>
  <strong>Échec contamination</strong>
</p>

## Utilisation

1. Téléchargez/clônez le projet.
2. Ouvrez le dossier du projet dans Unity Editor 2022.
3. Chargez la scène appropriée pour visualiser la simulation VR.
4. Test possible avec le Meta Simulator
