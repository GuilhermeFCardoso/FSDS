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

Os modelos 3D e as fotos de cabine (`Assets/Art`, cerca de 290 MB) não ficam no repositório, por tamanho e licenças. A origem de cada um está em [`docs/assets.md`](docs/assets.md).

Os dados de missões, forças e inimigos são **ficção de jogo**, sem detalhes operacionais reais.

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
