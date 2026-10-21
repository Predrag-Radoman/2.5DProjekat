# REPLACED feel — analiza kretanja i kako je oponasano

**REPLACED** (Sad Cat Studios) — 2.5D akcioni platformer (alternativne 1980-te):
fluidan, "težak ali responsivan" movement sa momentum-om i akrobatskim detaljima.

## Sta ga cini prepoznatljivim (iz design analiza/preview-a)

1. **Momentum run** — ubrzanje sa ease-out krivom blizu top speed-a (ne linearno);
   na pustanje/smenu smera **skid/decel** (ne instant stop) → tezina + responsivnost.
2. **Težak ali brz jump** — variable height + coyote + buffer; **fast-fall**:
   jaca gravitacija u padu nego u usponu (brzi, tezi doskok; uspon blago floaty).
3. **Turn pivot sa skid-om** — pri smeni smera kratko usporavanje pre pivota.
4. **Dodge step / dash** — kratki burst-impuls sa cooldown-om (Shift / gamepad West).
5. **Sporiji depth (Z) movement** — koridor feel: dubina ~0.55 brzine bocnog kretanja.
6. **Smanjen air control** — manji air authority + fast-fall kombinacija.

## Kako je oponasano u PlayerController-u (header "REPLACED feel")

| Parametar | Vrednost | REPLACED osobina |
|---|---|---|
| `runEaseNearTop` | 0.55 | ease-out ubrzanje blizu top speed-a (momentum run) |
| `groundFriction` | 14 | skid/decel na pustanje (ne instant stop) |
| `turnSlowdown` | 0.6 | skid pri smeni smera (turn pivot) |
| `riseGravityMult` | 0.95 | blago floaty uspon |
| `fallGravityMult` | 1.35 | fast-fall — tezi, brzi pad |
| `dashSpeed` / `dashTime` / `dashCooldown` | 9 / 0.16 / 0.55 | dodge step burst |
| `depthSpeedFactor` | 0.55 | sporija dubina (koridor feel) |

Dodatno: `OnDashed` event (za SFX/VFX dash-a), dash input LeftAlt / gamepad West.

Sve vrednosti su Inspector-tunable — fine-tune po osecaju bez koda.

## Dodato: air rotation lock + cucanj/puzanje

- **Trcanje/hodanje (toggle):** Shift / gamepad L3 — prvi pritisak = trcanje
  (0.8x), drugi = hodanje (0.5x). Stanje ostaje dok se ne pritisne ponovo.
  Trcanje troši staminu (vidi dole).
- **Air rotation lock:** rotacija modela samo na tlu — u vazduhu facing zakljucan
  (realno: ne moze da se okrene u letu).
- **Cucanj (toggle):** Ctrl / gamepad North — collider 1.2 m, brzina 0.6x, rig se
  spusta (noge savijene, telo nagnuto, ruke drze).
- **Puzanje (toggle, prone crawl):** C / gamepad East — collider 0.8 m (radius
  0.4), brzina 0.3x; telo horizontalno (~78°), noge ispruzene, ruke naizmenicno
  vuku. Puzanje i cucanj se medjusobno iskljucuju; ustajanje ide prvo u cucanj pa
  u stajanje (headroom provera za svaku visinu). Skok i dash nisu dozvoljeni u
  cucnju/puzanju.
- **Headroom:** ne ustaje pod preprekom — proverava se mesto za ciljnu visinu.

## Ledge grab (hvatanje ivice + shimmy + mantle)

- **Auto-hvatanje:** dok si u vazduhu (blizu temena skoka/pad) i gledaš u
  vertikalnu ivicu, lik se sam zakači rukama — bez dodatnog tastera.
- **Visenje:** fizika je privremeno isključena (kinematic), telo visi sa rukama
  na ivici.
- **Shimmy:** A/D (gamepad left stick X) — pomeranje duž ivice; svaki korak
  proverava da ivica i zid i dalje postoje (ne može da se pređe kraj).
- **Penjanje (mantle):** W (gamepad up) — tek posle novog pritiska W (držanje W
  kroz sam skok ne penje automatski). Na kontejneru W vodi na **vrh kontejnera**
  (`containerClimbTime`), a na ostalim ivicama na ivicu/platformu (~0.9 s).
- **Skok sa ivice:** Space (gamepad South) dok visiš — **ti biraš smer**: drži
  **A/D** pre pritiska za levu/desnu platformu; bez smera skače uvis. Vertikalno
  ~5.5 m/s, horizontalno ~6 m/s u izabranom smeru; posle 0.22 s se okrene u tom
  smeru, a 0.6 s možeš da skreneš u vazduhu. **Radi samo na objektu čije ime
  sadrži `kickOffObjectName` (podrazumevano `Container`)** — na platformama
  Space dok visiš ne odguruje.
- **Puštanje:** S (gamepad down) — blagi odskok od zida i pad.
- **Blokade:** dash, čučanj i puzanje su onemogućeni dok visiš; posle
  puštanja/penjanja postoji kratak cooldown i pauza da zadržani A/D ne odvuče
  lik odmah u stranu.

Tuning (Inspector, header "Ledge grab"): `ledgeGrabReach`, `ledgeGrabMinHeight` /
`ledgeGrabMaxHeight`, `hangFeetOffset`, `ledgeShimmySpeed`, `ledgeJumpVelocity`,
`ledgeJumpPush`, `kickOffObjectName`, `mantleTime`, `containerClimbTime`,
`mantleInset`,
`ledgeRegrabDelay`.

## Stamina

- **Max:** 100; trcanje troši **30/s** (~3.3 s neprekidnog trcanja).
- **Regeneracija:** **15/s** posle **0.7 s** bez trcanja (i dok stojiš/miruješ).
- **Iscrpljenost:** na 0 stamina lik je prisilno na hod; Shift se može ponovo
  uključiti tek kad se skupi **15** stamine.
- **HUD:** mali bar gore levo (`showStaminaHud` u PlayerController-u; skripta
  `StaminaHud.cs` se dodaje automatski).
- Tuning: `maxStamina`, `staminaDrainPerSecond`, `staminaRegenPerSecond`,
  `staminaRegenDelay`, `staminaMinToStart`, `runSpeedFactor`.

Test geometrija: postojeći ProBuilder Cube.
