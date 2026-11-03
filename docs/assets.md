# Modelos 3D e fotos de cabine: lista para aprovação

Consulta feita em 2026-11-02 pela API pública do Sketchfab (`downloadable=true`) e pela API do Wikimedia Commons. **Nada foi baixado.** Baixar modelos do Sketchfab exige conta/token, então o download fica para depois da aprovação.

Poligonos = `faceCount` informado pelo Sketchfab. "Cabine" indica se o modelo é anunciado com cabine; "não confirmado" significa que a descrição e as tags não mencionam cabine e é preciso abrir o visualizador antes de aprovar.

## Recomendação (um item por aeronave e por cabine)

| Item | Recomendado | Motivo |
|---|---|---|
| F-39E (modelo) | **SAAB Jas 39A Gripen (Detailed Cockpit)** (ngkoret) | Única opção CC BY com cabine detalhada. É a versão A, não a E, e tem 768.679 polígonos: precisa de decimação/LOD. |
| A-29 (modelo) | **EMB-314 / A-29 "Super Tucano"** (KOG_THORNS) | CC BY, 45.262 polígonos, livre para uso comercial com crédito. Cabine confirmada pelo usuário; baixado em 2026-11-02. |
| KC-390 (modelo) | **Embraer KC-390 "Millennium"** (KOG_THORNS) | CC BY, 107.912 polígonos. Cabine confirmada pelo usuário no visualizador; baixado em 2026-11-02. |
| Cabine F-39E (foto) | `GripenNG cockpit br.png` | CC BY 3.0, réplica da versão brasileira do Gripen NG, Ministério da Defesa. |
| Cabine A-29 | O próprio modelo do KOG_THORNS (tem cabine) | A foto `Super Cucano Flight Simulator.png` (domínio público, USAF, simulador) fica só como referência visual. |
| Cabine KC-390 | O próprio modelo do KOG_THORNS (tem cabine) | A foto `KC-390 cockpit.png` (CC BY-SA 4.0, 716x531 px) fica só como referência visual. |

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
| **EMB-314 / A-29 "Super Tucano"** (recomendado, ID 017824164b9e40749a67b87edbbcab92) | KOG_THORNS | CC BY 4.0 | 45.262 (vértices 25.963) | Sim (confirmada pelo usuário) | https://sketchfab.com/3d-models/emb-314-a-29-super-tucano-017824164b9e40749a67b87edbbcab92 |

A descrição do autor diz "FREE USE FOR ALL" e a licença no Sketchfab é CC BY 4.0: exige crédito. Publicado em 2024-05-25. Tags: fab, embraer, brazilian, supertucano.

### KC-390

| Modelo | Autor | Licença | Polígonos | Cabine | Link |
|---|---|---|---|---|---|
| **Embraer KC-390 "Millennium"** (recomendado, ID ed745ffba2fe45b796cbc833116e3adc) | KOG_THORNS | CC BY 4.0 | 107.912 (vértices 66.284) | Sim (confirmada pelo usuário) | https://sketchfab.com/3d-models/embraer-kc-390-millennium-ed745ffba2fe45b796cbc833116e3adc |

Publicado em 2025-10-02. A descrição diz que o autor ainda trabalha no modelo (pode mudar). Mesmo autor do A-29, o que ajuda na consistência de estilo.

### Excluídos (não usar)

| Modelo | Motivo |
|---|---|
| JAS39C Gripen (War Thunder) (`eb3e46db19bc43c1ae5c7dfb1d30386c`) | Extraído do War Thunder: excluído por regra do projeto. |
| JAS39E Gripen (War Thunder) (`642e47e513a0401a89222cee025caf93`) | Extraído do War Thunder, apesar de ser a versão E. Excluído. |
| Saab JAS 39 Gripen - Fighter Jet - Free (bohmerang, `03d4f4f0f7e94f469299ba7db4c707c4`) | CC BY-NC-SA: licença NonCommercial. |
| EMB-314 Super Tucano A29B (Gilberto, `ad14e4e33ac4482998784249bb11d7f8`) | Licença "Free Standard" do Sketchfab, não verificada. |

## Inimigos (caças dos EUA e da Argentina)

Consulta pela API do Sketchfab, só com CC BY. A licença é a declarada pelo autor: conferir no visualizador antes de usar. O Gripen de urgo fica para o ala, não para os inimigos.

| Força | Aeronave | Modelo | Autor | Polígonos | ID |
|---|---|---|---|---|---|
| EUA / Argentina | F-16 | Lockheed Martin F-16e/f "Fighting Falcon" (variante diferente da F-16C/AM; pintura a ajustar) | andertan | 35.478 | d54c72e055a647ad9173e01548e50db3 |
| EUA | F/A-18E | Boeing F/A-18E/F "Super Hornet" | andertan | 57.856 | f71e9fea01e24fea9b1b380161d21d38 |
| EUA | F-35A | Lockheed Martin F35 Lightning II | andertan | 83.174 | db911de503c74285a8d506951df786d6 |
| Argentina | A-4AR | A-4X Ultra Skyhawk (variante diferente da A-4AR) | pandoraskitten | 19.528 | 7b1cf9730fed43b3b4b94b37cb6677fa |
| Narcotráfico | Cessna 182 | CESSNA 182 (pesado para uma aeronave pequena) | engineerdesign | 152.488 | f31b9e74b6c34285af8a1f5ede6da95e |

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

## 3. Aeronaves inimigas (para aprovação)

Consulta de 2026-11-02 no Sketchfab (`downloadable=true`), só CC BY ou CC0, sem War Thunder e sem NonCommercial. Cabine não é necessária. Polígonos = `faceCount`. Todos são ficção de jogo: "forças dos EUA" e "forças da Argentina" são cenários hipotéticos.

### Recomendação (um item por papel)

| Papel | Missão | Modelo | Autor | Licença | Polígonos | Link |
|---|---|---|---|---|---|---|
| Caça dos EUA | m03 (escolta) | Boeing F/A-18E/F "Super Hornet" | andertan | CC BY | 57.856 | https://sketchfab.com/3d-models/f71e9fea01e24fea9b1b380161d21d38 |
| Reconhecimento dos EUA | m03 | Boeing P-8 Poseidon | Muhamad Mirza Arrafi | CC BY | 42.265 | https://sketchfab.com/3d-models/3fc134024b69403bb5df5ab3aff48919 |
| Caça da Argentina | m04 (1ª onda) | Dassault Mirage III | Spark_Customs | CC BY | 13.703 | https://sketchfab.com/3d-models/9e61e04aa3a14159a6fe2f70b40fb8f2 |
| Ataque da Argentina | m04 (2ª onda) | A-4X Ultra Skyhawk | pandoraskitten | CC BY | 19.528 | https://sketchfab.com/3d-models/7b1cf9730fed43b3b4b94b37cb6677fa |
| Narcotráfico (monomotor) | m02 | Cessna 172 ("Plane") | osmosikum | CC BY | 14.892 | https://sketchfab.com/3d-models/cedc8a07370747f7b0d14400cdf2faf9 |
| Narcotráfico (bimotor) | m02 | Cessna 421 Golden Eagle | helijah | CC BY | 34.601 | https://sketchfab.com/3d-models/3d933b6fd5b447dfbc65d05228bbe902 |

Por que esses:
- O F/A-18E/F, o F-16, o F-35 e o F-22 do **andertan** formam um conjunto com o mesmo estilo (35 mil, 83 mil e 11 mil polígonos), o que ajuda na consistência visual. Alternativas dos EUA do mesmo autor: F-16E/F (`d54c72e055a647ad9173e01548e50db3`, 35.478), F-35 (`db911de503c74285a8d506951df786d6`, 83.174), F-22 (`7e7b3c372e374dda98e70208d09cfdcf`, 10.955).
- Para a Argentina não há modelo bom do A-4AR Fightinghawk nem do Dagger. O A-4X e o Mirage III são os mais próximos das aeronaves que o país já operou, mas **não são as versões argentinas**: servem como ficção. O IA-63 Pampa (Lolmom1313, `99bbe3cce23248cf88fb5be6c4808c59`, 1.832 polígonos) existe, mas é um primeiro modelo do autor, de baixa qualidade.
- Não achei bom modelo de Cessna 208 Caravan nem Piper Navajo. O 172 e o 421 são os melhores livres encontrados; o 172 tem animação.

### Excluídos ou não recomendados

| Modelo | Motivo |
|---|---|
| Qualquer modelo de **manilov.ap** (F16, F18, Su27, Cessna185, E3d...) | Origem não verificada: nomes como "Il2", "A37" e "Mig29" sugerem extração de jogos antigos. Sem prova de autoria própria, não usar. |
| Argentine FMA IA 58 Pucara (42manako, `7d8cbbb5cf8f42d0811e0a406e6098c3`) | A descrição diz "Not my model": é re-upload de obra de terceiros. |
| FMA IA 58A Pucará e Mirage IIIE (mgg8, CC0) | Licença livre, mas gerados proceduralmente e pesados (161 mil e 171 mil polígonos). Reservas se precisar da versão argentina do Pucará. |
| F-35A (shangus930, 190.514) e A-4 Skyhawk (johnnokomis, 592.163) | Pesados demais para inimigo de jogo. |
| Learjet 35A (mudkipz321, 2,7 milhões de polígonos) | Pesado demais. |

## Próximos passos

1. Você aprova (ou troca) um item por aeronave e por cabine.
2. Eu baixo apenas os aprovados, cada um em pasta própria, e atualizo `docs/creditos.md` com autor, licença e link.
3. Cabines do A-29 e do KC-390 confirmadas pelo usuário no visualizador do Sketchfab.
