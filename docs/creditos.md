# Créditos e fontes

Dados de aeródromos e pistas usados em `FABSimulator/Assets/StreamingAssets/Data/bases.json`. Uso apenas em simulação, nunca para navegação real.

## Atribuição obrigatória

- **OpenStreetMap:** © colaboradores do OpenStreetMap, dados sob a licença Open Database License (ODbL) 1.0. https://www.openstreetmap.org/copyright
  Consulta feita pela API Overpass (`aeroway=runway`) em 2026-11-02. Os campos `cabeceira_a`, `cabeceira_b`, `rumo_verdadeiro` e `comprimento_osm_m` derivam da geometria do OSM.
- **OurAirports:** nomes, coordenadas do aeródromo, elevação, comprimento e largura publicados das pistas. Dados dedicados ao domínio público. https://davidmegginson.github.io/ourairports-data/

> Atenção jurídica (revisar antes de distribuir): o ODbL exige que uma base de dados derivada seja disponibilizada sob a mesma licença. `bases.json` mistura dados do OSM e do OurAirports e deve ser tratado como base derivada se for distribuído publicamente. Isto não é parecer jurídico.

## Método

1. Aeródromos e pistas publicadas: OurAirports (`airports.csv`, `runways.csv`), ignorando pistas fechadas e helipontos.
2. Geometria: ways OSM com `aeroway=runway` num raio de 5 km de cada aeródromo, casadas pela designação (ex.: 06L/24R). Ways fragmentadas foram unidas por proximidade de extremos (< 40 m); as cabeceiras são os dois pontos mais distantes da cadeia.
3. Cabeceira A/B: atribuída pela proximidade da proa a partir de cada ponta com o número da cabeceira (declinação magnética aceita na comparação). `rumo_verdadeiro` é a proa geodésica de A para B.
4. Origem de cada pista: o campo `fonte` de cada item de `bases.json`. As cabeceiras do OurAirports não foram usadas, pois faltam em várias bases militares.

## Validação de comprimento (diferença acima de 100 m)

Comprimento calculado do OSM (haversine entre as cabeceiras) contra o publicado no OurAirports. Estas pistas têm `geometria_divergente: true` em `bases.json`; as cabeceiras delas são aproximadas até conferência manual.

| ICAO | Pista | Publicado (m) | OSM (m) | Diferença (m) |
|---|---|---|---|---|
| SBAN | 06L/24R | 3300 | 4008 | +708 |
| SBCO | 13/31 | 2751 | 3437 | +686 |
| SBMN | 9/27 | 2170 | 2310 | +140 |
| SBSM | 02/20 | 1414 | 1803 | +389 |
| SBFZ | 13/31 | 2755 | 2871 | +116 |

Outra divergência: em SBEG 11/29, o rumo verdadeiro do OSM (90,1°) difere em mais de 2° do `le_heading_degT` do OurAirports (106°). O OSM coincide com as próprias coordenadas das cabeceiras do OurAirports, então o campo de rumo do OurAirports parece inconsistente. Foi mantido o OSM.

## Todas as pistas

| ICAO | Pista | Publicado (m) | OSM (m) | Diferença (m) | Ways OSM |
|---|---|---|---|---|---|
| SBAN | 06L/24R | 3300 | 4008 | +708 | 30787911 |
| SBAN | 06R/24L | 2233 | 2253 | +20 | 288077976 |
| SBBE | 03/21 | 1830 | 1838 | +8 | 19997695 |
| SBBE | 07/25 | 2800 | 2861 | +61 | 19997693 |
| SBBV | 8/26 | 2700 | 2788 | +88 | 34799124,785968251 |
| SBBR | 11L/29R | 3200 | 3196 | -4 | 12302442,1065696938 |
| SBBR | 11R/29L | 3300 | 3292 | -8 | 48958839,1065696937 |
| SBCG | 06/24 | 2600 | 2599 | -1 | 800983696 |
| SBCO | 13/31 | 2751 | 3437 | +686 | 13487276 |
| SBMN | 9/27 | 2170 | 2310 | +140 | 23955290 |
| SBNT | 12/30 | 1825 | 1816 | -9 | 19998381 |
| SBNT | 16L/34R | 2600 | 2611 | +11 | 19998382 |
| SBNT | 16R/34L | 1800 | 1805 | +5 | 183971015 |
| SBPV | 1/19 | 2400 | 2462 | +62 | 55356808 |
| SBSC | 5/23 | 2739 | 2752 | +13 | 40089639 |
| SBSM | 02/20 | 1414 | 1803 | +389 | 36912673 |
| SBSM | 11/29 | 2694 | 2685 | -9 | 110663413 |
| SBGL | 10/28 | 4000 | 3996 | -4 | 12291112 |
| SBGL | 15/33 | 3180 | 3183 | +3 | 12291116,1356281630,1356281631 |
| SBGR | 10L/28R | 3700 | 3694 | -6 | 5367900,510285454,1388686363,1388686360,510285450 |
| SBGR | 10R/28L | 3000 | 2996 | -4 | 5367899,1388686342 |
| SBKP | 15/33 | 3240 | 3241 | +1 | 10492703,1240952238 |
| SBEG | 11/29 | 2700 | 2691 | -9 | 281372114 |
| SBRF | 18/36 | 3007 | 2946 | -61 | 22732877 |
| SBSV | 10/28 | 3005 | 3004 | -1 | 12331758,1062856298,1062856300 |
| SBSV | 17/35 | 1520 | 1532 | +12 | 12331757 |
| SBPA | 11/29 | 3200 | 3248 | +48 | 13487289 |
| SBFZ | 13/31 | 2755 | 2871 | +116 | 24330814 |
| SBCF | 16/34 | 3600 | 3631 | +31 | 5121266 |

## Modelos 3D e fotos

Itens aprovados em 2026-11-02 (ainda não baixados). Atribuição exigida em créditos do jogo.

| Uso | Obra | Autor | Licença | Link |
|---|---|---|---|---|
| Modelo F-39E | SAAB Jas 39A Gripen (Detailed Cockpit) | ngkoret | CC BY 4.0 | https://sketchfab.com/3d-models/saab-jas-39a-gripen-detailed-cockpit-f4646ccf489341c28c4118de38feface |
| Modelo A-29 | EMB-314 / A-29 "Super Tucano" | KOG_THORNS | CC BY 4.0 | https://sketchfab.com/3d-models/emb-314-a-29-super-tucano-017824164b9e40749a67b87edbbcab92 |
| Modelo KC-390 | Embraer KC-390 "Millennium" | KOG_THORNS | CC BY 4.0 | https://sketchfab.com/3d-models/embraer-kc-390-millennium-ed745ffba2fe45b796cbc833116e3adc |
| Foto cabine F-39E | GripenNG cockpit br.png | Brazilian Ministry of Defense | CC BY 3.0 | https://commons.wikimedia.org/wiki/File:GripenNG_cockpit_br.png |
| Foto cabine A-29 | Super Cucano Flight Simulator.png | USAF / Master Sgt. Jeffrey Allen | Domínio público | https://commons.wikimedia.org/wiki/File:Super_Cucano_Flight_Simulator.png |
| Foto cabine KC-390 | KC-390 cockpit.png | Fernando Xavier | CC BY-SA 4.0 (obras derivadas também em BY-SA) | https://commons.wikimedia.org/wiki/File:KC-390_cockpit.png |
