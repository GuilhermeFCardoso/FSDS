# Modelos 3D e fotos de cabine: lista para aprovação

Consulta feita em 2026-11-02 pela API pública do Sketchfab (`downloadable=true`) e pela API do Wikimedia Commons. **Nada foi baixado.** Baixar modelos do Sketchfab exige conta/token, então o download fica para depois da aprovação.

Poligonos = `faceCount` informado pelo Sketchfab. "Cabine" indica se o modelo é anunciado com cabine; "não confirmado" significa que a descrição e as tags não mencionam cabine e é preciso abrir o visualizador antes de aprovar.

## Recomendação (um item por aeronave e por cabine)

| Item | Recomendado | Motivo |
|---|---|---|
| F-39E (modelo) | **SAAB Jas 39A Gripen (Detailed Cockpit)** (ngkoret) | Única opção CC BY com cabine detalhada. É a versão A, não a E, e tem 768.679 polígonos: precisa de decimação/LOD. |
| A-29 (modelo) | **EMB-314 / A-29 "Super Tucano"** (KOG_THORNS) | CC BY, 45.262 polígonos, livre para uso comercial com crédito. Cabine não confirmada. |
| KC-390 (modelo) | **Embraer KC-390 "Millennium"** (KOG_THORNS) | CC BY, 107.912 polígonos. Cabine não confirmada. |
| Cabine F-39E (foto) | `GripenNG cockpit br.png` | CC BY 3.0, réplica da versão brasileira do Gripen NG, Ministério da Defesa. |
| Cabine A-29 (foto) | `Super Cucano Flight Simulator.png` | Domínio público (USAF), mas é um simulador de A-29 e não a cabine real. É o melhor item livre encontrado. |
| Cabine KC-390 (foto) | `KC-390 cockpit.png` | CC BY-SA 4.0, mas só 716x531 px: serve como referência, não como textura. |

## 1. Modelos 3D (Sketchfab)

### F-39E (Gripen E/F)

Não existe modelo CC BY ou CC0 do Gripen E com cabine. As opções utilizáveis são:

| Modelo | Autor | Licença | Polígonos | Cabine | Link |
|---|---|---|---|---|---|
| **SAAB Jas 39A Gripen (Detailed Cockpit)** (recomendado) | ngkoret | CC BY 4.0 | 768.679 | Sim (detalhada) | https://sketchfab.com/3d-models/saab-jas-39a-gripen-detailed-cockpit-f4646ccf489341c28c4118de38feface |
| Gripen NG | TuszPro | CC BY-SA | 5.652 | Não confirmado | https://sketchfab.com/3d-models/none-be94bc8bdab14385b67442df3eaa34ae |
| Saab Jas 39 Gripen | urgo | CC BY | 25.023 | Não confirmado | https://sketchfab.com/3d-models/none-3c90b2acdf204599b45bf91a09740be9 |

Observações:
- O ngkoret é JAS 39A (publicado em 2025-07-11) e muito pesado; o jogo precisará de uma versão decimada para o exterior e a cabine separada.
- O TuszPro retrata a versão NG (mais próxima do F-39E), mas **CC BY-SA** exige que obras derivadas usem a mesma licença; só aprovar se isso for aceitável para os assets do jogo. Tem 5.652 polígonos, então precisaria de trabalho para ganhar detalhe.
- Alternativa pronta para jogo, sem cabine: urgo (25 mil polígonos).

### A-29

| Modelo | Autor | Licença | Polígonos | Cabine | Link |
|---|---|---|---|---|---|
| **EMB-314 / A-29 "Super Tucano"** (recomendado, ID 017824164b9e40749a67b87edbbcab92) | KOG_THORNS | CC BY 4.0 | 45.262 (vértices 25.963) | Não confirmado | https://sketchfab.com/3d-models/emb-314-a-29-super-tucano-017824164b9e40749a67b87edbbcab92 |

A descrição do autor diz "FREE USE FOR ALL" e a licença no Sketchfab é CC BY 4.0: exige crédito. Publicado em 2024-05-25. Tags: fab, embraer, brazilian, supertucano.

### KC-390

| Modelo | Autor | Licença | Polígonos | Cabine | Link |
|---|---|---|---|---|---|
| **Embraer KC-390 "Millennium"** (recomendado, ID ed745ffba2fe45b796cbc833116e3adc) | KOG_THORNS | CC BY 4.0 | 107.912 (vértices 66.284) | Não confirmado | https://sketchfab.com/3d-models/embraer-kc-390-millennium-ed745ffba2fe45b796cbc833116e3adc |

Publicado em 2025-10-02. A descrição diz que o autor ainda trabalha no modelo (pode mudar). Mesmo autor do A-29, o que ajuda na consistência de estilo.

### Excluídos (não usar)

| Modelo | Motivo |
|---|---|
| JAS39C Gripen (War Thunder) (`eb3e46db19bc43c1ae5c7dfb1d30386c`) | Extraído do War Thunder: excluído por regra do projeto. |
| JAS39E Gripen (War Thunder) (`642e47e513a0401a89222cee025caf93`) | Extraído do War Thunder, apesar de ser a versão E. Excluído. |
| Saab JAS 39 Gripen - Fighter Jet - Free (bohmerang, `03d4f4f0f7e94f469299ba7db4c707c4`) | CC BY-NC-SA: licença NonCommercial. |
| EMB-314 Super Tucano A29B (Gilberto, `ad14e4e33ac4482998784249bb11d7f8`) | Licença "Free Standard" do Sketchfab, não verificada. |

## 2. Fotos de cabine (Wikimedia Commons)

Todas as licenças foram lidas da API de metadados do Commons. Atribuição exige o nome do autor e a licença.

| Aeronave | Arquivo | Autor | Licença | Resolução | Observação |
|---|---|---|---|---|---|
| F-39E | [GripenNG cockpit br.png](https://commons.wikimedia.org/wiki/File:GripenNG_cockpit_br.png) (recomendada) | Brazilian Ministry of Defense | CC BY 3.0 | 1024x576 | "Replica of Brazilian version of Gripen NG cockpit" |
| F-39E | [Saab Gripen NG cabina.png](https://commons.wikimedia.org/wiki/File:Saab_Gripen_NG_cabina.png) | G4ng3r | CC BY-SA 4.0 | 1764x1217 | Cabine com telas digitais; BY-SA exige compartilhar igual |
| F-39E | [JAS-39 Gripen Cockpit Simulator.jpg](https://commons.wikimedia.org/wiki/File:JAS-39_Gripen_Cockpit_Simulator.jpg) | rhk111 | CC BY-SA 4.0 | 4000x2896 | Simulador do JAS 39 em feira |
| A-29 | [Super Cucano Flight Simulator.png](https://commons.wikimedia.org/wiki/File:Super_Cucano_Flight_Simulator.png) (recomendada) | USAF / Master Sgt. Jeffrey Allen | Domínio público | 1365x697 | Simulador de A-29 em Moody AFB; não é a cabine real |
| KC-390 | [KC-390 cockpit.png](https://commons.wikimedia.org/wiki/File:KC-390_cockpit.png) (recomendada) | Fernando Xavier | CC BY-SA 4.0 | 716x531 | Cabine de vidro com comandos fly-by-wire; baixa resolução |

Não foi encontrada no Commons nenhuma foto livre da cabine real do A-29 nem de outra cabine do KC-390 além da acima.

### Não incluídas

- **Fotos do site da FAB (gov.br/fab):** "Todos os Direitos Reservados". Não usar.
- Arquivos de cabine do MiG-29 e de outras aeronaves que a busca retornou: irrelevantes.

## Próximos passos

1. Você aprova (ou troca) um item por aeronave e por cabine.
2. Eu baixo apenas os aprovados, cada um em pasta própria, e atualizo `docs/creditos.md` com autor, licença e link.
3. Verificação de cabine do A-29 e do KC-390 no visualizador do Sketchfab, antes do download.
