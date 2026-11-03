# FSDS — Simulador de Voo

Simulador de voo em português, com voo livre e combate fictício, em duas frentes:

- **Versão web** (raiz do repositório): JavaScript puro rodando no navegador, com globo 3D (CesiumJS), mapa de navegação (Leaflet), física de voo própria, instrumentos, cockpit, som e HUD.
- **FABSimulator** (Unity 6, HDRP): port em andamento para Unity, com o mesmo modelo de voo em C# e terreno via Cesium for Unity.

## Versão web

### Como rodar

Requer Python 3 e internet (CesiumJS, Leaflet, tiles, clima e elevação vêm de serviços externos).

```bash
python -m http.server 8765
```

Abra http://localhost:8765/index.html. No Windows, o `iniciar.bat` faz os dois passos.

### Recursos

- **Aeronaves:** Cessna 172, Boeing 737-800, Airbus A320, Saab Gripen, F-16C e Embraer EMB-314 Super Tucano, cada uma com parâmetros aerodinâmicos, motor e trem de pouso próprios (`aircraft.js`).
- **Voo:** física de 6 graus de liberdade (`physics.js`), fly-by-wire nos caças, trim, flaps, speedbrake, vento e turbulência, e clima real via Open-Meteo.
- **Modos:** voo livre, patrulha de combate, ataque ao solo e arena de sobrevivência, em três dificuldades (`combat.js`).
- **Locais:** pistas pré-definidas ou coordenadas personalizadas, com elevação real do terreno.
- **Cockpit e navegação:** instrumentos analógicos, HUD, mapa de navegação (`N`) e minimapa (`J`).
- **Controles:** teclado ou controle Xbox. A tabela completa fica no menu do jogo.

### Controles principais (teclado)

| Tecla | Ação |
|---|---|
| `↑` `↓` / `←` `→` | Profundor / aileron |
| `A` `D` | Leme |
| `W` `S` | Motor |
| `Q` `E` | Trim |
| `G` / `P` | Trem de pouso / freio de estacionamento |
| `Z` `X` | Flaps |
| `B` | Speedbrake |
| `V` | Câmera externa / cockpit |
| `Espaço` | Canhão (no ar) ou freio (no solo) |
| `F` ou `Enter` | Dispara a arma selecionada |
| `Tab`, `1`–`4` | Troca de arma |
| `T` / `L` | Troca de alvo / flares |
| `R` | Reiniciar |
| `Esc` | Menu |

### Estrutura

| Arquivo | Função |
|---|---|
| `index.html`, `game.js` | Interface, menu, loop principal e entrada |
| `physics.js`, `aircraft.js` | Modelo de voo e dados das aeronaves |
| `combat.js`, `hud.js`, `fx.js` | Combate, HUD e efeitos |
| `cockpit.js`, `instruments.js`, `models.js` | Cockpit, instrumentos e modelos 3D procedurais |
| `world.js`, `runway.js`, `nav.js`, `weather.js` | Terreno, pistas, navegação e clima |
| `sound.js` | Áudio sintetizado |

### Tokens opcionais

O menu aceita um token do Cesium ion e uma chave do Google Maps (tiles 3D fotorrealistas). Eles ficam só no `localStorage` do navegador e não vão para o repositório.

## FABSimulator (Unity)

- **Engine:** Unity 6000.3.27f2, HDRP 17.3.
- **Pacotes principais:** Cesium for Unity 1.26.1, glTFast e Input System.
- **Núcleo de voo** (`Assets/Scripts/Core`): `FlightModel.cs`, `AircraftDb.cs` e `Combat.cs`, sem dependência da Unity (assembly `FABSim.Core`).
- **Jogo** (`Assets/Scripts/Game`): controlador de voo, entrada e dados de bases.
- **Dados** (`Assets/StreamingAssets/Data`): bases e pistas, armamento, rádio e missões em JSON.
- **Token do Cesium ion:** crie `FABSimulator/cesium-ion-token.txt` com o seu token. O arquivo é ignorado pelo Git.

Os dados de missões, forças e inimigos são **ficção de jogo**, sem detalhes operacionais reais.

## Testes de paridade

Garantem que o núcleo C# produza as mesmas trajetórias que o `physics.js`.

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
- [`docs/creditos.md`](docs/creditos.md): fontes de dados (OpenStreetMap, OurAirports) e atribuições.

## Aviso

Projeto de entretenimento e estudo. Não use para navegação ou treinamento aeronáutico real.
