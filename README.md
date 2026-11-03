# FSDS — FABSimulator

Simulador de voo militar em português, desenvolvido na Unity, com aeronaves da FAB, terreno real via Cesium e combate fictício.

## Tecnologia

- **Engine:** Unity 6000.3.27f2, HDRP 17.3.
- **Pacotes principais:** Cesium for Unity 1.26.1, glTFast e Input System.
- **Núcleo de voo** (`FABSimulator/Assets/Scripts/Core`): `FlightModel.cs`, `AircraftDb.cs` e `Combat.cs`, sem dependência da Unity (assembly `FABSim.Core`).
- **Jogo** (`FABSimulator/Assets/Scripts/Game`): controlador de voo, entrada, HUD e dados de bases.
- **Dados** (`FABSimulator/Assets/StreamingAssets/Data`): bases e pistas, armamento, rádio e missões em JSON.

## Como abrir

1. Abra a pasta `FABSimulator` no Unity Hub com a versão 6000.3.27f2.
2. Crie `FABSimulator/cesium-ion-token.txt` com o seu token do Cesium ion. O arquivo é ignorado pelo Git.
3. Abra a cena em `Assets/Scenes`.

Os modelos 3D e as fotos de cabine (`Assets/Art`, cerca de 315 MB) não ficam no repositório, por tamanho e licenças. A origem de cada um está em [`docs/assets.md`](docs/assets.md).

Os dados de missões, forças e inimigos são **ficção de jogo**, sem detalhes operacionais reais.

## Modelos 3D

Os arquivos não ficam no repositório. Para rodar o jogo a partir de um clone, baixe cada modelo no link e salve no caminho indicado; depois execute `FabBootstrap.Build` para refazer a biblioteca de modelos. Os nomes de arquivo precisam ser exatamente estes.

| Uso no jogo | Caminho em `Assets/Art/Models/` | Autor | Licença | Link |
|---|---|---|---|---|
| F-39E (jogador) | `F39E/saab_jas_39a_gripen_detailed_cockpit.glb` | ngkoret | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/saab-jas-39a-gripen-detailed-cockpit-f4646ccf489341c28c4118de38feface) |
| Ala do F-39E e trem de pouso do jogador | `F39E_ALA/saab_jas_39_gripen_bohmerang.glb` | bohmerang | CC BY-NC-SA 4.0: só uso pessoal, não comercial | [Sketchfab](https://sketchfab.com/3d-models/03d4f4f0f7e94f469299ba7db4c707c4) |
| Ala do F-39E (reserva livre) | `F39E_ALA/saab_jas_39_gripen.glb` | urgo | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/3c90b2acdf204599b45bf91a09740be9) |
| A-29 | `A29/source/A-29/A29SUPERTUCANO.fbx` | KOG_THORNS | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/emb-314-a-29-super-tucano-017824164b9e40749a67b87edbbcab92) |
| KC-390 | `KC390/source/Embraer KC-390/KC-390.fbx` | KOG_THORNS | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/embraer-kc-390-millennium-ed745ffba2fe45b796cbc833116e3adc) |
| F/A-18E | `EUA_FA18/boeing_fa-18ef_super_hornet.glb` | andertan | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/f71e9fea01e24fea9b1b380161d21d38) |
| F-22A | `EUA_F22/lockheed_martin_f-22_raptor.glb` | andertan | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/7e7b3c372e374dda98e70208d09cfdcf) |
| P-8 | `EUA_P8/boeing_p-8_poseidon.glb` | Muhamad Mirza Arrafi | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/3fc134024b69403bb5df5ab3aff48919) |
| MH-60M (substituto: SH-60B) | `EUA_SH60B/sh-60b_seahawk_helicopter.glb` | Muhamad Mirza Arrafi | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/558373489bc145e292342efbe4b982ed) |
| MH-47G (substituto: CH-47) | `EUA_CH47/boeing_ch-47_chinook_military_transport_aircraft.glb` | Muhamad Mirza Arrafi | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/cdac73e931f6482e86960a326fef73bf) |
| Mirage III | `ARG_Mirage3/dassault_mirage_iii.glb` | Spark_Customs | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/9e61e04aa3a14159a6fe2f70b40fb8f2) |
| A-4X | `ARG_A4X/a-4x_ultra_skyhawk.glb` | pandoraskitten | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/7b1cf9730fed43b3b4b94b37cb6677fa) |
| Cessna 172 | `NARCO_Cessna172/plane.glb` | osmosikum | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/cedc8a07370747f7b0d14400cdf2faf9) |
| Cessna 421 | `NARCO_Cessna421/cessna_421_golden_eagle.glb` | helijah | CC BY 4.0 | [Sketchfab](https://sketchfab.com/3d-models/3d933b6fd5b447dfbc65d05228bbe902) |
| Fragata Tamandaré | `NAVIO_Tamandare/tamandare_frigate_sem_heli.glb` | ammar_ssr (re-upload) | CC BY 4.0, autoria incerta: só uso pessoal | [Sketchfab](https://sketchfab.com/3d-models/8443b68bc2a847b0a5fcd039ba24ef7f) |

O A-29 e o KC-390 são FBX com as texturas ao lado do arquivo. A fragata usada no jogo teve o helicóptero removido da versão original.

## Testes de paridade

Garantem que o núcleo C# produza as mesmas trajetórias que a implementação de referência em JavaScript (`physics.js` e `aircraft.js`, na raiz), que veio da versão web do projeto, já removida.

```bash
node tests/parity/trajectory.js > js.csv
```

```bash
dotnet run --project tests/parity > cs.csv
```

```bash
python tests/parity/compare.py js.csv cs.csv
```

Para rodar apenas as verificações internas do núcleo C#, use `dotnet run --project tests/parity -- checks`.

## Documentação

- [`docs/assets.md`](docs/assets.md): modelos 3D e fotos de cabine, com licenças.
- [`docs/modelos.md`](docs/modelos.md): notas sobre os modelos.
- [`docs/creditos.md`](docs/creditos.md): fontes de dados (OpenStreetMap, OurAirports) e atribuições.

## Aviso

Projeto de entretenimento e estudo. Não use para navegação ou treinamento aeronáutico real.
