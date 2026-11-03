# Modelos 3D por tipo de aeronave

Liga o campo `tipo` de `missoes.json` (e as aeronaves do jogador) ao modelo em `FABSimulator/Assets/Art/Models/`. Autoria e licença completas em `docs/creditos.md`. Todos são GLB, exceto A-29 e KC-390 (FBX).

**Substituto = sim** significa que o modelo é o mais próximo disponível com licença livre, não a versão exata operada pelo país.

## Jogador e ala

| Aeronave | Pasta | Arquivo | Polígonos | Cabine | Substituto | Estado |
|---|---|---|---|---|---|---|
| F-39E (jogador) | `F39E/` | `saab_jas_39a_gripen_detailed_cockpit.glb` (sem trem de pouso: o jogo usa o trem do modelo do bohmerang, **CC BY-NC-SA**) | 768.679 | Sim | Sim (é um JAS 39A) | Baixado |
| F-39E (ala, leve) | `F39E_ALA/` | `saab_jas_39_gripen_bohmerang.glb` (bohmerang, **CC BY-NC-SA: só uso pessoal**) | 29.535 | Visível no modelo | Sim (JAS 39, marcas suecas) | Baixado, texturas 1k; em uso |
| F-39E (ala, reserva livre) | `F39E_ALA/` | `saab_jas_39_gripen.glb` (urgo, CC BY) | 25.023 | Não | Sim (JAS 39) | Baixado; troque por ele se o jogo for distribuído |
| A-29 | `A29/` | `source/A-29/A29SUPERTUCANO.fbx` | 45.262 | Sim | Não | Baixado |
| KC-390 | `KC390/` | `source/Embraer KC-390/KC-390.fbx` | 107.912 | Sim | Não | Baixado |

## Inimigos (valores exatos do campo `tipo` em `missoes.json`)

| `tipo` | `forca` | Missão | Pasta | Polígonos | Substituto | Estado |
|---|---|---|---|---|---|---|
| `P-8` | EUA | m03 | `EUA_P8/` | 42.265 | Não | Baixado |
| `F/A-18E` | EUA | m03 | `EUA_FA18/` | 57.856 | Não (modelo é E/F) | Baixado |
| `F-22A` | EUA | m03 (`dificuldade_minima`: veterano) | `EUA_F22/` | 10.955 | Não | Baixado (`lockheed_martin_f-22_raptor.glb`) |
| `MH-60M` (missão `nao-vai-ter-golpe`, definida em `GameData.cs`) | EUA | nao-vai-ter-golpe | `EUA_SH60B/` | 63.968 | **Sim**: o modelo é um SH-60B (Marinha); o MH-60M é do Exército | Baixado (`sh-60b_seahawk_helicopter.glb`) |
| `MH-47G` (missão `nao-vai-ter-golpe`, definida em `GameData.cs`) | EUA | nao-vai-ter-golpe | `EUA_CH47/` | 15.386 | **Sim**: o modelo é um CH-47 genérico, não a variante MH-47G de operações especiais | Baixado (`boeing_ch-47_chinook_military_transport_aircraft.glb`) |
| `Mirage III` | Argentina | m04 | `ARG_Mirage3/` | 13.703 | **Sim**: a Argentina opera F-16 e A-4AR; não há Dagger/Mirage III argentino livre | Baixado |
| `A-4X` | Argentina | m04 | `ARG_A4X/` | 19.528 | **Sim**: o país opera o A-4AR Fightinghawk; o A-4X Ultra Skyhawk é o mais próximo | Baixado |
| `Cessna 172` | Narcotráfico | m02 | `NARCO_Cessna172/` | 14.892 | Não | Baixado |
| `Cessna 421` | Narcotráfico | m02 | `NARCO_Cessna421/` | 34.601 | Não | Baixado |

## Outros

| Item | Pasta | Observação |
|---|---|---|
| Fragata Tamandaré | `NAVIO_Tamandare/` | `tamandare_frigate_sem_heli.glb`, 966.677 polígonos; re-upload sem autoria original, só uso pessoal. |
